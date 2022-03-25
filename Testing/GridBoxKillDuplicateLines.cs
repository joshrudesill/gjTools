using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Input;
using Rhino.Geometry;

namespace gjTools.Testing
{
    public class GridBoxKillDuplicateLines : Command
    {
        public GridBoxKillDuplicateLines()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static GridBoxKillDuplicateLines Instance { get; private set; }

        public override string EnglishName => "GridBoxKillDuplicateLines";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            if (RhinoGet.GetMultipleObjects("Select Grid to unify", false, ObjectType.Curve, out ObjRef[] obj) != Result.Success)
                return Result.Cancel;

            // lets kill some segments
            var lines = new LineUnifier(obj);

            return Result.Success;
        }

        
        public class LineUnifier
        {
            public List<Line> OutLines;

            public LineUnifier(ObjRef[] obj)
            {
                OutLines = new List<Line>();

                // only allow for degree 1 curves
                var lines = new List<Line>();
                foreach (var o in obj)
                {
                    var crv = o.Curve();
                    var segs = crv.DuplicateSegments();

                    // only lines can make the cut
                    foreach (var l in segs)
                        lines.Add(new Line(l.PointAtStart, l.PointAtEnd));
                }

                foreach (var l in lines)
                    AddLine(l);
            }

            private void AddLine(Line l)
            {
                if (OutLines.Count == 0)
                {
                    OutLines.Add(l);
                    return;
                }

                for (int i = 0; i < OutLines.Count; i++)
                {
                    Line ol = OutLines[i];
                    if (l.Equals(ol))
                        return;

                    // collect the vectors
                    Vector3d vec1 = ol.UnitTangent;
                    Vector3d vec2 = l.UnitTangent;

                    // not parallel, advance the loop
                    if (vec1.IsParallelTo(vec2) != 1)
                        continue;
                    
                    // end point coincident
                    if (EndPointsTouching(i, l))
                        return;

                    // see if the lines are overlapping
                    if (OverlappingLine(i, l))
                        return;
                }

                // didnt fit any of the other lines
                OutLines.Add(l);
            }

            /// <summary>
            /// if the end points are touching, update the outlines length at index
            /// </summary>
            /// <param name="l1"></param>
            /// <param name="l2"></param>
            /// <returns></returns>
            private bool EndPointsTouching(int l1, Line l2)
            {

                return false;
            }

            /// <summary>
            /// if the lines are overlapping, update the outlines length at index
            /// </summary>
            /// <param name="l1"></param>
            /// <param name="l2"></param>
            /// <returns></returns>
            private bool OverlappingLine(int l1, Line l2)
            {

                return false;
            }
        }
    }


}