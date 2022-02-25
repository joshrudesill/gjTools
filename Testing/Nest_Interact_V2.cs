using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.DocObjects;
using Rhino.Input;
using Rhino.Input.Custom;

namespace gjTools.Testing
{
    public class Nest_Interact_V2 : Command
    {
        public Nest_Interact_V2()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static Nest_Interact_V2 Instance { get; private set; }

        public override string EnglishName => "Nest_Interact_V2";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            if (RhinoGet.GetOneObject("Select an Object to Nest", false, ObjectType.Curve, out ObjRef obj) != Result.Success)
                return Result.Cancel;

            var NestData = new NestingData(obj, 0.125);
            var interact = new NestInteractGP(NestData);
            var res = GetResult.NoResult;

            while(true)
            {
                res = interact.Get(true);

                if (res == GetResult.Cancel)
                    break;

                if (res == GetResult.Point)
                {
                    interact.CheckMove();
                }

                if (res == GetResult.Nothing)
                {
                    // for now cancell the action
                    // later, move on to the next phase
                    break;
                }
            }

            doc.Views.Redraw();
            return Result.Success;
        }


        public class NestingData
        {
            // chosen object
            public ObjRef oRef;
            public Curve inputCrv;
            public Curve offsetCrv;

            // rotated version
            public Curve RotCrv;
            public Curve offsetRotCrv;

            // duplication vectors
            public Vector3d v_up;
            public Vector3d v_right;
            public Vector3d v_stackRight;

            // qtys
            public int numberUp;
            public int numberRight;

            public NestingData(ObjRef inputObj, double space)
            {
                oRef = inputObj;
                inputCrv = oRef.Curve();
                offsetCrv = inputCrv.Offset(new Point3d(-10000, 10000, 0), Vector3d.ZAxis, space, 0.05, CurveOffsetCornerStyle.Smooth)[0];

                var BB = inputCrv.GetBoundingBox(true);
                double pWidth = BB.GetEdges()[0].Length;
                double pHeight = BB.GetEdges()[1].Length;

                // Rotated version
                RotCrv = inputCrv.DuplicateCurve();
                RotCrv.Rotate(RhinoMath.ToRadians(180), Vector3d.ZAxis, BB.Center);
                offsetRotCrv = offsetCrv.DuplicateCurve();
                offsetRotCrv.Rotate(RhinoMath.ToRadians(180), Vector3d.ZAxis, BB.Center);

                // number up
                numberUp = (int)(46 / pHeight);
                numberRight = 1;

                // preset the vectors
                v_up = new Vector3d(0, pHeight, 0);
                v_right = new Vector3d(pWidth * 0.75, 0, 0);
                v_stackRight = new Vector3d(pWidth * 2, 0, 0);
            }
        }

        public class BallVectorWidget
        {
            // Points to maintain
            private Point3d BasePoint;
            private Point3d WidgetLocation;

            // Draw these objects
            public List<Curve> DrawObjects;

            public BallVectorWidget(Point3d BaseLocation, Point3d StartLoction)
            {
                BasePoint = BaseLocation;
                WidgetLocation = StartLoction;

                DrawObjects = new List<Curve>()
                {
                    NurbsCurve.CreateFromCircle(new Circle(BasePoint, 0.75)),
                    NurbsCurve.CreateFromCircle(new Circle(new Point3d(BasePoint.X - 0.75, BasePoint.Y - 0.75, 0), 0.2))
                };
            }

            public bool IsRelocateClick(Point3d TestPoint)
            {
                var contains = DrawObjects[1].Contains(TestPoint, Plane.WorldXY, 0.1);

                if (contains == PointContainment.Inside || contains == PointContainment.Coincident)
                    return true;

                return false;
            }

            public bool IsMoveClick(Point3d TestPoint)
            {
                var contains = DrawObjects[0].Contains(TestPoint, Plane.WorldXY, 0.1);

                if (contains == PointContainment.Inside || contains == PointContainment.Coincident)
                    return true;

                return false;
            }

            public void MoveBall(Point3d NewLocation)
            {
                DrawObjects[0].Translate(WidgetLocation - NewLocation);
                DrawObjects[1].Translate(WidgetLocation - NewLocation);
                WidgetLocation = NewLocation;
            }

            public void RelocateBall(Point3d NewLocation)
            {
                DrawObjects[0].Translate(BasePoint - NewLocation);
                DrawObjects[1].Translate(BasePoint - NewLocation);
                BasePoint = NewLocation;
            }

            public Vector3d Translation
            {
                get { return WidgetLocation - BasePoint; }
            }
        }

        public class NestInteractGP : GetPoint
        {
            // object data
            private NestingData NData;

            // keep track of the click drag events
            private bool sel1;
            private bool sel2;
            private BallVectorWidget ballUp;
            private BallVectorWidget ballRh;

            public NestInteractGP(NestingData nestdata)
            {
                NData = nestdata;
                var center = NData.inputCrv.GetBoundingBox(true).Center;

                SetCommandPrompt("Move objects");
                AcceptNothing(true);
                SetBasePoint(center, true);

                // testing ball
                ballUp = new BallVectorWidget(center, new Point3d(center.X, center.Y + 10, 0));
                ballRh = new BallVectorWidget(center, new Point3d(center.X + 10, center.Y, 0));
            }


            protected override void OnDynamicDraw(GetPointDrawEventArgs e)
            {
                Vector3d xForm = new Vector3d(0, 0, 0);
                if (sel2)
                    ballRh.MoveBall(e.CurrentPoint);
                else if (sel1)
                    ballUp.MoveBall(e.CurrentPoint);

                base.OnDynamicDraw(e);
            }

            protected override void OnMouseDown(GetPointMouseEventArgs e)
            {
                var check = ballRh.IsRelocateClick(e.Point);
                if (check)
                    sel2 = true;

                base.OnMouseDown(e);
            }


            public void CheckMove()
            {
                

                sel1 = sel2 = false;
            }
        }
    }
}