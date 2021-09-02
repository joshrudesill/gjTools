using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Input.Custom;
using Rhino.Input;
using Rhino.Geometry;
using Rhino.DocObjects;
using Rhino.Commands;

namespace gjTools.Commands
{
    public class Nest_ColumnFlip : Command
    {
        public Nest_ColumnFlip()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static Nest_ColumnFlip Instance { get; private set; }

        public override string EnglishName => "Nest_ColumnFlip";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // my get point object
            var gp = new Collision();

            // Get the Base Objects
            if (RhinoGet.GetOneObject("Select Object", false, ObjectType.Curve, out gp.BaseCrv) != Result.Success)
                return Result.Cancel;

            // Spacing
            if (RhinoGet.GetNumber("Spacing between parts", false, ref gp.Spacing) != Result.Success)
                return Result.Cancel;

            // Flip the object and move along line until no intersect events
            var center = gp.BaseCrv.Curve().GetBoundingBox(true).Center;
            gp.SetBasePoint(center, true);
            gp.Base = center;
            gp.CalcOneUp();
            gp.SetCommandPrompt("This Should be fun");

            RhinoApp.WriteLine("This Cammand does nothing currently, just fun proximity stuff");

            gp.Get();

            return Result.Success;
        }
    }

    public class Collision : GetPoint
    {
        public ObjRef BaseCrv;
        public Curve CrvFit;
        public Curve CrvTop;
        public double Spacing = 0.125;
        public Point3d Base = Point3d.Origin;
        public double increment = 0.05;

        public void CalcOneUp()
        {
            CrvTop = BaseCrv.Curve();
            CrvTop.Translate(0, CrvTop.GetBoundingBox(true).GetEdges()[1].Length / 2, 0);
            var inter = Rhino.Geometry.Intersect.Intersection.CurveCurve(BaseCrv.Curve(), CrvTop, 0.1, 0.1);
            while (inter.Count > 0)
            {
                CrvTop.Translate(0, increment, 0);
                inter = Rhino.Geometry.Intersect.Intersection.CurveCurve(BaseCrv.Curve(), CrvTop, 0.1, 0.1);
            }
            CrvTop.Translate(0, Spacing, 0);
        }

        protected override void OnDynamicDraw(GetPointDrawEventArgs e)
        {
            base.OnDynamicDraw(e);

            Line ray = new Line(Base, e.CurrentPoint);
            Curve tmp = BaseCrv.Curve().DuplicateCurve();
            Curve tmpOffset = tmp.Offset(Plane.WorldXY, Spacing / 2, 0.1, CurveOffsetCornerStyle.Smooth)[0];

            if (tmpOffset.ClosestPoint(e.CurrentPoint, out double param, 10))
            {
                var cp = tmpOffset.PointAt(param);
                e.Display.DrawPoint(cp, Rhino.Display.PointStyle.Triangle, 5, System.Drawing.Color.Red);
                
                tmp.Transform(Transform.Rotation(RhinoMath.ToRadians(180), cp));

                e.Display.DrawCurve(tmp, System.Drawing.Color.DarkGreen);
            }

            e.Display.DrawCurve(CrvTop, System.Drawing.Color.DarkGreen);
        }
    }
}