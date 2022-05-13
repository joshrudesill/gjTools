using System.Collections.Generic;
using System;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.DocObjects;
using Rhino.Input;
using Rhino.Input.Custom;

namespace gjTools.Commands
{
    public class NestFan : Command
    {
        public NestFan()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static NestFan Instance { get; private set; }

        public override string EnglishName => "NestFan";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            if (RhinoGet.GetMultipleObjects("Select Objects to Fan", false, ObjectType.Curve, out ObjRef[] objs) != Result.Success)
                return Result.Cancel;

            Fan f = new Fan(objs);
            
            return Result.Success;
        }
    }



    public class Fan : GetPoint
    {
        public double m_fanAngle = 0;
        public Point3d m_start;
        public Point3d m_end;
        public Vector3d m_trans = Vector3d.Zero;
        public int m_qty = 2;
        public double m_space = 0.125;
        public bool IsSuccess = false;
        private bool m_draw = false;
        private List<Curve> m_crvs;

        public Fan(ObjRef[] objs)
        {
            m_crvs = new List<Curve>();
            foreach(var o in objs)
                m_crvs.Add(o.Curve());

            SetCommandPrompt("Base Point for the Fan");
            var res = Get();

            if (res != GetResult.Point)
                return;
            m_start = Point();

            SetCommandPrompt("Rotation touch point");
            AcceptString(true);
            AcceptNumber(true, false);
            AcceptNothing(true);
            res = Get();

            if (res != GetResult.Point)
                return;
            m_end = Point();
            
            if (!GetTransVector())
                return;

            m_draw = true;
            SetCommandPrompt("qty and other things");

            while(Get() != GetResult.Nothing)
            {

            }

            IsSuccess = true;
        }

        private bool GetTransVector()
        {
            BoundingBox bb = BoundingBox.Empty;
            Curve largestCurve = m_crvs[0];
            foreach (var c in m_crvs)
            {
                BoundingBox cbb = c.GetBoundingBox(false);
                if (cbb.Area > largestCurve.GetBoundingBox(false).Area)
                    largestCurve = c;
                bb.Union(cbb);
            }

            Point3d testPoint = new Point3d(m_start.X, m_start.Y + bb.GetEdges()[1].Length, 0);
            Point3d pt = m_start;

            foreach(var c in m_crvs)
            {
                c.ClosestPoint(testPoint, out double param);
                Point3d paramPt = c.PointAt(param);
                if (pt.Y < paramPt.Y)
                    pt = paramPt;
            }

            pt.Y += m_space;
            m_trans.Y = pt.Y;
            Arc testArc = new Arc(pt, m_start.DistanceTo(m_end), RhinoMath.ToRadians(-5));

            bool hit = false;
            int hardStop = 0;
            while(!hit || hardStop != 60)
            {
                NurbsCurve arcnurbs = testArc.ToNurbsCurve();
                hit = Curve.PlanarCurveCollision(largestCurve, arcnurbs, Plane.WorldXY, 0.05);

                testArc.AngleDegrees -= 1;
                hardStop++;
            }

            testArc.AngleDegrees -= m_space / (testArc.Length / testArc.AngleDegrees);

            m_fanAngle = testArc.Angle;
            return hit;
        }

        protected override void OnDynamicDraw(GetPointDrawEventArgs e)
        {
            if (!m_draw)
                return;

            var dupList = new List<Curve>(m_crvs.Count);
            foreach (var c in m_crvs)
            {
                Curve tmpCrv = c.DuplicateCurve();
                tmpCrv.Translate(m_trans);
                dupList.Add(tmpCrv);
            }

            Point3d cnter = m_start;
            Vector3d tmpTrans = m_trans;
            cnter.Transform(Transform.Translation(m_trans));

            for (int i = 0; i < m_qty; i++)
            {
                foreach(var c in dupList)
                {
                    e.Display.DrawCurve(c, System.Drawing.Color.Aquamarine);
                    c.Rotate(m_fanAngle, Vector3d.ZAxis, cnter);
                    c.Translate(tmpTrans);
                }

                tmpTrans.Rotate(m_fanAngle, Vector3d.ZAxis);
                cnter.Transform(Transform.Translation(m_trans));
            }

            base.OnDynamicDraw(e);
        }
    }
}