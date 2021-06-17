using System;
using Rhino;
using Rhino.Commands;
using System.Collections.Generic;
using Rhino.Geometry;
namespace gjTools.Commands
{
    public class OffsetBoundryBox : Command
    {
        public OffsetBoundryBox()
        {
            Instance = this;
        }
        public static OffsetBoundryBox Instance { get; private set; }

        public override string EnglishName => "OffsetBoundryBox";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            DialogTools d = new DialogTools(doc);
            Rhino.Input.Custom.GetObject go = new Rhino.Input.Custom.GetObject();
            go.SetCommandPrompt("Select object(s) to make offset box..");
            go.GetMultiple(0, -1);
            List<Rhino.DocObjects.RhinoObject> ids = new List<Rhino.DocObjects.RhinoObject>();
            for (int i = 0; i < go.ObjectCount; i++)
            {
                ids.Add(go.Object(i).Object());
            }
            BoundingBox bb;
            Rhino.DocObjects.RhinoObject.GetTightBoundingBox(ids, out bb);
            Point3d[] c = bb.GetCorners();
            double offset = 0.25;
            Rhino.Input.RhinoGet.GetNumber("Offset Distance?", true, ref offset);
            var rect = new Rectangle3d(Plane.WorldXY, new Point3d(c[0].X - offset, c[0].Y - offset, 0), new Point3d(c[2].X + offset, c[2].Y + offset, 0));
            var layer = doc.Layers.FindName("Temp");
            int li;
            if(layer == null)
            {
                li = d.addLayer("Temp", System.Drawing.Color.Black);
            }
            else
            {
                li = layer.Index;
            }
            doc.Layers.SetCurrentLayerIndex(li, true);
            doc.Objects.AddRectangle(rect);
            doc.Views.Redraw();
            return Result.Success;
        }
    }
}