using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.DocObjects;
using Rhino.Input.Custom;
using Rhino.Input;
using System;
using System.Collections.Generic;

namespace gjTools
{
    public struct CutSort
    {
        public List<ObjRef> obj;
        public List<int> objLayerIndex;
        public List<double> objCutLength;
        private readonly RhinoDoc doc;

        public int groupCount;
        public int parentLayerIndex;

        public CutSort(List<RhinoObject> selections)
        {
            obj = new List<ObjRef>();
            objLayerIndex = new List<int>();
            objCutLength = new List<double>();
            groupCount = 0;
            parentLayerIndex = -1;
            doc = selections[0].Document;

            // Sort the objects
            var uniqueGroup = new List<int>();
            foreach (var o in selections)
            {
                Layer objLayer = doc.Layers[o.Attributes.LayerIndex];

                if (objLayer.Name.Substring(0, 2) == "C_")
                {
                    if (parentLayerIndex == -1)
                    {
                        // get the parent of one object
                        if (objLayer.ParentLayerId != Guid.Empty)
                            parentLayerIndex = doc.Layers.FindId(objLayer.ParentLayerId).Index;
                    }

                    // add object to lists
                    var objR = new ObjRef(o);
                    obj.Add(objR);
                    objLayerIndex.Add(objLayer.Index);
                    objCutLength.Add(Math.Round(objR.Curve().GetLength(), 1));

                    // check groups
                    var group = o.Attributes.GetGroupList();
                    if (group != null && !uniqueGroup.Contains(group[0]))
                        uniqueGroup.Add(group[0]);
                }
            }
            groupCount = uniqueGroup.Count;
        }

        public CutSort(List<ObjRef> selections)
        {
            obj = new List<ObjRef>();
            objLayerIndex = new List<int>();
            objCutLength = new List<double>();
            groupCount = 0;
            parentLayerIndex = -1;
            doc = selections[0].Object().Document;

            // Sort the objects
            var uniqueGroup = new List<int>();
            foreach(var o in selections)
            {
                Layer objLayer = doc.Layers[o.Object().Attributes.LayerIndex];

                if (objLayer.Name.Substring(0,2) == "C_")
                {
                    if (parentLayerIndex == -1)
                    {
                        // get the parent of one object
                        if (objLayer.ParentLayerId != Guid.Empty)
                            parentLayerIndex = doc.Layers.FindId(objLayer.ParentLayerId).Index;
                    }

                    // add object to lists
                    obj.Add(o);
                    objLayerIndex.Add(objLayer.Index);
                    objCutLength.Add(Math.Round(o.Curve().GetLength(), 1));

                    // check groups
                    var group = o.Object().Attributes.GetGroupList();
                    if (group != null && !uniqueGroup.Contains(group[0]))
                        uniqueGroup.Add(group[0]);
                }
            }
            groupCount = uniqueGroup.Count;
        }

        public List<RhinoObject> GetRhinoObjects
        {
            get
            {
                var rObj = new List<RhinoObject>();
                foreach (var o in obj)
                    rObj.Add(o.Object());
                return rObj;
            }
        }
        public List<int> GetCutLayerIndexes
        {
            get
            {
                var cutList = new List<int>();
                foreach (int i in objLayerIndex)
                    if (!cutList.Contains(i))
                        cutList.Add(i);
                return cutList;
            }
        }
        public double CutLength(int layerIndex)
        {
            double count = 0;
            for (var i = 0; i < obj.Count; i++)
                if (objLayerIndex[i] == layerIndex)
                    count += objCutLength[i];
            return Math.Round(count);
        }
    }

    public class NestingBox : Command
    {
        public NestingBox()
        {
            Instance = this;
        }

        public static NestingBox Instance { get; private set; }

        public override string EnglishName => "gjNestingBox";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var obj = new GetObject();
                obj.SetCommandPrompt("Select Objects");
                obj.GeometryFilter = ObjectType.Curve;
                obj.GroupSelect = true;
                obj.GetMultiple(1, 0);

            if (obj.CommandResult() != Result.Success)
                return Result.Cancel;

            var lt = new LayerTools(doc);
            var cuts = new CutSort(new List<ObjRef>(obj.Objects()));

            RhinoObject.GetTightBoundingBox(cuts.GetRhinoObjects, out BoundingBox bb);
            
            // calc sheet size
            double sheetHeight = 48;
            double sheetWidth = 1;
            if (RhinoGet.GetNumber("Sheet Height", false, ref sheetHeight) != Result.Success)
                return Result.Cancel;
            sheetHeight = SheetEndSize(sheetHeight, bb.GetEdges()[1]);
            if (RhinoGet.GetNumber("Sheet Width", false, ref sheetWidth) != Result.Success)
                return Result.Cancel;
            sheetWidth = SheetEndSize(sheetWidth, bb.GetEdges()[0]);

            //  nest box object
            Rectangle3d nestBox = new Rectangle3d(
                Plane.WorldXY,
                new Point3d(bb.Center.X - sheetWidth, bb.Center.Y - sheetHeight, 0.0),
                new Point3d(bb.Center.X + sheetWidth, bb.Center.Y + sheetHeight, 0.0)
            );

            var box = doc.Objects.FindId(doc.Objects.AddRectangle(nestBox));
                box.Attributes.Name = "NestBox";
                box.Attributes.LayerIndex = lt.CreateLayer("NestBox", doc.Layers[cuts.parentLayerIndex].Name).Index;
                box.CommitChanges();

            CollectInfo(cuts, doc, bb, nestBox);

            doc.Views.Redraw();
            return Result.Success;
        }


        /// <summary>
        /// Makes the decision on sheet size
        /// </summary>
        /// <param name="userVal"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        public double SheetEndSize(double userVal, Line line)
        {
            if (userVal <= 3.95)
                return RoundQuarterIn((userVal * 2) + line.Length) / 2;
            else
                return RoundQuarterIn(userVal) / 2;
        }

        /// <summary>
        /// Rounds the input to quarter inch within tolerance
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        public double RoundQuarterIn(double num)
        {
            int wholeNum = (int)num;
            int afterDecimal = (int)((num - wholeNum) * 100);
            var ranges = new List<int> { 8, 30, 60, 80 };

            if (afterDecimal >= ranges[3])
                return wholeNum + 1;

            for (int i = 0;i < 4; i++)
            {
                if (afterDecimal < ranges[i])
                {
                    afterDecimal = 25 * i;
                    break;
                }
            }

            return wholeNum + (afterDecimal * 0.01);
        }

        /// <summary>
        /// Assembles the info from the nesting
        /// </summary>
        /// <param name="cutInfo"></param>
        /// <param name="doc"></param>
        /// <param name="bb"></param>
        /// <param name="nestBox"></param>
        public void CollectInfo(CutSort cuts, RhinoDoc doc, BoundingBox bb, Rectangle3d nestBox)
        {
            // build the text values
            string partNumber = "PN: " + doc.Layers[cuts.parentLayerIndex].Name;
            
            string path = (doc.Name == null) ? "File Not Saved" : "Path: " + doc.Path;

            string shtSizeInfo = string.Format("Sheet Size: {0}w x {1}h   (Part Area: {2}w x {3}h)",
                nestBox.Width, nestBox.Height, Math.Round(bb.GetEdges()[0].Length, 2), Math.Round(bb.GetEdges()[3].Length, 2));
            
            string itemLine = (cuts.groupCount > 0) ? "Items up: " + cuts.groupCount : "Items up: " + cuts.obj.Count;

            itemLine += "    Kerf:";
            foreach (var l in cuts.GetCutLayerIndexes)
                itemLine += string.Format(" [{0}: {1}]", doc.Layers[l].Name.Substring(2), cuts.CutLength(l));

            var creds = new SQLTools();
            string timeStamp = creds.queryVariableData()[0].userFirstName + "\n" + DateTime.UtcNow;

            var tool = new DrawTools(doc);
            int ds = tool.StandardDimstyle();

            var textEnt = new List<TextEntity>
            {
                tool.AddText(partNumber, new Point3d(0.1, -0.1, 0), ds, 0.135, 1),
                tool.AddText(path, new Point3d(0.1, -0.35, 0), ds, 0.07),
                tool.AddText(shtSizeInfo, new Point3d(0.1, -0.6, 0), ds, 0.1, 1),
                tool.AddText(itemLine, new Point3d(0.1, -0.8, 0), ds, 0.1, 2)
            };

            // Name and date/time inside the top-right corner of the current text
            BoundingBox tbb = BoundingBox.Empty;
            foreach (TextEntity t in textEnt)
                tbb.Union(t.GetBoundingBox(true));

            textEnt.Add(tool.AddText(timeStamp, tbb.GetCorners()[2], ds, 0.06, 1, 2));

            var box = new Rectangle3d(
                Plane.WorldXY,
                tbb.GetCorners()[0].DistanceTo(tbb.GetCorners()[1]) + 0.2,
                -tbb.GetCorners()[1].DistanceTo(tbb.GetCorners()[2]) - 0.2
            );
            var line = new Line(
                new Point3d(0.1, tbb.Center.Y, 0),
                new Point3d(tbb.GetCorners()[1].X, tbb.Center.Y, 0)
            );

            // move data
            Point3d basePt = nestBox.Corner(0);
            Transform Xmove = Transform.Translation(basePt.X, basePt.Y, 0);

            // scale data
            double scale = 1;
            var allowedWidth = new List<int> { 9, 12, 23, 36, 44, 65, 142, 200 };
            foreach (int i in allowedWidth)
                if (nestBox.Width > i)
                    scale = i / box.Width;
            Transform Xscale = Transform.Scale(basePt, scale);

            // Perform the translates
            box.Transform(Xmove);
            box.Transform(Xscale);
            line.Transform(Xmove);
            line.Transform(Xscale);

            var objIDs = new List<Guid>();

            foreach (var i in textEnt)
            {
                i.Transform(Xmove);
                i.Transform(Xscale);
                i.TextHeight = i.TextHeight * scale;
                objIDs.Add(doc.Objects.Add(i));
            }

            objIDs.Add(doc.Objects.AddRectangle(box));
            objIDs.Add(doc.Objects.AddLine(line));

            doc.Groups.Add(objIDs);
            foreach (Guid i in objIDs)
            {
                var obj = doc.Objects.FindId(i);
                    obj.Attributes.LayerIndex = cuts.parentLayerIndex;
                    obj.CommitChanges();
            }
                
            doc.Views.Redraw();
        }
    }
}
