using System.Collections.Generic;
using System;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.DocObjects;
using Rhino.Input;
using Rhino.Input.Custom;

namespace gjTools.Testing
{
    public class NestInteract : Command
    {
        public NestInteract()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static NestInteract Instance { get; private set; }

        public override string EnglishName => "NestInteract";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // defaults
            double SheetWidth = 530;
            double PartSpacing = 0.125;

            // get user values
            if (RhinoGet.GetOneObject("Select an Object to Nest", false, ObjectType.Curve, out ObjRef obj) != Result.Success)
                return Result.Cancel;
            if (RhinoGet.GetNumber("Sheet Width", false, ref SheetWidth, 5, 1000) != Result.Success)
                return Result.Cancel;
            if (RhinoGet.GetNumber("Part Spacing", false, ref PartSpacing, 0.01, 1) != Result.Success)
                return Result.Cancel;

            var NestData = new NestingData(obj, PartSpacing, SheetWidth);
            var interact = new NestInteractGP(NestData);

            // start the input loop
            while(true)
            {
                GetResult res = interact.Get(true);

                // cancell  the command
                if (res == GetResult.Cancel)
                    break;

                // Point selected for input, reset the move flags
                if (res == GetResult.Point)
                {
                    interact.ResetMove();
                    continue;
                }

                // all string args should go here
                if (res == GetResult.String)
                {
                    if (interact.StringResult() == "+")
                        NestData.numberUp++;
                    else if (interact.StringResult() == "-")
                        NestData.numberUp--;
                    continue;
                }

                // nothing means that the nesting should be successfully completed
                if (res == GetResult.Nothing)
                {
                    interact.WriteVectors();
                    CreateNesting(NestData);
                    break;
                }
            }

            doc.Views.Redraw();
            return Result.Success;
        }

        /// <summary>
        /// Creates the nesting according to the data from the user
        /// </summary>
        /// <param name="NData"></param>
        public void CreateNesting(NestingData NData)
        {
            RhinoDoc doc = NData.oRef.Object().Document;
            ObjectAttributes attr = NData.oRef.Object().Attributes;
            Curve crv = NData.inputCrv;
            Curve rot = NData.RotCrv;

            // make some counters
            Vector3d vec_up = NData.upVect;
            Vector3d vec_rh = NData.rhVect;
            Vector3d vec_stk = NData.stackVect;
            BoundingBox bb = crv.GetBoundingBox(true);

            // apply some corrective rotation
            double correctiveRotation = Vector3d.VectorAngle(Vector3d.XAxis, vec_stk);
            correctiveRotation *= (vec_stk.Y > 0) ? -1 : 1;
            vec_up.Rotate(correctiveRotation, Vector3d.ZAxis);
            vec_rh.Rotate(correctiveRotation, Vector3d.ZAxis);
            vec_stk.Rotate(correctiveRotation, Vector3d.ZAxis);
            crv.Rotate(correctiveRotation, Vector3d.ZAxis, bb.Center);
            rot.Rotate(correctiveRotation, Vector3d.ZAxis, bb.Center);

            // stack the parts horizontally
            while (true)
            {
                Guid dup = doc.Objects.AddCurve(crv, attr);
                for (int i = 0; i < NData.numberUp; i++)
                    dup = doc.Objects.Transform(dup, Transform.Translation(vec_up), false);

                // move the rotated curve to the correct position.
                rot.Translate(vec_rh);

                dup = doc.Objects.AddCurve(rot, attr);
                for (int i = 0; i < NData.numberUp; i++)
                    dup = doc.Objects.Transform(dup, Transform.Translation(vec_up), false);

                rot.Translate(vec_stk - vec_rh);
                crv.Translate(vec_stk);

                // check to see if the limit is reached
                bb.Union(crv.GetBoundingBox(true));
                if (bb.GetEdges()[0].Length > NData.LayoutLength)
                    break;
            }

            // Move the original to the top center
            doc.Objects.Transform(NData.oRef, Transform.Translation(bb.GetEdges()[0].Length / 2, 75, 0), true);
        }




        /// <summary>
        /// Houses the part data to recreate the Layout
        /// </summary>
        public class NestingData
        {
            // chosen object
            public ObjRef oRef;
            public Curve inputCrv;
            public Curve offsetCrv;

            // rotated version
            public Curve RotCrv;
            public Curve offsetRotCrv;

            // qtys
            public int numberUp;
            public double LayoutLength;

            // output translation data
            public Vector3d upVect;
            public Vector3d rhVect;
            public Vector3d stackVect;

            public NestingData(ObjRef inputObj, double space, double layoutlength)
            {
                oRef = inputObj;
                inputCrv = oRef.Curve();
                offsetCrv = inputCrv.Offset(new Point3d(-10000, 10000, 0), Vector3d.ZAxis, space, 0.05, CurveOffsetCornerStyle.Smooth)[0];

                var BB = inputCrv.GetBoundingBox(true);
                double pWidth = BB.GetEdges()[0].Length;
                double pHeight = BB.GetEdges()[1].Length;
                LayoutLength = layoutlength;

                // Rotated version
                RotCrv = inputCrv.DuplicateCurve();
                RotCrv.Rotate(RhinoMath.ToRadians(180), Vector3d.ZAxis, BB.Center);
                offsetRotCrv = offsetCrv.DuplicateCurve();
                offsetRotCrv.Rotate(RhinoMath.ToRadians(180), Vector3d.ZAxis, BB.Center);

                // number up
                numberUp = (int)(46 / pHeight);
            }
        }

        /// <summary>
        /// Home-Made Gumball style widget for the NestInteractGP
        /// </summary>
        public class BallVectorWidget
        {
            // Points to maintain
            private Point3d BasePoint;
            private Point3d WidgetLocation;
            private Point3d WidgetRelLocation;

            // Draw these objects
            public List<Curve> DrawObjects;

            // object sizes
            private const double radius_LG = 0.75;
            private const double radius_SM = 0.20;

            public BallVectorWidget(Point3d BaseLocation, Point3d StartLoction)
            {
                BasePoint = BaseLocation;
                WidgetLocation = StartLoction;
                WidgetRelLocation = new Point3d(StartLoction.X - radius_LG, StartLoction.Y - radius_LG, 0);

                DrawObjects = new List<Curve>()
                {
                    NurbsCurve.CreateFromCircle(new Circle(StartLoction, radius_LG)),
                    NurbsCurve.CreateFromCircle(new Circle(WidgetRelLocation, radius_SM))
                };
            }

            /// <summary>
            /// is the click inside the small ball to relocate but not move the widget
            /// </summary>
            /// <param name="TestPoint"></param>
            /// <returns></returns>
            public bool IsRelocateClick(Point3d TestPoint)
            {
                var dist = WidgetRelLocation.DistanceTo(TestPoint);

                if (dist <= radius_SM)
                    return true;

                return false;
            }

            /// <summary>
            /// is the click inside the large ball to move the objects
            /// </summary>
            /// <param name="TestPoint"></param>
            /// <returns></returns>
            public bool IsMoveClick(Point3d TestPoint)
            {
                var dist = WidgetLocation.DistanceTo(TestPoint);

                if (dist <= radius_LG)
                    return true;

                return false;
            }

            /// <summary>
            /// Move the balls and objects associated with it
            /// </summary>
            /// <param name="NewLocation"></param>
            public void MoveBall(Point3d NewLocation)
            {
                DrawObjects[0].Translate(NewLocation - WidgetLocation);
                DrawObjects[1].Translate(NewLocation - WidgetLocation);
                WidgetLocation = NewLocation;
                WidgetRelLocation = DrawObjects[1].GetBoundingBox(true).Center;
            }

            /// <summary>
            /// Move the balls but NOT the objects associated with it
            /// </summary>
            /// <param name="NewLocation"></param>
            public void RelocateBall(Point3d NewLocation)
            {
                var xForm = Transform.Translation(NewLocation - WidgetRelLocation);

                DrawObjects[0].Transform(xForm);
                DrawObjects[1].Transform(xForm);
                BasePoint.Transform(xForm);
                
                WidgetRelLocation = NewLocation;
                WidgetLocation = DrawObjects[0].GetBoundingBox(true).Center;
            }

            /// <summary>
            /// the total movement made with this object
            /// </summary>
            public Vector3d Translation
            {
                get { return WidgetLocation - BasePoint; }
            }
        }

        /// <summary>
        /// Custom Rhino GetPoint
        /// </summary>
        public class NestInteractGP : GetPoint
        {
            // object data
            private NestingData NData;

            // keep track of the click drag events
            // Selection and Relocate checkers
            private bool sel1 = false;
            private bool rel1 = false;
            private BallVectorWidget ballUp;

            private bool sel2 = false;
            private bool rel2 = false;
            private BallVectorWidget ballRh;

            private bool sel3 = false;
            private bool rel3 = false;
            private BallVectorWidget ballStk2;

            public NestInteractGP(NestingData nestdata)
            {
                NData = nestdata;
                var BB = NData.inputCrv.GetBoundingBox(true);
                var edges = BB.GetEdges();
                var center = BB.Center;

                SetCommandPrompt("Move objects");
                AcceptNothing(true);
                AcceptString(true);
                SetBasePoint(center, true);

                // testing ball
                ballUp = new BallVectorWidget(center, new Point3d(center.X, center.Y + 10, 0));
                ballRh = new BallVectorWidget(center, new Point3d(center.X + (edges[0].Length * 0.5), center.Y, 0));
                ballStk2 = new BallVectorWidget(center, new Point3d(center.X + (edges[0].Length * 1.1), center.Y, 0));
            }

            // Colors for the draw objects
            private System.Drawing.Color clr_obj = System.Drawing.Color.Red;
            private System.Drawing.Color clr_off = System.Drawing.Color.Yellow;
            private System.Drawing.Color clr_wid = System.Drawing.Color.Blue;

            protected override void OnDynamicDraw(GetPointDrawEventArgs e)
            {
                DrawStackOne(e);
                DrawStackTwo(e);

                base.OnDynamicDraw(e);
            }

            protected override void OnMouseDown(GetPointMouseEventArgs e)
            {
                if (ballUp.IsMoveClick(e.Point))
                    sel1 = true;
                else if (ballUp.IsRelocateClick(e.Point))
                    rel1 = true;
                else if (ballRh.IsMoveClick(e.Point))
                    sel2 = true;
                else if (ballRh.IsRelocateClick(e.Point))
                    rel2 = true;
                else if (ballStk2.IsMoveClick(e.Point))
                    sel3 = true;
                else if (ballStk2.IsRelocateClick(e.Point))
                    rel3 = true;

                base.OnMouseDown(e);
            }

            /// <summary>
            /// Unsets the flags that signify that a widget is to move
            /// </summary>
            public void ResetMove()
            {
                sel1 = sel2 = rel1 = rel2 = sel3 = rel3 = false;
            }

            /// <summary>
            /// Write all the vectors to the Nest Data object
            /// </summary>
            public void WriteVectors()
            {
                NData.upVect = ballUp.Translation;
                NData.rhVect = ballRh.Translation;
                NData.stackVect = ballStk2.Translation;
            }

            private void DrawStackOne(GetPointDrawEventArgs e)
            {
                // check if the widgets are moving
                if (sel1)
                    ballUp.MoveBall(e.CurrentPoint);
                else if (rel1)
                    ballUp.RelocateBall(e.CurrentPoint);
                else if (sel2)
                    ballRh.MoveBall(e.CurrentPoint);
                else if (rel2)
                    ballRh.RelocateBall(e.CurrentPoint);

                // Draw the widgets
                e.Display.DrawCurve(ballUp.DrawObjects[0], clr_wid);
                e.Display.DrawCurve(ballUp.DrawObjects[1], clr_wid);
                e.Display.DrawCurve(ballRh.DrawObjects[0], clr_wid);
                e.Display.DrawCurve(ballRh.DrawObjects[1], clr_wid);

                // draw initial objects
                var upCrv = NData.inputCrv.DuplicateCurve();
                var upCrvOff = NData.offsetCrv.DuplicateCurve();
                e.Display.DrawCurve(upCrv, clr_obj);
                e.Display.DrawCurve(upCrvOff, clr_off);

                // translate the rh part
                var rhCrv = NData.RotCrv.DuplicateCurve();
                var rhCrvOff = NData.offsetRotCrv.DuplicateCurve();
                rhCrv.Translate(ballRh.Translation);
                rhCrvOff.Translate(ballRh.Translation);

                // draw the rh objects
                e.Display.DrawCurve(rhCrv, clr_obj);
                e.Display.DrawCurve(rhCrvOff, clr_off);

                // draw the remaining dupes
                for (int i = 0; i < NData.numberUp; i++)
                {
                    // Move the objects
                    upCrv.Translate(ballUp.Translation);
                    upCrvOff.Translate(ballUp.Translation);
                    rhCrv.Translate(ballUp.Translation);
                    rhCrvOff.Translate(ballUp.Translation);

                    // Draw the objects
                    e.Display.DrawCurve(upCrv, clr_obj);
                    e.Display.DrawCurve(upCrvOff, clr_off);
                    e.Display.DrawCurve(rhCrv, clr_obj);
                    e.Display.DrawCurve(rhCrvOff, clr_off);
                }

                // dispose of overhead
                rhCrv.Dispose();
                rhCrvOff.Dispose();
                upCrv.Dispose();
                upCrvOff.Dispose();
            }

            private void DrawStackTwo(GetPointDrawEventArgs e)
            {
                // check if widget is to move
                if (sel3)
                    ballStk2.MoveBall(e.CurrentPoint);
                else if (rel3)
                    ballStk2.RelocateBall(e.CurrentPoint);

                // draw the widget
                e.Display.DrawCurve(ballStk2.DrawObjects[0], clr_wid);
                e.Display.DrawCurve(ballStk2.DrawObjects[1], clr_wid);

                // Create initials
                var inpBase = NData.inputCrv.DuplicateCurve();
                var inpBaseRot = NData.RotCrv.DuplicateCurve();
                inpBase.Translate(ballStk2.Translation);
                inpBaseRot.Translate(ballStk2.Translation + ballRh.Translation);

                // Draw initial objects
                e.Display.DrawCurve(inpBase, clr_obj);
                e.Display.DrawCurve(inpBaseRot, clr_obj);

                // draw dupes
                for (int i = 0; i < NData.numberUp; i++)
                {
                    inpBase.Translate(ballUp.Translation);
                    inpBaseRot.Translate(ballUp.Translation);

                    // Draw Dupe objects
                    e.Display.DrawCurve(inpBase, clr_obj);
                    e.Display.DrawCurve(inpBaseRot, clr_obj);
                }

                // cleanup resources
                inpBase.Dispose();
                inpBaseRot.Dispose();
            }
        }
    }
}