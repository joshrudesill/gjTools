using System;
using Rhino;
using Rhino.Commands;
using System.Collections.Generic;
using Rhino.Geometry;
namespace gjTools.Commands
{
    public class PartOffset : Command
    {
        public PartOffset()
        {
            Instance = this;
        }
        public static PartOffset Instance { get; private set; }
        public override string EnglishName => "PartOffset";
        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            DialogTools d = new DialogTools(doc);
            Rhino.Input.Custom.GetObject go = new Rhino.Input.Custom.GetObject();
            go.SetCommandPrompt("Select object(s) to offset..");
            go.GetMultiple(0, -1);
            List<Rhino.DocObjects.RhinoObject> ids = new List<Rhino.DocObjects.RhinoObject>();
            double offset = 0.125;
            Rhino.Input.RhinoGet.GetNumber("Offset Distance?", true, ref offset);
            var layer = doc.Layers.FindName("Temp");
            int li;
            if (layer == null)
            {
                li = d.addLayer("Temp", System.Drawing.Color.FromArgb(135, 0, 255, 21));
            }
            else
            {
                li = layer.Index;
            }
            doc.Layers.SetCurrentLayerIndex(li, true);
            Curve[] cl;
            for (int i = 0; i < go.ObjectCount; i++)
            {
                cl = go.Object(i).Curve().Offset(Plane.WorldXY, offset, 0.001, CurveOffsetCornerStyle.Round);
                doc.Objects.AddCurve(cl[0]);
            }
            doc.Views.Redraw();
            return Result.Success;
        }
    }
}