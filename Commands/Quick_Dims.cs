using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Input;
using Rhino.Geometry;

namespace gjTools.Commands
{
    public class Quick_Dims : Command
    {
        public Quick_Dims()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static Quick_Dims Instance { get; private set; }

        public override string EnglishName => "QuickDimension";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            if (RhinoGet.GetMultipleObjects("Objects", false, ObjectType.Curve, out ObjRef[] obj) != Result.Success)
                return Result.Cancel;

            if (RhinoGet.GetPoint("Dim Location", false, out Point3d pt) != Result.Success)
                return Result.Cancel;

            // get the bounding of all objects
            var bb = new List<BoundingBox>();
            foreach(var o in obj)
                bb.Add(o.Curve().GetBoundingBox(true));

            AddDimensions(doc, bb, pt);

            return Result.Success;
        }

        private bool AddDimensions(RhinoDoc doc, List<BoundingBox> bb, Point3d pt)
        {
            // Make the overall Bounding
            var bbb = bb[0];
            foreach (var b in bb)
                bbb.Union(b);

            var allDims = new List<LinearDimension>();
            List<int> pIndex = new List<int>();

            if (pt.Y > bbb.Max.Y || pt.Y < bbb.Min.Y) // Horizontal Dims
            {
                if (pt.Y > bbb.Max.Y)      // Top Dimension
                    pIndex = new List<int> { 3, 2 };
                else if (pt.Y < bbb.Min.Y) // Bottom Dimension
                    pIndex = new List<int> { 0, 1 };

                // Make the dims
                foreach (var b in bb)
                {
                    var pts = b.GetCorners();
                    var dim = LinearDimension.FromPoints(pts[pIndex[0]], pts[pIndex[1]],
                            new Point3d(pts[pIndex[0]].X + pts[pIndex[0]].DistanceTo(pts[pIndex[1]]), pt.Y, 0));
                    dim.Aligned = true;
                    allDims.Add(dim);
                }
            }
            else  // Vertical Dims
            {
                if (pt.X > bbb.Max.X)      // Right Dimension
                    pIndex = new List<int> { 1, 2 };
                else if (pt.X < bbb.Min.X) // Left Dimension
                    pIndex = new List<int> { 0, 3 };

                // Make the dims
                foreach (var b in bb)
                {
                    var pts = b.GetCorners();
                    var dim = LinearDimension.FromPoints(pts[pIndex[0]], 
                            new Point3d(pts[pIndex[0]].X + pts[pIndex[0]].DistanceTo(pts[pIndex[1]]), pts[pIndex[0]].Y, 0),
                            new Point3d(pts[pIndex[0]].X, pts[pIndex[0]].Y + (pts[pIndex[0]].X - pt.X), 0));
                    dim.Aligned = true;
                    var plane = dim.Plane;
                        plane.Rotate(RhinoMath.ToRadians(90), Vector3d.ZAxis);
                    dim.Plane = plane;
                    allDims.Add(dim);
                }
            }

            // change the 
            foreach (var d in allDims)
                doc.Objects.AddLinearDimension(d);

            return true;
        }
    }
}