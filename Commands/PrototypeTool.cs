using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.Input.Custom;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.UI;

namespace gjTools.Commands
{
    public struct OEM_Label
    {
        public List<string> rawLines;
        public string drawingNumber;
        public bool IsValid;
        
        public OEM_Label(string OEMPartNumber)
        {
            rawLines = new List<string>();
            drawingNumber = OEMPartNumber.ToUpper();
            IsValid = false;
            IsValid = GetData();
        }

        private bool GetData()
        {
            string folderPath = "\\\\spi\\art\\PROTOTYPE\\AutoCAD_XML\\";
            if (System.IO.File.Exists(folderPath + drawingNumber + ".xml"))
            {
                var XMLfile = System.IO.File.OpenText(folderPath + drawingNumber + ".xml");
                while (true)
                {
                    string Line = XMLfile.ReadLine();
                    if (Line == "<AUTOCAD>")
                        continue;
                    if (Line == "</AUTOCAD>" || Line == null)
                        break;

                    rawLines.Add(Line);
                }
                return true;
            }
            else
                return false;
        }

        public string partName { get { return rawLines[1]; } }
        public string year { get { return rawLines[2]; } }
        public string customer { get { return rawLines[3]; } }
        public string process { get { return rawLines[4]; } }
        public string partsPerUnit { get { return rawLines[5]; } }
        public string DOC { get { return rawLines[6]; } }
        public string path { get { return rawLines[7]; } }
        public string customerPartNumber { 
            get {
                if (rawLines.Count > 9)
                    return rawLines[9];
                else
                    return "";
            } 
        }
    }

    public class PrototypeTool : Command
    {
        public PrototypeTool()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static PrototypeTool Instance { get; private set; }

        public override string EnglishName => "gjProtoUtility";
        public Layer _parentLayer;

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // SQL Lines used for this Command
            var SQL_Rows = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };
            var sql = new SQLTools();

            var partInfo = sql.queryDataStore(SQL_Rows);
            var slot = partInfo[0];
            var jobInfo = sql.queryJobSlots()[slot.intValue - 1];
            var parts = new List<OEM_Label>();

            // prep data for the Property Box
            var userValues = GetDataFromUser(jobInfo, partInfo);
            if (userValues.Count == 0)
                return Result.Cancel;

            // Split data apart
            var newJobVals = userValues.GetRange(0, 5);
            var newParts = userValues.GetRange(5, 10);

            // write values back to the database
            sql.updateJobSlot(new JobSlot(slot.intValue, newJobVals[0], newJobVals[1], newJobVals[2], int.Parse(newJobVals[3]), newJobVals[4]));
            for (var i = 0; i < newParts.Count; i++)
                sql.updateDataStore(new DataStore(SQL_Rows[i + 1], newParts[i].ToUpper(), 0, 0.0));
            foreach ( var p in newParts)
            {
                var label = new OEM_Label(p);
                if (label.IsValid)
                    parts.Add(label);
            }

            // Get the first OEM Color found
            var colorList = sql.queryOEMColors(newJobVals[4]);
            if (colorList.Count == 0)
                colorList.Add(new OEMColor(newJobVals[4], "Not Found", 0));

            // Present the new data
            var res = CheckDataMessage(newJobVals, parts, colorList[0]);
            if (res.Count == 0)
                return Result.Cancel;

            // add the title block
            bool placed = PlaceTitleBlock(doc, res);
            if (!placed)
                return Result.Cancel;

            // ask if user wants to place labels
            foreach (var p in parts)
                if (!PlaceProtoLabels(doc, p, _parentLayer))
                    break;

            return Result.Success;
        }





        /// <summary>
        /// creates a production style datamatrix label intended for E&P on target Layer
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="part"></param>
        /// <param name="parentLayer"></param>
        /// <returns>True if the label was placed, False if user cancelled</returns>
        public bool PlaceProductionLabel(RhinoDoc doc, OEM_Label part, Layer targetLayer, Point3d pt)
        {
            if (!part.IsValid)
                return false;

            var dt = new DrawTools(doc);
            var ds = dt.StandardDimstyle();

            // make text
            var Etxt = new List<TextEntity> {
                dt.AddText(string.Format("{0}        <datamatrix,{0}>", part.drawingNumber), pt, ds, 0.16, 0, 3, 6),
                dt.AddText(string.Format("{0}   {1} CUT DATE: <date,MM/dd/yyyy> <orderid>", part.customerPartNumber, part.partName), 
                    new Point3d(pt.X, pt.Y - 0.25, 0), ds, 0.14, 0, 3, 0)
            };

            // add to the doc
            foreach(var t in Etxt)
            {
                Guid id = doc.Objects.AddText(t);
                var obj = doc.Objects.FindId(id);
                obj.Attributes.LayerIndex = targetLayer.Index;
                obj.CommitChanges();
            }

            return true;
        }

        /// <summary>
        /// Create a Proto label within the parent::C_TEXT Layer
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="part"></param>
        /// <param name="parentLayer"></param>
        /// <returns>True if the label was placed, False if user cancelled</returns>
        public bool PlaceProtoLabels(RhinoDoc doc, OEM_Label part, Layer parentLayer)
        {
            if (!part.IsValid)
                return false;

            var gp = new GetPoint();
                gp.SetCommandPrompt(string.Format("Place label for {0} - {1}", part.drawingNumber, part.partName));
                gp.Get();

            if (gp.CommandResult() != Result.Success)
                return false;

            var dt = new DrawTools(doc);
            var ds = dt.StandardDimstyle();
            var lt = new LayerTools(doc);

            // create text cut layer
            Layer C_TEXT = lt.CreateLayer("C_TEXT", parentLayer.Name);

            // create the doc block
            TextEntity docNo = dt.AddText( part.DOC, gp.Point(), ds, 0.75, 1, 1, 3 );
            var docCrv = docNo.Explode();
            BoundingBox docbb = docCrv[0].GetBoundingBox(true);
            for (int i = 0;i < docCrv.Length; i++)
            {
                docCrv[i] = docCrv[i].ToPolyline(0.2, RhinoMath.ToRadians(15), 0, 0);
                docbb.Union(docCrv[i].GetBoundingBox(true));
            }
            
            // create the rectangle around the docnumber
            var docRect = new Rectangle3d(Plane.WorldXY, docbb.GetCorners()[0], docbb.GetCorners()[2]).ToNurbsCurve().Offset(
                Plane.WorldXY, 0.06, doc.ModelAbsoluteTolerance, CurveOffsetCornerStyle.Round);

            // create the hatches
            var docObject = new List<Curve>(docRect);
            docObject.AddRange(docCrv);
            var hatch = Hatch.Create(docObject, doc.HatchPatterns.FindName("Grid60").Index, 0, 0.15, doc.ModelAbsoluteTolerance);
            foreach (var h in hatch)
            {
                var pieces = h.Explode();
                foreach (var p in pieces)
                    docObject.Add(p as Curve);
            }

            // make the information text
            var partText = dt.AddText(
                string.Format("{0} {1}\n{2}\n{3}\n{4}", part.year, part.customer, part.partName, part.drawingNumber, part.partsPerUnit),
                new Point3d(docbb.GetCorners()[2].X + 0.131, docbb.GetCorners()[2].Y + 0.06, 0),
                ds, 0.15, 0, 3, 0
            );

            // create a group and add stuff to it after added to the doc
            var group = doc.Groups.Add();
            foreach(var crv in docObject)
            {
                var id = doc.Objects.AddCurve(crv);
                doc.Groups.AddToGroup(group, id);

                var obj = doc.Objects.FindId(id);
                obj.Attributes.LayerIndex = C_TEXT.Index;
                obj.CommitChanges();
            }
            var textId = doc.Objects.AddText(partText);
            doc.Groups.AddToGroup(group, textId);

            var tobj = doc.Objects.FindId(textId);
            tobj.Attributes.LayerIndex = C_TEXT.Index;
            tobj.CommitChanges();

            doc.Views.Redraw();
            return true;
        }

        /// <summary>
        /// places the scaled proto title block
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="textBlocks"></param>
        /// <returns> true or false based on success</returns>
        public bool PlaceTitleBlock(RhinoDoc doc, List<string> textBlocks)
        {
            var dt = new DrawTools(doc);
            var lt = new LayerTools(doc);

            // get the nesting box
            var go = new GetObject();
                go.SetCommandPrompt("Select Nesting Box");
                go.GeometryFilter = ObjectType.Curve;
                go.Get();

            if (go.CommandResult() != Result.Success)
                return false;

            var bb = go.Object(0).Curve().GetBoundingBox(true);
            var fit = bb.GetEdges()[0].Length * 0.7;
            var ds = dt.StandardDimstyle();

            // make the text
            TextEntity txt1 = dt.AddText(
                textBlocks[0],
                new Point3d(bb.GetCorners()[3].X, bb.GetCorners()[3].Y + 0.5, 0),
                ds, 1, 0, 3, 6
            );
            var tbb = txt1.GetBoundingBox(true);
            TextEntity txt2 = dt.AddText(
                textBlocks[1],
                new Point3d(tbb.GetCorners()[2].X, tbb.GetCorners()[2].Y, 0),
                ds, 1, 0, 3, 0
            );

            Transform sc = Transform.Scale(tbb.GetCorners()[0], fit / (txt1.GetBoundingBox(true).GetEdges()[0].Length + txt2.GetBoundingBox(true).GetEdges()[0].Length));
            txt1.Transform(sc, doc.DimStyles[ds]);
            txt2.Transform(sc, doc.DimStyles[ds]);

            // make them the same layer
            _parentLayer = lt.ObjLayer(go.Object(0).ObjectId);
            if (_parentLayer.ParentLayerId != Guid.Empty)
                _parentLayer = doc.Layers.FindId(_parentLayer.ParentLayerId);

            // add the text
            var txtGuid = new List<Guid> {
                doc.Objects.AddText(txt1),
                doc.Objects.AddText(txt2)
            };

            // change text layer
            foreach(var g in txtGuid)
            {
                var obj = doc.Objects.FindId(g);
                obj.Attributes.LayerIndex = _parentLayer.Index;
                obj.CommitChanges();
            }

            doc.Views.Redraw();
            return true;
        }

        /// <summary>
        /// Asks the user for information on the active prototype
        /// </summary>
        /// <param name="job"></param>
        /// <param name="parts"></param>
        /// <returns>empty list on Cancel or job data on OK</returns>
        public List<string> GetDataFromUser(JobSlot job, List<DataStore> parts)
        {
            // prep data for the Property Box
            var propertyLabels = new List<string> { "Job", "Due Date", "Job Description", "Cut QTY", "Film:", "PN 1", "PN 2", "PN 3", "PN 4", "PN 5", "PN 6", "PN 7", "PN 8", "PN 9", "PN 10" };
            var propertyValues = new List<string> { job.job, job.due, job.description, job.quantity.ToString(), job.material };
            for (int i = 1; i < parts.Count; i++)
                propertyValues.Add(parts[i].stringValue);

            var res = Dialogs.ShowPropertyListBox("Prototype Utility", "Job Information", propertyLabels, propertyValues);
            var newValues = new List<string>();
            if (res != null)
                newValues.AddRange(res);

            return newValues;
        }

        /// <summary>
        /// presents the data before placing proto titleblock
        /// </summary>
        /// <param name="job"></param>
        /// <param name="parts"></param>
        /// <param name="color"></param>
        /// <returns>true if ok to place</returns>
        public List<string> CheckDataMessage(List<string> job, List<OEM_Label> parts, OEMColor color)
        {
            // Present the new data
            string textMatl = string.Format("{0} - {1}\nCut - {2}x", color.colorNum, color.colorName, job[3]);
            string textBlob = string.Format("Job: {0}   Due: {1}\n{2}\n", job[0], job[1], job[2]);
            foreach (var t in parts)
                textBlob += string.Format("\n{0} - {1}", t.drawingNumber, t.partName);

            var ok = Dialogs.ShowMessage(string.Format("{0}\n\n{1}", textMatl, textBlob), "Info Preview", ShowMessageButton.OKCancel, ShowMessageIcon.Information);
            var str = new List<string>();
            if (ok == ShowMessageResult.OK)
            {
                str.Add(textBlob);
                str.Add(textMatl);
            }
            return str;
        }
    }
}