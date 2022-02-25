using System;
using Rhino;
using Rhino.Commands;
using Rhino.UI.Gumball;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.Display;

namespace gjTools.Commands
{
    public class Nest_Interact_GB : Command
    {
        public Nest_Interact_GB()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static Nest_Interact_GB Instance { get; private set; }

        public override string EnglishName => "Nest_Interact_GB";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var interact = new NestInteract();
            var res = interact.Get(true);

            while(res != GetResult.Cancel)
                res = interact.Get(true);

            interact.DisableGumBalls();
            doc.Views.Redraw();
            return Result.Success;
        }
    }


    public class NestInteract : GetPoint
    {
        public Gball m_Gball_1 = new Gball(new Point3d(0, 0, 0));
        public Gball m_Gball_2 = new Gball(new Point3d(0, 10, 0));
        public Gball m_Gball_3 = new Gball(new Point3d(10, 0, 0));

        
        protected override void OnMouseMove(GetPointMouseEventArgs e)
        {
            MoveGumball(e, m_Gball_1);
            MoveGumball(e, m_Gball_2);
            MoveGumball(e, m_Gball_3);

            base.OnMouseMove(e);
        }
        protected override void OnMouseDown(GetPointMouseEventArgs e)
        {
            base.OnMouseDown(e);
            
            SetPickContext(e, m_Gball_1);
            SetPickContext(e, m_Gball_2);
            SetPickContext(e, m_Gball_2);
        }
        protected override void OnDynamicDraw(GetPointDrawEventArgs e)
        {
            base.OnDynamicDraw(e);
        }
        private void SetPickContext(GetPointMouseEventArgs e, Gball ball)
        {
            if (ball.PickMode() != GumballMode.None)
                return;
            ball.m_displayConduit.PickResult.SetToDefault();

            var pContext = new PickContext();
            pContext.View = e.Viewport.ParentView;
            pContext.PickStyle = PickStyle.PointPick;
            var xForm = e.Viewport.GetPickTransform(e.WindowPoint);
            pContext.SetPickTransform(xForm);
            Line pLine;
            e.Viewport.GetFrustumLine(e.WindowPoint.X, e.WindowPoint.Y, out pLine);
            pContext.PickLine = pLine;
            pContext.UpdateClippingPlanes();
            ball.m_displayConduit.PickGumball(pContext, this);
        }

        private void MoveGumball(GetPointMouseEventArgs e, Gball ball)
        {
            if (ball.PickMode() == GumballMode.None)
                return;

            ball.m_displayConduit.CheckShiftAndControlKeys();

            Line mLine;
            e.Viewport.GetFrustumLine(e.WindowPoint.X, e.WindowPoint.Y, out mLine);
            ball.m_displayConduit.UpdateGumball(e.Point, mLine);
        }

        public void DisableGumBalls()
        {
            m_Gball_1.DisableGumball();
            m_Gball_2.DisableGumball();
            m_Gball_3.DisableGumball();
        }
    }

    public class Gball
    {
        // all gumball objects i might need go here
        public GumballObject m_gball;
        private GumballAppearanceSettings m_settings;
        public GumballDisplayConduit m_displayConduit;

        public Gball (Point3d startPoint)
        {
            m_gball = new GumballObject();
            m_gball.SetFromPlane(new Plane(startPoint, Vector3d.ZAxis));

            m_settings = new GumballAppearanceSettings()
            {
                MenuEnabled = false,
                RelocateEnabled = true,
                RotateXEnabled = false,
                RotateYEnabled = false,
                RotateZEnabled = false,
                ScaleXEnabled = false,
                ScaleYEnabled = false, 
                ScaleZEnabled = false,
                TranslateZEnabled = false,
                TranslateZXEnabled = false,
                TranslateYZEnabled = false
            };

            m_displayConduit = new GumballDisplayConduit();
            m_displayConduit.SetBaseGumball(m_gball, m_settings);
            m_displayConduit.Enabled = true;
        }

        public void DisableGumball ()
        {
            m_displayConduit.Enabled = false;

            // Kill off the gumball
            m_displayConduit.Dispose();
            m_gball.Dispose();
        }

        public GumballMode PickMode()
        {
            return m_displayConduit.PickResult.Mode;
        }
    }
}