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

        public override string EnglishName => "drawingTools";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // ask for input
            var options = new List<string>(2);
            options.Add("Part Boundries");
            options.Add("Check for Polylines");

            string operation = (string)Rhino.UI.Dialogs.ShowListBox("Part Operations", "Choose Operation", options);
            if (operation == null)
                return Result.Cancel;


            // make part boundry boxes
            if (operation == options[0])
            {
                var gt = new genTools();
                List<string> selections = gt.SelParentLayers(doc, true);

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
                    } else
                    {
                        tmpLay = doc.Layers.FindName("Temp");
                    }

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
            }


            // Check for bad polylines
            if (operation == options[1])
            {
                var objs = new GetObject();
                    objs.SetCommandPrompt("Select Objects to Check");
                    objs.GeometryFilter = Rhino.DocObjects.ObjectType.Curve;
                    objs.GetMultiple(1, 0);
                
                if (objs.CommandResult() != Result.Success)
                    return objs.CommandResult();

                var gt = new genTools();

                gt.CheckPolylines(objs, doc, false);
            }

            return Result.Success;
        }
    }
}