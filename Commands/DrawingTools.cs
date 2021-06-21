using System;
using Rhino;
using Rhino.Geometry;
using Rhino.Input.Custom;
using Rhino.Commands;
using System.Collections;
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
            var options = new List<string> { "Part Boundries", "Check for Polylines" };

            string operation = (string)Rhino.UI.Dialogs.ShowListBox("Part Operations", "Choose Operation", options);
            if (operation == null)
                return Result.Cancel;


            if (operation == options[0])
                return PartBoundries(doc);

            if (operation == options[1])
                return CheckPolylines(doc);
            
            return Result.Success;
        }


        /// <summary>
        /// Make Boundry boxes around selected layers as a way of seeing if something got included on the wrong layer.
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public Result PartBoundries(RhinoDoc doc)
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

            return Result.Success;
        }


        /// <summary>
        /// Checks that the selected are polylines and shows a nifty X-Mas like display
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public Result CheckPolylines(RhinoDoc doc)
        {
            var objs = new GetObject();
            objs.SetCommandPrompt("Select Objects to Check");
            objs.GeometryFilter = Rhino.DocObjects.ObjectType.Curve;
            objs.GetMultiple(1, 0);

            if (objs.CommandResult() != Result.Success)
                return objs.CommandResult();

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

            return Result.Success;
        }
    }
}