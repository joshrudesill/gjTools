using System;
using System.Collections.Generic;
using Rhino.Input.Custom;
using Rhino.Geometry;
using Rhino.DocObjects;
using Rhino;

namespace Nest_Interact.Prog_Classes
{
    public struct BasePoints
    {
        public Point3d Base { get; set; }
        public Point3d Up { get; set; }
        public Point3d Right { get; set; }
        public Point3d NextCol { get; set; }
        public Point3d StackBase { get; set; }
    }

    public struct BoundBoxes
    {
        public BoundingBox Single { get; set; }
        public BoundingBox Stack1 { get; set; }
        public BoundingBox Stack2 { get; set; }
        public BoundingBox Stack34 { get; set; }
        public BoundingBox All { get; set; }

        public BoundBoxes(BoundingBox init)
        {
            Single = init;
            Stack1 = init;
            Stack2 = init;
            Stack34 = init;
            All = init;
        }
    }

    /// <summary>
    /// Manage the Layout Data
    /// </summary>
    public class StackData
    {
        // Layout Settings
        public double Height = 46.0;
        public double PartSpacing = 0.125;

        // Original Part Ref
        public List<ObjRef> OriginalPart = new List<ObjRef>();

        // Part info
        public BoundBoxes BBox = new BoundBoxes(BoundingBox.Empty);
        public List<Curve> Crv = new List<Curve>();
        public List<Curve> Offset = new List<Curve>();
        public BasePoints BasePts = new BasePoints();
        public int QtyUp;
        public int QtyAccross;

        // Duplicated Objects
        public List<Curve> Crv_Stack1 = new List<Curve>();
        public List<Curve> Crv_Stack2 = new List<Curve>();
        public List<Curve> Crv_Stack34 = new List<Curve>();

        // TextDots
        public TextDot TDot1 = new TextDot("Up", Point3d.Origin) { FontHeight = 14 };
        public TextDot TDot2 = new TextDot("Right", Point3d.Origin) { FontHeight = 14 };
        public TextDot TDot3 = new TextDot("Next Stack", Point3d.Origin) { FontHeight = 14 };

        // Other Data
        public RhinoDoc doc;
        public int PartLayer;

        /// <summary>
        /// Do this before anything else
        /// </summary>
        public void ProcessPart(RhinoDoc document, int LayerIndex)
        {
            // Set the part info
            doc = document;
            PartLayer = LayerIndex;

            Offset.Clear();
            var bb = BoundingBox.Empty;

            // construct offsets and bounding
            for(int i = 0; i < Crv.Count; i++)
            {
                var off = Crv[i].Offset(
                    new Point3d(-1000, -1000, 0),
                    Vector3d.ZAxis,
                    PartSpacing,
                    0.1,
                    CurveOffsetCornerStyle.Round )[0];
                
                bb.Union(Crv[i].GetBoundingBox(true));
                Offset.Add(off);
            }
            BBox.Single = bb;

            // How many will fit
            QtyUp = (int)(Height / BBox.Single.GetEdges()[1].Length) - 1;

            BasePts.Base = BBox.Single.Center;
            BasePts.Up = new Point3d(BBox.Single.Center.X, BBox.Single.Center.Y + BBox.Single.GetEdges()[1].Length, 0);
            BasePts.Right = new Point3d(
                BBox.Single.Corner(false, false, true).X + (BBox.Single.GetEdges()[0].Length / 2), 
                BBox.Single.Corner(false, false, true).Y, 
                0);

            // Set TextDots
            TDot1.Point = BasePts.Up;
            TDot2.Point = BasePts.Right;
        }

        /// <summary>
        /// Remove the Stored Duplicates
        /// </summary>
        public void ClearDuplicatedObjects()
        {
            Crv_Stack1.Clear();
            Crv_Stack2.Clear();
        }

        /// <summary>
        /// New Base point Left-Center
        /// </summary>
        /// <returns></returns>
        public Point3d GetStack1LeftCenter()
        {
            var bb = BoundingBox.Empty;
            foreach (var c in Crv_Stack1)
                bb.Union(c.GetBoundingBox(true));
            BBox.Stack1 = bb;

            BasePts.StackBase = new Point3d(BBox.Stack1.Min.X, BBox.Stack1.Center.Y, 0);
            return BasePts.StackBase;
        }

        /// <summary>
        /// Radian value to straiten the nest
        /// </summary>
        /// <returns></returns>
        public double StraitenRotValue()
        {
            var opposite = BasePts.StackBase.Y - BasePts.NextCol.Y;
            var hypot = BasePts.StackBase.DistanceTo(BasePts.NextCol);
            return Math.Sin(opposite / hypot);
        }

        /// <summary>
        /// Calc all fresh Boundry boxes
        /// </summary>
        public void CalcAllBounds()
        {
            var bb = BoundingBox.Empty;
            for (int i = 0; i < Crv_Stack1.Count; i++)
                bb.Union(Crv_Stack1[i].GetBoundingBox(true));
            BBox.Stack1 = bb;

            bb = BoundingBox.Empty;
            for (int i = 0; i < Crv_Stack2.Count; i++)
                bb.Union(Crv_Stack2[i].GetBoundingBox(true));
            BBox.Stack2 = bb;

            bb = BoundingBox.Empty;
            for (int i = 0; i < Crv_Stack34.Count; i++)
                bb.Union(Crv_Stack34[i].GetBoundingBox(true));
            BBox.Stack34 = bb;

            bb = BoundingBox.Empty;
            bb.Union(BBox.Stack1);
            bb.Union(BBox.Stack2);
            bb.Union(BBox.Stack34);
            BBox.All = bb;
        }
    }




    /// <summary>
    /// Custom GetPoint Class for Strait Stacking Nestings
    /// </summary>
    public class TwoStack : GetPoint
    {
        /// <summary>
        /// Required object data
        /// </summary>
        public StackData SData { get; set; }

        /// <summary>
        /// The current point being moved
        /// </summary>
        public int PointSelect = 0;

        // Colors
        private readonly System.Drawing.Color clr_Offset = System.Drawing.Color.Yellow;
        private readonly System.Drawing.Color clr_Part = System.Drawing.Color.DarkRed;
        private readonly System.Drawing.Color clr_DTFill = System.Drawing.Color.Black;
        private readonly System.Drawing.Color clr_DTText = System.Drawing.Color.White;
        private readonly System.Drawing.Color clr_DTBord = System.Drawing.Color.Orange;

        // 180 Rot
        private readonly double Rot = Math.PI;

        // Connection Lines
        private Line ln_ToTop = new Line();
        private Line ln_ToRight = new Line();
        private Line ln_ToNext = new Line();

        // Store the offsets
        private List<Curve> TmpOffsets = new List<Curve>();

        /// <summary>
        /// Current Phase level of the nest Process
        /// </summary>
        public int Phase = 1;

        /// <summary>
        /// Custom Draw event
        /// </summary>
        /// <param name="e"></param>
        protected override void OnDynamicDraw(GetPointDrawEventArgs e)
        {
            base.OnDynamicDraw(e);

            if (Phase == 1)
                Phase1(e);      // Initial stacks
            else if (Phase == 2)
                Phase2(e);      // Line up the next Column of parts
            else if (Phase == 3)
                Phase3(e);      // Final Phase, Fill the Sheet
        }

        /// <summary>
        /// Make Stacks 1 & 2 phase
        /// </summary>
        /// <param name="e"></param>
        private void Phase1(GetPointDrawEventArgs e)
        {
            // Clear offsets
            TmpOffsets.Clear();
            SData.ClearDuplicatedObjects();

            // Give control over a control point
            if (PointSelect == 1)
                SData.BasePts.Up = e.CurrentPoint;
            if (PointSelect == 2)
                SData.BasePts.Right = e.CurrentPoint;

            // update the textdots and draw them
            SData.TDot1.Point = SData.BasePts.Up;
            SData.TDot2.Point = SData.BasePts.Right;
            e.Display.DrawDot(SData.TDot1, clr_DTFill, clr_DTText, clr_DTBord);
            e.Display.DrawDot(SData.TDot2, clr_DTFill, clr_DTText, clr_DTBord);

            // Draw a line from base to points
            ln_ToRight.From = ln_ToTop.From = SData.BasePts.Base;
            ln_ToRight.To = SData.BasePts.Right;
            ln_ToTop.To = SData.BasePts.Up;
            e.Display.DrawLine(ln_ToTop, clr_DTBord);
            e.Display.DrawLine(ln_ToRight, clr_DTBord);

            // Transforms
            var xformUp = new Vector3d(SData.BasePts.Up - SData.BasePts.Base);
            var xformRight = new Vector3d(SData.BasePts.Right - SData.BasePts.Base);

            // Draw the objects
            for (int i = 0; i < SData.Crv.Count; i++)
            {
                // Draw orig offset
                e.Display.DrawCurve(SData.Offset[i], clr_Offset);

                // Dupe the objects
                Curve c_Off = SData.Offset[i].DuplicateCurve();
                Curve c_Crv = SData.Crv[i].DuplicateCurve();
                Curve c2_Off = SData.Offset[i].DuplicateCurve();
                Curve c2_Crv = SData.Crv[i].DuplicateCurve();

                // Stack 2 adjustment
                var center = c2_Crv.GetBoundingBox(true).Center;
                c2_Crv.Rotate(Rot, Vector3d.ZAxis, center);
                c2_Off.Rotate(Rot, Vector3d.ZAxis, center);
                c2_Crv.Translate(xformRight);
                c2_Off.Translate(xformRight);
                e.Display.DrawCurve(c2_Crv, clr_Part);
                e.Display.DrawCurve(c2_Off, clr_Offset);

                // add temp offsets
                TmpOffsets.Add(c_Off.DuplicateCurve());
                TmpOffsets.Add(c2_Off.DuplicateCurve());

                // add the base to the master list and dupe again
                SData.Crv_Stack1.Add(c_Crv.DuplicateCurve());
                SData.Crv_Stack2.Add(c2_Crv.DuplicateCurve());

                // Move Dupes around
                for (int ii = 1; ii <= SData.QtyUp; ii++)
                {
                    c_Crv.Translate(xformUp);
                    c2_Crv.Translate(xformUp);
                    c_Off.Translate(xformUp);
                    c2_Off.Translate(xformUp);
                    e.Display.DrawCurve(c_Crv, clr_Part);
                    e.Display.DrawCurve(c_Off, clr_Offset);
                    e.Display.DrawCurve(c2_Crv, clr_Part);
                    e.Display.DrawCurve(c2_Off, clr_Offset);

                    // Write the new object to stacks
                    SData.Crv_Stack1.Add(c_Crv.DuplicateCurve());
                    SData.Crv_Stack2.Add(c2_Crv.DuplicateCurve());
                    TmpOffsets.Add(c_Off.DuplicateCurve());
                    TmpOffsets.Add(c2_Off.DuplicateCurve());
                }
            }
        }

        /// <summary>
        /// Make Stacks 3 & 4 phase
        /// </summary>
        /// <param name="e"></param>
        private void Phase2(GetPointDrawEventArgs e)
        {
            // Reset the Dupes
            SData.Crv_Stack34.Clear();

            // Bounding and dupe objects
            for(int i = 0; i < SData.Crv_Stack2.Count; i++)
            {
                // Dupe to the 3rd Stack
                SData.Crv_Stack34.Add(SData.Crv_Stack1[i].DuplicateCurve());
                SData.Crv_Stack34.Add(SData.Crv_Stack2[i].DuplicateCurve());
            }

            // Draw the connection line
            ln_ToNext.From = SData.BasePts.StackBase;
            if (PointSelect == 1)
            {
                ln_ToNext.To = e.CurrentPoint;
                SData.BasePts.NextCol = e.CurrentPoint;
            }
            else
            {
                ln_ToNext.To = SData.BasePts.NextCol;
            }
            e.Display.DrawLine(ln_ToNext, clr_DTBord);

            // Write the text dot
            SData.TDot3.Point = SData.BasePts.NextCol;
            e.Display.DrawDot(SData.TDot3, clr_DTFill, clr_DTText, clr_DTBord);

            // Transform for Next Stack
            var xform = new Vector3d(ln_ToNext.To - ln_ToNext.From);

            // Apply the transform and draw
            for (int i = 0; i < SData.Crv_Stack34.Count; i++)
            {
                SData.Crv_Stack34[i].Translate(xform);
                e.Display.DrawCurve(SData.Crv_Stack34[i], clr_Part);
            }

            // Draw offsets
            for (int i = 0; i < TmpOffsets.Count; i++)
                e.Display.DrawCurve(TmpOffsets[i], clr_Offset);

            // Draw stack 1
            for (int i = 0; i < SData.Crv_Stack1.Count; i++)
                e.Display.DrawCurve(SData.Crv_Stack1[i], clr_Part);

            // Draw stack 2
            for (int i = 0; i < SData.Crv_Stack2.Count; i++)
                e.Display.DrawCurve(SData.Crv_Stack2[i], clr_Part);
        }

        /// <summary>
        /// Filling the layout in this phase
        /// </summary>
        /// <param name="e"></param>
        private void Phase3(GetPointDrawEventArgs e)
        {
            // Draw stack 1
            for (int i = 0; i < SData.Crv_Stack1.Count; i++)
                e.Display.DrawCurve(SData.Crv_Stack1[i], clr_Part);

            // Draw stack 2
            for (int i = 0; i < SData.Crv_Stack2.Count; i++)
                e.Display.DrawCurve(SData.Crv_Stack2[i], clr_Part);

            // Draw stack 3 & 4
            for (int i = 0; i < SData.Crv_Stack34.Count; i++)
                e.Display.DrawCurve(SData.Crv_Stack34[i], clr_Part);

            // for the sakes of speed and less clutter
            // Only draw a simple box to show how far to go
            Point3d bpt = SData.BBox.All.Min;
            double length = (SData.BBox.All.Max.X - SData.BBox.All.Min.X) / 2;
            double mouseLength = e.CurrentPoint.X - bpt.X;
            int qty = (int)(mouseLength / length);

            BoundingBox rect = new BoundingBox(
                SData.BBox.All.Min,
                new Point3d(SData.BBox.All.Min.X + (length * qty), SData.BBox.All.Min.Y + SData.Height, 0) );

            // Draw the rectangle
            e.Display.DrawBox(rect, clr_DTBord);

            // draw text inside
            e.Display.Draw2dText($"Qty: {qty} Stacks\n{(SData.Crv_Stack1.Count * 2 * qty) / SData.Crv.Count} Total Parts", 
                clr_DTFill, rect.Center, true, 25);

            // record the qty
            SData.QtyAccross = qty;
        }
    }
}
