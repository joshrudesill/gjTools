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

            // only allow for degree 1 curves
            var lines = new List<Line>();
            foreach(var o in obj)
            {
                var crv = o.Curve();
                var segs = crv.DuplicateSegments();

                // only lines can make the cut
                foreach (var l in segs)
                    lines.Add(new Line(l.PointAtStart, l.PointAtEnd));
            }

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

                foreach (Line oline in OutLines)
                {
                    if (l.Equals(oline))
                        return;

                    
                }
            }
        }
    }


}