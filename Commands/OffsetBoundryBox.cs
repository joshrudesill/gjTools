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
            var go = d.selectObjects("Select object(s) to make boundry box");
            if (go == null)
            {
                RhinoApp.WriteLine("No objects selected. Command canceled");
                return Result.Cancel;
            }
            List<Rhino.DocObjects.RhinoObject> ids = new List<Rhino.DocObjects.RhinoObject>();
            for (int i = 0; i < go.ObjectCount; i++)
            {
                ids.Add(go.Object(i).Object());
            }
            BoundingBox bb;
            Rhino.DocObjects.RhinoObject.GetTightBoundingBox(ids, out bb);
            
            double offset = 0.25;
            Rhino.Input.RhinoGet.GetNumber("Offset Distance?", true, ref offset);
            bb.Inflate(offset);
            var c = bb.GetCorners();
            var rect = new Rectangle3d(Plane.WorldXY, new Point3d(c[0].X, c[0].Y, 0), new Point3d(c[2].X, c[2].Y, 0));
            int layerind = ids[0].Attributes.LayerIndex;
            doc.Layers.SetCurrentLayerIndex(layerind, true);
            doc.Objects.AddRectangle(rect);
            doc.Views.Redraw();
            return Result.Success;
        }
    }
}