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

                // start the consolidation processes
                RemoveUnusedSegments();
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
                while(true)
                {
                    if (m_uniqueLines.Count == 0)
                        return;

                    var colinearLines = new List<Line>();
                    colinearLines.Add(m_uniqueLines[0]);
                    m_uniqueLines.RemoveAt(0);

                    // get a collection of colinear lines
                    for (int i = 1; i < m_uniqueLines.Count; i++)
                    {
                        if (!IsLineColinear(colinearLines[0], m_uniqueLines[i]))
                            continue;

                        colinearLines.Add(m_uniqueLines[i]);
                        m_uniqueLines.RemoveAt(i);
                        i--;
                    }

                    // if only one, move on
                    if (colinearLines.Count == 1)
                    {
                        OutLines.Add(colinearLines[0]);
                        continue;
                    }

                    // all lines are accounted for, stop doing things
                    if (colinearLines.Count == 0)
                        return;

                    // we are here, so there are some lines to potentially join together
                    // here we need to condense the lines
                    JoinColinearLines(colinearLines);
                }
            }


            /// <summary>
            /// The input is assumed to all be colinear lines, anything past that will have unpredictable results
            /// </summary>
            /// <param name="l"></param>
            private void JoinColinearLines(List<Line> l)
            {
                // horizontal lines have no soul
                double slope = GetSlope(l[0]);
                double yInt = l[0].FromY;
                bool IsHorizontal = slope == double.NaN;

                if (!IsHorizontal)
                    yInt = l[0].FromY - slope * l[0].FromX;

                var InterVals = new List<Interval>(l.Count);

                // convert the lines to intervals
                for (int i = 0; i < l.Count; i++)
                {
                    if (IsHorizontal)
                        InterVals.Add(new Interval(l[i].FromX, l[i].ToX));
                    else
                        InterVals.Add(new Interval(l[i].FromY, l[i].ToY));
                }

                // combine intervals
                // allow extra passes on the event no interval combinations are hit
                int noHitPass = 0;
                int c = 0;
                while(noHitPass < 8)
                {
                    // check if a union was made
                    bool hasUnion = false;

                    for (int i = 0; i < InterVals.Count; i++)
                    {
                        // skip self or unset checking
                        if (c == i || InterVals[i] == Interval.Unset)
                            continue;

                        if (InterVals[c].IncludesInterval(InterVals[i]))
                        {
                            InterVals[c] = Interval.FromUnion(InterVals[c], InterVals[i]);
                            InterVals[i] = Interval.Unset;
                            hasUnion = true;
                            continue;
                        }
                    }

                    // end loop if only one interval is present
                    int SetCount = 0;
                    foreach (var i in InterVals)
                        if (i != Interval.Unset)
                            SetCount++;

                    if (SetCount > 1)
                        break;

                    // if union, try for another pass
                    if (hasUnion)
                    {
                        noHitPass = 0;
                        continue;
                    }

                    noHitPass++;
                    c = (c + 1 == InterVals.Count) ? 0 : c + 1;

                    // find next set interval
                    for (int i = 0; i < InterVals.Count; i++)
                    {
                        // check counter limits
                        if (InterVals[c] == Interval.Unset)
                        {
                            c = (c + 1 == InterVals.Count) ? 0 : c + 1;
                            continue;
                        }
                        break;
                    }
                }

                // reassemble the lines
                foreach (var i  in InterVals)
                {
                    if (i != Interval.Unset)
                    {
                        if (IsHorizontal)
                        {
                            OutLines.Add(new Line(new Point3d(i.T0, yInt, 0), new Point3d(i.T1, yInt, 0)));
                            continue;
                        }
                        if (slope == 0)
                        {
                            // vertical line
                            OutLines.Add(new Line(new Point3d(l[0].FromX, i.T0, 0), new Point3d(l[0].FromX, i.T1, 0)));
                            continue;
                        }

                        double x1 = (i.T0 - yInt) / slope;
                        double x2 = (i.T1 - yInt) / slope;

                        OutLines.Add(new Line(new Point3d(x1, i.T0, 0), new Point3d(x2, i.T1, 0)));
                    }
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

    public class ConlinearLines
    {

    }

    public struct SimpleLine
    {
        /// <summary>
        /// Y value interval
        /// </summary>
        public SimpleInterval V_Interval;
        /// <summary>
        /// X value interval
        /// </summary>
        public SimpleInterval H_Interval;
        public LineOrientation Orientation;
        public readonly double slope;
        public readonly double Y_Intercept;

        public SimpleLine(Line line)
        {
            H_Interval = new SimpleInterval(line.FromX, line.ToX);
            V_Interval = new SimpleInterval(line.FromY, line.ToY);
            
            // preset these to nan
            slope = double.NaN;
            Y_Intercept = double.NaN;
            Orientation = LineOrientation.Sloped;

            if (V_Interval.Min == V_Interval.Max)
            {
                Orientation = LineOrientation.Horizontal;
                Y_Intercept = V_Interval.Min;
                return;
            }

            if (H_Interval.Min == H_Interval.Max)
            {
                Orientation = LineOrientation.Vertical;
                return;
            }

            // sloped line stuff now
            slope = Math.Round(V_Interval.Length / H_Interval.Length, 3);
            Y_Intercept = Math.Round(slope * H_Interval.Min + V_Interval.Min, 3);
        }

        public bool IsColinear(SimpleLine other)
        {
            return slope == other.slope && Y_Intercept == other.Y_Intercept;
        }

        public bool CheckOverlap(SimpleLine other)
        {
            return H_Interval.CheckOverlap(other.H_Interval) && V_Interval.CheckOverlap(other.V_Interval);
        }

        public void Union(SimpleLine other)
        {
            V_Interval.Union(other.V_Interval);
            H_Interval.Union(other.H_Interval);
        }

        public Line GetLine
        {
            get
            {
                return new Line(new Point3d(H_Interval.Min, V_Interval.Min, 0), new Point3d(H_Interval.Max, V_Interval.Max, 0));
            }
        }
    }

    public enum LineOrientation
    {
        Vertical, Horizontal, Sloped
    }

    public struct SimpleInterval
    {
        public double Min;
        public double Max;
        public double Length;

        public SimpleInterval(double start, double end)
        {
            // Round these to 3 digits
            start = Math.Round(start, 3);
            end = Math.Round(end, 3);
            
            Min = Math.Min(start, end);
            Max = Math.Max(start, end);
            Length = Max - Min;
        }

        public bool CheckOverlap(SimpleInterval other)
        {
            return (other.Min <= Max && other.Min >= Min) || (other.Max <= Max && other.Max >= Min);
        }

        public void Union(SimpleInterval other)
        {
            Min = Math.Min(other.Min, Min);
            Max = Math.Max(other.Max, Max);
        }
    }
}