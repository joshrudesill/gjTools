using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Input;
using Rhino.Input.Custom;
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

        public bool overallDimOption = false;
        public bool centerOnlyOption = false;

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            if (RhinoGet.GetMultipleObjects("Objects", false, ObjectType.Curve, out ObjRef[] obj) != Result.Success)
                return Result.Cancel;

            // get the bounding of all objects
            var bb = new List<BoundingBox>();
            foreach(var o in obj)
                bb.Add(o.Curve().GetBoundingBox(true));

            var overallDim = new OptionToggle(overallDimOption, "Individual", "Overall");
            var centerOnly = new OptionToggle(centerOnlyOption, "Bounds", "Centers");
            var gp = new CustomDynamicDims();
                gp.ProcessBounds(bb);
                gp.onlyCenters = centerOnly.CurrentValue;
                gp.overallDim = overallDim.CurrentValue;
                gp.AddOptionToggle("DimOutput", ref overallDim);
                gp.AddOptionToggle("DimType", ref centerOnly);
                gp.SetCommandPrompt("Place Dims");
                gp.ds = doc.DimStyles.Current;
            var res = gp.Get();

            while (true)
            {
                if (res == GetResult.Point)
                    break;
                if (res == GetResult.Cancel)
                    return Result.Cancel;
                if (res == GetResult.Option)
                {
                    gp.onlyCenters = centerOnlyOption = centerOnly.CurrentValue;
                    gp.overallDim = overallDimOption = overallDim.CurrentValue;
                    res = gp.Get();
                }
            }

            if (centerOnlyOption && bb.Count < 2)
            {
                RhinoApp.WriteLine("Must have more than one Object for Center to Center Option");
                return Result.Cancel;
            }

            var lay = doc.Layers[obj[0].Object().Attributes.LayerIndex];
            if (lay.FullPath.Contains("::"))
                lay = doc.Layers.FindId(lay.ParentLayerId);

            AddDimensions(doc, lay, gp);

            doc.Views.Redraw();
            return Result.Success;
        }





        /// <summary>
        /// Makes the dims and weeds out duplicates
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="parentLayer"></param>
        /// <param name="gp"></param>
        /// <returns></returns>
        private bool AddDimensions(RhinoDoc doc, Layer parentLayer, CustomDynamicDims gp) 
        {
            var projectedLines = new List<Line>();
            var uniqueLines = new List<Line>();
            var pt = gp.Point();
            var baseLine = gp.M_Edge[(int)gp.Dir];
            bool rotated = false;

            // project the lines flat for comparison
            foreach(var l in gp._CurrentDimPoints())
            {
                if (gp.Dir == CustomDynamicDims.Side.right || gp.Dir == CustomDynamicDims.Side.left)
                {
                    rotated = true;
                    projectedLines.Add(new Line(new Point3d(baseLine.FromX, l.FromY, 0), new Point3d(baseLine.ToX, l.ToY, 0)));
                }
                else
                    projectedLines.Add(new Line(new Point3d(l.FromX, baseLine.FromY, 0), new Point3d(l.ToX, baseLine.ToY, 0)));
            }

            //  for center mode
            var totalLengthbb = projectedLines[0].BoundingBox;
            foreach (var l in projectedLines)
                totalLengthbb.Union(l.BoundingBox);
            double totalLength = (rotated) ? totalLengthbb.GetEdges()[1].Length : totalLengthbb.GetEdges()[0].Length;


            // compare them
            bool match = false;
            for (var i = 0; i < projectedLines.Count; i++)
            {
                for(var ii = 0;ii < projectedLines.Count; ii++)
                {
                    if (i == ii)
                        continue;
                    if (projectedLines[i].Equals(projectedLines[ii]))
                    {
                        match = true;
                        break;
                    }
                }

                if (!match || (match && !uniqueLines.Contains(projectedLines[i])))
                    if (!(gp.onlyCenters && projectedLines[i].Length == totalLength && projectedLines.Count > 2))
                        uniqueLines.Add(projectedLines[i]);
                match = false;
            }

            // Make the dims
            var attr = new ObjectAttributes { LayerIndex = parentLayer.Index };
            foreach (var l in uniqueLines)
                doc.Objects.AddLinearDimension(MakeDim(gp.ds, l, pt, rotated), attr);

            return true;
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
    }

    

    public class CustomDynamicDims : GetPoint
    {
        private BoundingBox MasterBB;
        public List<Line> M_Edge { get; private set; }
        public DimensionStyle ds;

        // list( list(start, end, mid), ... )
        private List<Line> DimPointsLH = new List<Line>();
        private List<Line> DimPointsRH = new List<Line>();
        private List<Line> DimPointsTop = new List<Line>();
        private List<Line> DimPointsBott = new List<Line>();

        // Complete List for Dimensions
        private List<Line> CurrentDimPoints;
        public bool overallDim { get; set; }
        public bool onlyCenters { get; set; }
        public enum Side { bottom, right, top, left }
        public Side Dir { get; private set; }

        public List<Line> _CurrentDimPoints()
        {
            // overrides all other outputs
            if (onlyCenters)
            {
                var centerLines = new List<Line>();
                for(var i = 0; i< CurrentDimPoints.Count; i++)
                {
                    if ( i + 1 < CurrentDimPoints.Count)
                    {
                        centerLines.Add(new Line(
                            CurrentDimPoints[i].PointAtLength(CurrentDimPoints[i].Length / 2),
                            CurrentDimPoints[i+1].PointAtLength(CurrentDimPoints[i+1].Length / 2)
                        ));
                    }
                }
                return centerLines;
            }
            if (overallDim)
                return new List<Line> { M_Edge[(int)Dir] };
            return CurrentDimPoints;
        }

        public void ProcessBounds(List<BoundingBox> bboxes)
        {
            MasterBB = bboxes[0];
            foreach (var b in bboxes)
            {
                var edges = b.GetEdges();

                DimPointsLH.Add(edges[3]);
                DimPointsRH.Add(edges[1]);
                DimPointsTop.Add(edges[2]);
                DimPointsBott.Add(edges[0]);

                MasterBB.Union(b);
            }

            M_Edge = new List<Line>( MasterBB.GetEdges() );
        }

        protected override void OnDynamicDraw(GetPointDrawEventArgs e)
        {
            base.OnDynamicDraw(e);

            // Determine what side the mouse is on
            if (!MasterBB.Contains(e.CurrentPoint))  // Make sure the mouse is not in the master box
            {
                if (MasterBB.Min.Y > e.CurrentPoint.Y || MasterBB.Max.Y < e.CurrentPoint.Y)
                {   // top or bottom
                    if (MasterBB.Max.Y < e.CurrentPoint.Y)
                        CurrentDimPoints = DrawPreview(DimPointsTop, Side.top);
                    else
                        CurrentDimPoints = DrawPreview(DimPointsBott, Side.bottom);
                }
                else
                {   // left or right
                    if (MasterBB.Min.X > e.CurrentPoint.X)
                        CurrentDimPoints = DrawPreview(DimPointsLH, Side.left);
                    else
                        CurrentDimPoints = DrawPreview(DimPointsRH, Side.right);
                }
            }

            List<Line> DrawPreview(List<Line> lines, Side dir)
            {
                Dir = dir;
                Line edge = M_Edge[(int)dir];
                Point3d cent = edge.PointAtLength(edge.Length / 2);
                Line ext1;
                Line ext2;

                if (dir == Side.top || dir == Side.right)
                    edge.Flip();

                // Text entity
                var txt = TextEntity.Create( $"{Math.Round(edge.Length, 2)}", new Plane(cent, Vector3d.ZAxis), ds, true, 0, 0);
                    txt.TextHeight = ds.TextHeight * ds.DimensionScale;
                    txt.Justification = TextJustification.MiddleCenter;
                    txt.MaskEnabled = true;
                    txt.MaskFrame = DimensionStyle.MaskFrame.RectFrame;
                    txt.MaskOffset = txt.TextHeight / 4;
                    txt.MaskColorSource = DimensionStyle.MaskType.MaskColor;
                    txt.MaskUsesViewportColor = true;

                // Draw the preview
                if (dir == Side.left || dir == Side.right)
                {
                    cent.X = e.CurrentPoint.X;
                    txt.PlainText += "h";
                    txt.Plane = new Plane(cent, Vector3d.ZAxis);
                    ext1 = new Line(edge.To, new Point3d(cent.X, edge.ToY, 0));
                    ext2 = new Line(edge.From, new Point3d(cent.X, edge.FromY, 0));
                }
                else
                {
                    cent.Y = e.CurrentPoint.Y;
                    txt.PlainText += "w";
                    txt.Plane = new Plane(cent, Vector3d.ZAxis);
                    ext1 = new Line(edge.To, new Point3d(edge.ToX, cent.Y, 0));
                    ext2 = new Line(edge.From, new Point3d(edge.FromX, cent.Y, 0));
                }

                // draw the dim
                var color = System.Drawing.Color.Crimson;
                e.Display.DrawText(txt, color);
                e.Display.DrawArrow(new Line(cent, ext1.To), color, 15, 0);
                e.Display.DrawArrow(new Line(cent, ext2.To), color, 15, 0);

                ext1.Length += 0.75;
                ext2.Length += 0.75;

                e.Display.DrawLine(ext1, color, 2);
                e.Display.DrawLine(ext2, color, 2);

                return lines;
            }
        }
    }
}