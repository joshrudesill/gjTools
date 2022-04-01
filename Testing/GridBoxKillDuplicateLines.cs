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

            // lets draw some new lines
            ObjectAttributes attr = new ObjectAttributes();
            attr.LayerIndex = obj[0].Object().Attributes.LayerIndex;
            attr.AddToGroup(doc.Groups.Add());
            doc.Objects.UnselectAll();

            foreach (Line l in lines.OutLines)
            {
                Guid ln = doc.Objects.AddLine(l, attr);
                doc.Objects.Select(ln);
            }

            doc.Views.Redraw();
            return Result.Success;
        }

        
        public class LineUnifier
        {
            public List<Line> OutLines;
            public List<Line> m_uniqueLines;

            public LineUnifier(ObjRef[] obj)
            {
                OutLines = new List<Line>();
                m_uniqueLines = new List<Line>();

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

                foreach(var l in lines)
                    FilterUniqueLines(l);

                // testing
                OutLines = m_uniqueLines;
            }

            /// <summary>
            /// filter the input into only unique lines for further processing
            /// </summary>
            /// <param name="l"></param>
            private void FilterUniqueLines (Line l)
            {
                double Tol = 0.1;

                if (m_uniqueLines.Count == 0)
                {
                    m_uniqueLines.Add(l);
                    return;
                }

                for (int i = 0; i < m_uniqueLines.Count; i++)
                {
                    Line ul = m_uniqueLines[i];
                    Line lFlip = l;
                    lFlip.Flip();

                    if (ul.EpsilonEquals(l, Tol) || ul.EpsilonEquals(lFlip, Tol))
                        return;
                }

                m_uniqueLines.Add(l);
            }

            /// <summary>
            /// remove segments that fall within others
            /// </summary>
            private void RemoveUnusedSegments()
            {
                // assign one item to the outlines
                OutLines.Add(m_uniqueLines[0]);
                m_uniqueLines.RemoveAt(0);
                
                while(true)
                {
                    bool foundJoin = false;
                }
            }

            /// <summary>
            /// test for colinearity
            /// </summary>
            /// <param name="l1"></param>
            /// <param name="l2"></param>
            /// <returns></returns>
            public static bool IsLineColinear(Line l1, Line l2)
            {
                Vector3d vec1 = l1.UnitTangent;
                Vector3d vec2 = l2.UnitTangent;

                if (vec1.IsParallelTo(vec2) == 0)
                    return false;

                // they are parrallel at this point, now see if they are on the same ray
                // vertical check
                if (vec1.Y == 1 && vec1.X == 0)
                    if (l1.From.X == l2.From.X)
                        return true;

                // horizontal check
                if (vec1.X == 1 && vec1.Y == 0)
                    if (l1.From.Y == l2.From.Y)
                        return true;

                // Slope intercept check
                if (Math.Round((l1.FromY - GetSlope(l1) * l1.FromX), 2) == Math.Round((l2.FromY - GetSlope(l2) * l2.FromX), 2))
                    return true;

                return false;
            }

            /// <summary>
            /// simple slope getter
            /// </summary>
            /// <param name="l"></param>
            /// <returns></returns>
            public static double GetSlope(Line l)
            {
                double rise = l.ToY - l.FromY;
                double run = l.ToX - l.FromX;

                // vertical line
                if (run == 0)
                    return double.NaN;
                return rise / run;
            }
        }
    }


}