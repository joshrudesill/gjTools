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

            
            return Result.Success;
        }
    }


    public class FanWidget
    {
        public Circle m_Base;
        public Circle m_Rot;
        public Line m_BaseLine;
        public Line m_RotLine;
        public Line m_StartToBaseLine;

        public FanWidget(Point3d StartPoint, double XVal)
        {
            double radius = 0.5;

            // make the base circle widget
            m_Base = new Circle(StartPoint, radius);

            // create an invalid displacment line
            m_StartToBaseLine = new Line(StartPoint, StartPoint);
            
            // advance the x and make the rest of the widget
            StartPoint.X = XVal;
            m_Rot = new Circle(StartPoint, radius);
            m_BaseLine = new Line(m_Base.Center, m_Rot.Center);
            m_RotLine = new Line(m_Base.Center, m_Rot.Center);
        }
    }

    public class Fan : GetPoint
    {
        protected override void OnDynamicDraw(GetPointDrawEventArgs e)
        {
            base.OnDynamicDraw(e);
        }
        protected override void OnMouseDown(GetPointMouseEventArgs e)
        {
            base.OnMouseDown(e);
        }
    }
}