using System;
using Rhino;
using Rhino.Commands;
using System.Collections.Generic;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input.Custom;

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
            double offset = 0.25;
            var lt = new LayerTools(doc);

            var go = new GetObject();
                go.SetCommandPrompt("Select Objects <Offset=" + offset + ">");
                go.GeometryFilter = ObjectType.Curve;
                go.DisablePreSelect();
                go.AcceptNumber(true, true);

            while (true)
            {
                var res = go.GetMultiple(1, 0);
                if (res == Rhino.Input.GetResult.Cancel)
                    return Result.Cancel;
                if (res == Rhino.Input.GetResult.Number)
                {
                    offset = go.Number();
                    go.SetCommandPrompt("Select Objects <Offset=" + offset + ">");
                }
                else if (res == Rhino.Input.GetResult.Object)
                    break;
                else
                    return Result.Cancel;
            }

            var obj = new List<ObjRef> (go.Objects());
            BoundingBox bb = obj[0].Curve().GetBoundingBox(true);

            // Test one object for layer and cuttype
            var layers = lt.isObjectOnCutLayer(obj[0].Object(), true);
            var boxLayer = lt.ObjLayer(obj[0].Object());
            if (layers.Count > 1)
            {
                if (layers[1].Name == "C_KISS")
                    boxLayer = lt.CreateLayer("C_THRU", layers[0].Name, System.Drawing.Color.Red);
                else if (layers[1].Name == "C_THRU")
                    boxLayer = lt.CreateLayer("C_KISS", layers[0].Name, System.Drawing.Color.FromArgb(255, 200, 0, 200));
            }

            // update boundingbox and offset
            foreach (var o in obj)
                bb.Union(o.Curve().GetBoundingBox(true));
            bb.Inflate(offset);

            // create objects and assign layer
            Guid id = doc.Objects.AddRectangle(new Rectangle3d(Plane.WorldXY, bb.GetCorners()[0], bb.GetCorners()[2]));
            var newRect = doc.Objects.FindId(id);
                newRect.Attributes.LayerIndex = boxLayer.Index;
                newRect.CommitChanges();

            doc.Views.Redraw();
            return Result.Success;
        }
    }
}