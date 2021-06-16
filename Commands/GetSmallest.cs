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
            go.SetCommandPrompt("Select objects to get smallest rotation..");
            go.Get();
            BoundingBox bb;
            Rhino.DocObjects.RhinoObject ro = go.Object(0).Object();
            bb = ro.Geometry.GetBoundingBox(true);
            List<BRotation> brl = new List<BRotation>();
            Point3d[] ps = bb.GetCorners();
            brl.Add(new BRotation(0, Math.Abs(ps[0].Y - ps[3].Y)));
            double rotation = (2 * Math.PI) / 360;
            BoundingBox bbt;
            for (int j = 1; j < 360; j++)
            {
                ro.Geometry.Rotate(rotation, new Vector3d(0, 0, 1), bb.Center);
                bbt = ro.Geometry.GetBoundingBox(true);
                Point3d[] pst = bbt.GetCorners();
                brl.Add(new BRotation(j * rotation, Math.Abs(pst[0].Y - pst[3].Y)));
            }
            double height = 100000000000000000;
            int index = 0;
            for (int i = 0; i < brl.Count; i++)
            {
                if (brl[i].length < height)
                {
                    height = brl[i].length;
                    index = i;
                }
            }
            RhinoApp.WriteLine((index * rotation).ToString());
            RhinoApp.WriteLine(ro.Geometry.Rotate(index * rotation, new Vector3d(0, 0, 1), new Point3d(0, 0, 0)).ToString());
            var r = Transform.Rotation(index * rotation, new Vector3d(0, 0, 1), new Point3d(0, 0, 0));
            ro.Geometry.Transform(r);
            doc.Views.Redraw();
            RhinoApp.WriteLine(index.ToString());
            return Result.Success;
        }
    }
}