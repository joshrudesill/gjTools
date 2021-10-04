using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.DocObjects;
using Rhino.Input;
using Rhino.Input.Custom;

namespace gjTools.Commands
{
    public class XYDims : Command
    {
        public int dimlevel = 1;

        public XYDims()
        {
            Instance = this;
        }

        public static XYDims Instance { get; private set; }

        public override string EnglishName => "XYDims";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            if (RhinoGet.GetMultipleObjects("Select Objects", false, ObjectType.AnyObject, out ObjRef[] objs) != Result.Success)
                return Result.Cancel;

            var gp = new XYDimVisual(doc.DimStyles.Current, new List<ObjRef>(objs));
            gp.Get();

            if (gp.CommandResult() != Result.Success)
                return Result.Cancel;

            var lDims = gp.GetDimObjects();
            var parentlayer = doc.Layers[objs[0].Object().Attributes.LayerIndex];
            if (parentlayer.ParentLayerId != Guid.Empty)
                parentlayer = doc.Layers.FindId(parentlayer.ParentLayerId);

            var attr = new ObjectAttributes { LayerIndex = parentlayer.Index };
            doc.Objects.AddLinearDimension(lDims[0], attr);
            doc.Objects.AddLinearDimension(lDims[1], attr);

            doc.Views.Redraw();
            return Result.Success;
        }
    }



    public class XYDimVisual : GetPoint
    {
        public DimensionStyle ds { get; private set; }
        public LinearDimension VertDimension { get; private set; }
        public LinearDimension HorizDimension { get; private set; }

        private Box BB;
        private Box inf;

        private System.Drawing.Color Clr_Blu = System.Drawing.Color.DeepSkyBlue;

        public XYDimVisual(DimensionStyle DimStyle, List<ObjRef> Objs)
        {
            ds = DimStyle;
            ProcessSelection(Objs);
        }


        private void ProcessSelection(List<ObjRef> sel)
        {
            var tmpBB = BoundingBox.Empty;

            foreach (var s in sel)
            {
                var bb = s.Geometry().GetBoundingBox(true);
                tmpBB.Union(bb);
            }

            BB = new Box(tmpBB);
        }

        /// <summary>
        /// Gets the dim objects if there are any
        /// </summary>
        /// <returns></returns>
        public List<LinearDimension> GetDimObjects()
        {
            var cnr2 = inf.GetCorners();
            var cnr = BB.GetCorners();

            return new List<LinearDimension>
            {
                MakeDim(ds, new Line(cnr[0], cnr[3]), cnr2[0], true),
                MakeDim(ds, new Line(cnr[3], cnr[2]), cnr2[2], false)
            };
        }

        /// <summary>
        /// Tailored make dim for this command
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="l"></param>
        /// <param name="pt"></param>
        /// <param name="VorH"></param>
        /// <returns></returns>
        private LinearDimension MakeDim(DimensionStyle ds, Line l, Point3d pt, bool VorH)
        {
            var dm_Center = l.PointAtLength(l.Length / 2);

            if (VorH)
                dm_Center.X = pt.X;
            else
                dm_Center.Y = pt.Y;

            Plane p = new Plane(l.From, l.To, dm_Center);
            p.ClosestParameter(l.From, out double sX, out double sY);
            p.ClosestParameter(dm_Center, out double mX, out double mY);
            p.ClosestParameter(l.To, out double eX, out double eY);

            return new LinearDimension(p, new Point2d(sX, sY), new Point2d(eX, eY), new Point2d(mX, mY)) { Aligned = true, DimensionStyleId = ds.Id };
        }

        /// <summary>
        /// Custom Bounding visual for the dims
        /// </summary>
        /// <param name="e"></param>
        protected override void OnDynamicDraw(GetPointDrawEventArgs e)
        {
            base.OnDynamicDraw(e);
            e.Display.DrawBox(BB, Clr_Blu, 1);
            inf = new Box(BB);

            if (!BB.Contains(e.CurrentPoint))
            {
                double dist = BB.ClosestPoint(e.CurrentPoint).DistanceTo(e.CurrentPoint);

                // Inflate the box and draw it for now
                inf.Inflate(dist);
                e.Display.DrawBox(inf, Clr_Blu);
                var Icnrs = inf.GetCorners();

                // other data
                var arrLen = ds.ArrowLength;
                var txtH = ds.TextHeight;
                var dsScale = ds.DimensionScale;
                var lns = new List<Line>
                {
                    new Line(Icnrs[0], Icnrs[3]),
                    new Line(Icnrs[3], Icnrs[2])
                };

                
                e.Display.DrawArrowHead(lns[1].From, -lns[1].Direction, Clr_Blu, 0, arrLen * dsScale);
                e.Display.DrawArrowHead(lns[1].To, lns[1].Direction, Clr_Blu, 0, arrLen * dsScale);
                e.Display.Draw3dText(
                    "X.XX",
                    Clr_Blu,
                    new Plane(lns[1].PointAtLength(lns[1].Length / 2), Vector3d.ZAxis),
                    txtH * dsScale,
                    ds.Font.EnglishFaceName,
                    false,
                    false,
                    TextHorizontalAlignment.Center,
                    TextVerticalAlignment.Middle);

                e.Display.DrawArrowHead(lns[0].From, -lns[0].Direction, Clr_Blu, 0, arrLen * dsScale);
                e.Display.DrawArrowHead(lns[0].To, lns[0].Direction, Clr_Blu, 0, arrLen * dsScale);
                var rPlane = new Plane(lns[0].PointAtLength(lns[0].Length / 2), Vector3d.ZAxis);
                rPlane.Rotate(3.14 / 2, Vector3d.ZAxis);
                e.Display.Draw3dText(
                    "X.XX", 
                    Clr_Blu, 
                    rPlane, 
                    txtH * dsScale, 
                    ds.Font.EnglishFaceName, 
                    false, 
                    false, 
                    TextHorizontalAlignment.Center, 
                    TextVerticalAlignment.Middle);
            }
        }
    }
}