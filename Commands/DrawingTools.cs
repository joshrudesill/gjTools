using System;
using Rhino;
using Rhino.Geometry;
using Rhino.Input.Custom;
using Rhino.Commands;
using System.Collections.Generic;

namespace gjTools
{
    public class DrawingTools : Command
    {
        public DrawingTools()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static DrawingTools Instance { get; private set; }

        public override string EnglishName => "gjdrawingTools";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // ask for input
            var options = new List<string> { 
                "Part Boundries", 
                "Check for Polylines", 
                "Make Objects into Circles", 
                "Destroy all Blocks" 
            };

            string operation = (string)Rhino.UI.Dialogs.ShowListBox("Part Operations", "Choose Operation", options);
            if (operation == null)
                return Result.Cancel;


            if (operation == options[0])
                PartBoundries(doc);

            if (operation == options[1])
                CheckPolylines(doc);

            if (operation == options[2])
                ForceCircleOnObject(doc);

            if (operation == options[3])
                ExplodeAllBlocks(doc);

            return Result.Success;
        }



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
                    Guid cir = doc.Objects.AddCircle(new Circle(bb.Center, bb.GetEdges()[0].Length / 2));
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
    }
}