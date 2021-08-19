using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.Geometry;
using Rhino.DocObjects;

namespace gjTools.Commands
{
    public class ArrayCopy_Fit : Command
    {
        public ArrayCopy_Fit()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static ArrayCopy_Fit Instance { get; private set; }

        public override string EnglishName => "CopyFit";
        private int CF_qty = 2;

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            if (RhinoGet.GetMultipleObjects("Select Objects", false, ObjectType.AnyObject, out ObjRef[] obj) != Result.Success)
                return Result.Cancel;

            if (RhinoGet.GetPoint("Start Point", false, out Point3d start) != Result.Success)
                return Result.Cancel;

            var qtyOpt = new OptionInteger(CF_qty, true, 2);
            var cf = new CopyFit();
                cf.SetCommandPrompt("Select Start Point");
                cf.AddOptionInteger("QTY", ref qtyOpt);
                cf.SetBasePoint(start, true);
                cf.CopyQty = CF_qty;
                cf.obj = new List<ObjRef>(obj);
                cf.start = start;
            var res = cf.Get();

            while (true)
            {
                if (res == GetResult.Cancel || res == GetResult.Point)
                    break;
                cf.CopyQty = CF_qty = qtyOpt.CurrentValue;
                res = cf.Get();
            }

            if (res == GetResult.Cancel)
                return Result.Cancel;

            DupeObjects(cf);

            doc.Views.Redraw();
            return Result.Success;
        }



        public void DupeObjects(CopyFit CFit)
        {
            var doc = CFit.obj[0].Object().Document;
            var oID = new List<Guid>();

            foreach (var o in CFit.obj)
                oID.Add(o.ObjectId);

            for (var i = 1; i < CFit.CopyQty; i++)
            {
                var tmp = new List<Guid>();
                foreach(var o in oID)
                    tmp.Add(doc.Objects.Transform(o, CFit.xform, false));

                oID = tmp;
            }
        }
    }

    public class CopyFit : GetPoint
    {
        public List<ObjRef> obj { get; set; }
        public int CopyQty { get; set; }
        public Point3d start { get; set; }
        public Transform xform { get; private set; }
        protected override void OnDynamicDraw(GetPointDrawEventArgs e)
        {
            base.OnDynamicDraw(e);

            var line = new Line(start, e.CurrentPoint);
            var len = line.Length / (CopyQty - 1);
            var curPt = line.PointAtLength(len);
            xform = Transform.Translation(new Line(start, curPt).Direction);

            e.Display.DrawLine(line, System.Drawing.Color.Aquamarine);

            // dup all curves
            var crv = new List<Curve>();
            var others = new List<Polyline>();
            foreach (var o in obj)
            {
                if (o.Curve() != null)
                    crv.Add(o.Curve().DuplicateCurve());
                else
                    others.Add(new Polyline(o.Geometry().GetBoundingBox(false).GetCorners()));
            }

            // draw all of them
            for (var i = 1; i < CopyQty; i++)
            {
                // Draw the curves
                foreach(var c in crv)
                {
                    c.Transform(xform);
                    e.Display.DrawCurve(c, System.Drawing.Color.DarkGreen);
                }

                // Draw the other objects
                foreach(var bb in others)
                {
                    bb.Transform(xform);
                    e.Display.DrawPolyline(bb, System.Drawing.Color.DarkSeaGreen);
                }
            }
        }
    }
}