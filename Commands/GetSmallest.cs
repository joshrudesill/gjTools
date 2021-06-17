using System;
using Rhino;
using Rhino.Commands;
using System.Collections.Generic;
using Rhino.Geometry;
namespace gjTools.Commands
{
    struct BRotation
    {
        public BRotation(double rotation, double length)
        {
            this.rotation = rotation;
            this.length = length;
        }
        public double rotation;
        public double length;
    }
    public class GetSmallest : Command
    {
        public GetSmallest()
        {
            Instance = this;
        }

        public static GetSmallest Instance { get; private set; }

        public override string EnglishName => "gjGetSmallest";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            Rhino.Input.Custom.GetObject go = new Rhino.Input.Custom.GetObject();
            go.SetCommandPrompt("Select object to get smallest rotation..");
            go.Get();
            BoundingBox bb;
            Rhino.DocObjects.RhinoObject ro = go.Object(0).Object();
            bb = ro.Geometry.GetBoundingBox(true);
            List<BRotation> brl = new List<BRotation>();
            Point3d[] ps = bb.GetCorners();
            brl.Add(new BRotation(0, Math.Abs(ps[0].Y - ps[3].Y)));
            double rotation = (2 * Math.PI) / 3600;
            BoundingBox bbt;
            for (int j = 0; j < 3600; j++)
            {
                var xf = Transform.Rotation(rotation, bb.Center);
                Guid id = doc.Objects.Transform(ro, xf, true);
                var ror1 = new Rhino.DocObjects.ObjRef(id);
                var ro1 = ror1.Object();
                bbt = ro1.Geometry.GetBoundingBox(true);
                Point3d[] pst = bbt.GetCorners();
                brl.Add(new BRotation(j * rotation, Math.Abs(pst[0].Y - pst[3].Y)));
                ro = ro1;
            }
            double height = 10000000000000080085;
            int index = 0;
            for (int i = 0; i < brl.Count; i++)
            {
                if (brl[i].length < height)
                {
                    height = brl[i].length;
                    index = i;
                }
            }
            var r = Transform.Rotation(brl[index].rotation, bb.Center);
            doc.Objects.Transform(ro, r, true);
            doc.Views.Redraw();
            return Result.Success;
        }
    }
}