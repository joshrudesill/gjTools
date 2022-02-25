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
        private int CF_qty = 3;

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            if (RhinoGet.GetMultipleObjects("Select Objects", false, ObjectType.AnyObject, out ObjRef[] obj) != Result.Success)
                return Result.Cancel;

            if (RhinoGet.GetPoint("Start Point", false, out Point3d start) != Result.Success)
                return Result.Cancel;

            // Custom GetPoint
            var cf = new CopyFit(CF_qty, start, obj);
            var res = cf.Get();

            while (true)
            {
                if (res == GetResult.Cancel)
                    return Result.Cancel;
                else if (res == GetResult.Point)
                    break;
                else if (res == GetResult.Option)
                    CF_qty = cf.OptionChosen();

                res = cf.Get();
            }

            // Create the objects
            DupeObjects(cf);

            doc.Views.Redraw();
            return Result.Success;
        }



        private void DupeObjects(CopyFit CFit)
        {
            // Get the document
            var doc = CFit.obj[0].Object().Document;
            var lastItem = new List<Guid>(CFit.obj.Count);

            // populate the guid list
            foreach (var item in CFit.obj)
                lastItem.Add(item.ObjectId);

            // loop through the items and transform duplicate them
            for (var i = 1; i < CFit.CopyQty; i++)
                for (int ii = 0; ii < CFit.obj.Count; ii++)
                    lastItem[ii] = doc.Objects.Transform(lastItem[ii], CFit.xform, false);
        }
    }

    public class CopyFit : GetPoint
    {
        public List<ObjRef> obj { get; set; }
        public int CopyQty { get; set; }
        public Point3d start { get; set; }
        public Transform xform { get; private set; }

        private OptionInteger q_Option;

        /// <summary>
        /// Construct the object and setup the Get parameters
        /// </summary>
        /// <param name="initialQty"></param>
        /// <param name="intialPoint"></param>
        public CopyFit(int i_Qty, Point3d i_Point, ObjRef[] i_Objects)
        {
            SetCommandPrompt("End Point");

            // Create the option to change QTY
            CopyQty = i_Qty;
            q_Option = new OptionInteger(CopyQty, true, 2);
            AddOptionInteger("Qty", ref q_Option);

            // Setup the geometry
            start = i_Point;
            SetBasePoint(start, true);
            obj = new List<ObjRef>(i_Objects);
        }

        /// <summary>
        /// Simply updates the qty and returns it
        /// </summary>
        /// <returns></returns>
        public int OptionChosen()
        {
            CopyQty = q_Option.CurrentValue;
            return CopyQty;
        }

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