using System;
using Rhino;
using Rhino.Geometry;
using Rhino.DocObjects;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.Commands;
using System.Collections.Generic;

namespace gjTools
{

    public struct MeasureDrawing
    {
        public TextEntity pnTxt;
        public RhinoObject pnTxtObj;
        public Layer pnLayer;

        public List<RhinoObject> cutObjs;
        public Point3d topLeftPt;

        public string GetPN
        {
            get { return pnTxt.PlainText.Replace("PN:", "").Trim(); }
        }
        public List<Point3d> GetDiagRegion (Point3d pt)
        {
            return new List<Point3d> {
                topLeftPt,
                new Point3d(topLeftPt.X + 110, topLeftPt.Y, 0),
                new Point3d(pt.X + 110, pt.Y, 0),
                new Point3d(pt.X, pt.Y, 0)
            };
        }
    }

    public class DrawingTools : Command
    {
        public DrawingTools()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static DrawingTools Instance { get; private set; }

        public override string EnglishName => "DrawingTools";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // ask for input
            var options = new List<string> { 
                "Part Boundries", 
                "Check for Polylines", 
                "Make Objects into Circles", 
                "Destroy all Blocks",
                "Process Measure Drawing",
                "Make Selection to Height",
                "Make Selection to Width"
            };

            string operation = "";

            if (mode == RunMode.Interactive)
            {
                operation = (string)Rhino.UI.Dialogs.ShowListBox("Part Operations", "Choose Operation", options);
                if (operation == null)
                    return Result.Cancel;
            }
            else
            {
                int opt = 0;
                if (RhinoGet.GetInteger($"Part Operations 0-{options.Count - 1}", false, ref opt) != Result.Success)
                    return Result.Cancel;
                operation = options[opt];
            }

            if (operation == options[0])
                PartBoundries(doc);
            if (operation == options[1])
                CheckPolylines(doc);
            if (operation == options[2])
                ForceCircleOnObject(doc);
            if (operation == options[3])
                ExplodeAllBlocks(doc);
            if (operation == options[4])
                MeasureDrawingPrep(doc);
            if (operation == options[5])
                MakeObjectToSize(doc, true);
            if (operation == options[6])
                MakeObjectToSize(doc, false);


            return Result.Success;
        }


        public bool MakeObjectToSize(RhinoDoc doc, bool height = true)
        {
            if (RhinoGet.GetMultipleObjects("Select Objects", false, ObjectType.Curve, out ObjRef[] obj) != Result.Success)
                return false;

            var bb = obj[0].Geometry().GetBoundingBox(true);
            foreach (var o in obj)
                bb.Union(o.Geometry().GetBoundingBox(true));
            double currentSize = (height) ? bb.GetEdges()[3].Length : bb.GetEdges()[0].Length;
            double origSize = currentSize;
            string Label = (height) ? "Height" : "Width";

            if (RhinoGet.GetNumber($"Size Requirement for {Label}", false, ref currentSize) != Result.Success)
                return false;

            // scale the objects
            var scaleTransform = Transform.Scale(bb.GetCorners()[0], currentSize / origSize);
            RhinoApp.WriteLine($"Scaled {Math.Round(currentSize / origSize, 2)} from Original");
            foreach (var o in obj)
                doc.Objects.Transform(o, scaleTransform, true);
            
            return true;
        }

        /// <summary>
        /// Untested fully, but should work fine
        /// </summary>
        /// <param name="doc"></param>
        public void ExplodeAllBlocks(RhinoDoc doc)
        {
            var blocks =new List<Rhino.DocObjects.InstanceDefinition>(doc.InstanceDefinitions.GetList(true));
            foreach (var b in blocks)
            {
                var NestBlocks = new List<Rhino.DocObjects.InstanceObject>(b.GetReferences(1));
                if (NestBlocks.Count > 0)
                    foreach (var nest in NestBlocks)
                        doc.Objects.AddExplodedInstancePieces(nest, true, true);

                doc.InstanceDefinitions.Delete(b);
            }
        }

        /// <summary>
        /// Makes objects into circles if they fit into tolerance
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public bool ForceCircleOnObject(RhinoDoc doc, double tolerance = 0.05)
        {
            var go = new GetObject();
                go.SetCommandPrompt("Objects to Try Circle Convert <Tolerance=" + tolerance + ">");
                go.AcceptNumber(true, true);
                go.GeometryFilter = Rhino.DocObjects.ObjectType.Curve;
                go.DisablePreSelect();
            var res = go.GetMultiple(1, 0);

            while (res != Rhino.Input.GetResult.Object)
            {
                if (go.CommandResult() == Result.Cancel)
                    return false;
                else if (res == Rhino.Input.GetResult.Number)
                {
                    tolerance = go.Number();
                    go.SetCommandPrompt("Objects to Try Circle Convert <Tolerance=" + tolerance + ">");
                    res = go.GetMultiple(1, 0);
                }
                else
                    res = go.GetMultiple(1, 0);
            }

            var obj = new List<Rhino.DocObjects.ObjRef> (go.Objects());
            int converted = 0;
            foreach(var o in obj)
            {
                var crv = o.Curve();
                var bb = crv.GetBoundingBox(true);
                double diff = 0.0;

                if (bb.GetEdges()[0].Length >= bb.GetEdges()[1].Length)
                    diff = bb.GetEdges()[0].Length - bb.GetEdges()[1].Length;
                else
                    diff = bb.GetEdges()[1].Length - bb.GetEdges()[0].Length;

                if (diff <= tolerance)
                {
                    int layerIndex = doc.Objects.FindId(o.ObjectId).Attributes.LayerIndex;
                    Guid cir = doc.Objects.AddCircle(new Circle(bb.Center, bb.GetEdges()[0].Length / 2));
                    
                    var newObj = doc.Objects.FindId(cir);
                        newObj.Attributes.LayerIndex = layerIndex;
                        newObj.CommitChanges();
                    
                    doc.Objects.Select(cir);
                    doc.Objects.Delete(o, true);
                    converted++;
                }
            }

            RhinoApp.WriteLine(converted + " Objects were converted to Circles");
            return true;
        }

        /// <summary>
        /// Make Boundry boxes around selected layers as a way of seeing if something got included on the wrong layer.
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public bool PartBoundries(RhinoDoc doc)
        {
            var gt = new DrawTools(doc);
            List<string> selections = gt.SelParentLayers(true);

            foreach (string sel in selections)
            {
                BoundingBox bb;
                var layObj = new List<Rhino.DocObjects.RhinoObject>();

                // get objects from parent layer (If any)
                foreach (var o in doc.Objects.FindByLayer(doc.Layers.FindName(sel)))
                    layObj.Add(o);

                // get sub-layers of parent
                var subLays = doc.Layers.FindName(sel).GetChildren();
                foreach (var sl in subLays)
                    foreach (var o in doc.Objects.FindByLayer(sl))
                        layObj.Add(o);

                bb = layObj[0].Geometry.GetBoundingBox(true);
                foreach (var b in layObj)
                    bb.Union(b.Geometry.GetBoundingBox(true));

                // Create new Temp Layer
                Rhino.DocObjects.Layer tmpLay;
                if (doc.Layers.FindName("Temp") == null)
                {
                    tmpLay = doc.Layers[doc.Layers.Add()];
                    tmpLay.Name = "Temp";
                }
                tmpLay = doc.Layers.FindName("Temp");

                // define layer
                tmpLay.Color = System.Drawing.Color.Aquamarine;
                tmpLay.LinetypeIndex = doc.Linetypes.FindName("DashDot").Index;

                // create bounding box
                Point3d[] cpt = bb.GetCorners();
                Plane p = new Plane(cpt[0], cpt[1], cpt[3]);
                Rectangle3d r = new Rectangle3d(p, cpt[0], cpt[2]);

                var id = doc.Objects.AddRectangle(r);
                var idObj = doc.Objects.FindId(id);
                idObj.Attributes.LayerIndex = tmpLay.Index;
                idObj.CommitChanges();
            }
            doc.Views.Redraw();

            return true;
        }

        /// <summary>
        /// Checks that the selected are polylines and shows a nifty X-Mas like display
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public bool CheckPolylines(RhinoDoc doc)
        {
            var objs = new GetObject();
            objs.SetCommandPrompt("Select Objects to Check");
            objs.GeometryFilter = Rhino.DocObjects.ObjectType.Curve;
            objs.GetMultiple(1, 0);

            if (objs.CommandResult() != Result.Success)
                return false;

            var gt = new DrawTools(doc);
            bool res = gt.CheckPolylines(objs, true);

            var cancelCommand = new GetString();
            if (res)
                cancelCommand.SetCommandPrompt("All Lines are Good Polylines, Enter to Continue...");
            else
                cancelCommand.SetCommandPrompt("Bad Lines and Need Converting, Enter to Continue...");

            cancelCommand.AcceptNothing(true);
            cancelCommand.Get();

            gt.hideDynamicDraw();

            if (res)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Layer out the Measured drawings
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public bool MeasureDrawingPrep(RhinoDoc doc)
        {
            doc.Objects.UnselectAll();

            var txt = doc.Objects.FindByLayer("C_PTNO");
            var divPoint = new List<Point3d>();
            var lt = new LayerTools(doc);
            var md = new List<MeasureDrawing>
            {
                new MeasureDrawing { topLeftPt = new Point3d(-10, -1, 0) }
            };  // add an empty to the list

            // check the object for PN
            foreach (var t in txt)
            {
                var oref = new ObjRef(t).TextEntity();
                
                // if casting success
                if (oref == null)
                    continue;
                
                // if a PN Tag
                if (oref.PlainText.Substring(0, 3) != "PN:")
                    continue;

                md.Add(new MeasureDrawing
                {
                    pnTxt = oref,
                    pnTxtObj = t,
                    topLeftPt = new Point3d(-10, oref.GetBoundingBox(true).GetCorners()[2].Y + 0.25, 0)
                });
            }

            // Sort the point by Y coord
            md.Sort((x, y) => x.topLeftPt.Y.CompareTo(y.topLeftPt.Y));

            var garbageLays = new List<Layer>();

            // sort the parts
            for(int i = 1; i < md.Count; i++)
            {
                var mdObj = md[i];
                var ptObj = doc.Objects.FindByCrossingWindowRegion(
                    doc.Views.ActiveView.MainViewport,
                    mdObj.GetDiagRegion(md[i-1].topLeftPt),
                    true,
                    ObjectType.Curve
                );

                // make pn layer
                mdObj.pnLayer = lt.CreateLayer(mdObj.GetPN);

                // add to garbage
                if (!garbageLays.Contains(doc.Layers[mdObj.pnTxtObj.Attributes.LayerIndex]))
                    garbageLays.Add(doc.Layers[mdObj.pnTxtObj.Attributes.LayerIndex]);

                // assign the text string to the pn layer
                mdObj.pnTxtObj.Attributes.LayerIndex = mdObj.pnLayer.Index;
                mdObj.pnTxtObj.CommitChanges();

                // add the objects and remove the pn text
                mdObj.cutObjs = new List<RhinoObject>(ptObj);

                // reassign the layer
                foreach(var o in mdObj.cutObjs)
                {
                    if (o == mdObj.pnTxtObj)
                        continue;

                    var curLay = doc.Layers[o.Attributes.LayerIndex];

                    var newLayer = lt.CreateLayer(
                        curLay.Name,
                        mdObj.pnLayer.Name,
                        curLay.Color
                    );

                    if (!garbageLays.Contains(curLay))
                        garbageLays.Add(curLay);

                    o.Attributes.LayerIndex = newLayer.Index;
                    o.CommitChanges();
                }

                md[i] = mdObj;
            }

            // remove the element that's empty
            md.RemoveAt(0);

            // delete the old layers
            if (garbageLays.Count > 0)
                foreach (var l in garbageLays)
                    doc.Layers.Delete(l.Index, true);

            return true;
        }
    }
}