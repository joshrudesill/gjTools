using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using Rhino.Input;
using Rhino.DocObjects;

namespace gjTools
{
    public class JTest : Command
    {
        public JTest()
        {
            Instance = this;
        }

        public static JTest Instance { get; private set; }
        public override string EnglishName => "asdf";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            if (RhinoGet.GetOneObject("Farts", false, ObjectType.AnyObject, out ObjRef obj) != Result.Success)
                return Result.Cancel;

            var bb = obj.Geometry().GetBoundingBox(true);
            var pts = bb.GetCorners();

            var hOnPt = new Point3d(pts[3].X + pts[3].DistanceTo(pts[2]) / 2, pts[3].Y + 2, 0);
            var hPlane = new Plane(pts[3], pts[2], hOnPt);
            
            var vOnPt = new Point3d(pts[0].X - 2, pts[0].Y + pts[0].DistanceTo(pts[3]) / 2, 0);
            var vPlane = new Plane(pts[0], pts[3], vOnPt);

            hPlane.ClosestParameter(pts[3], out double x1, out double y1);
            hPlane.ClosestParameter(pts[2], out double x2, out double y2);
            hPlane.ClosestParameter(hOnPt, out double x3, out double y3);

            var dim1 = new LinearDimension(hPlane, new Point2d(x1, y1), new Point2d(x2, y2), new Point2d(x3, y3));

            vPlane.ClosestParameter(pts[0], out x1, out y1);
            vPlane.ClosestParameter(pts[3], out x2, out y2);
            vPlane.ClosestParameter(vOnPt, out x3, out y3);

            var dim2 = new LinearDimension(vPlane, new Point2d(x1, y1), new Point2d(x2, y2), new Point2d(x3, y3));

            var ds = new DrawTools(doc).StandardDimstyle();
            dim1.Aligned = dim2.Aligned = true;
            dim1.DimensionStyleId = dim2.DimensionStyleId = doc.DimStyles[ds].Id;

            doc.Objects.AddLinearDimension(dim1);
            doc.Objects.AddLinearDimension(dim2);

            return Result.Success;
        }
    }
}