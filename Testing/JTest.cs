using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
namespace gjTools
{
    public class JTest : Command
    {
        public JTest()
        {
            Instance = this;
        }

        public static JTest Instance { get; private set; }
        public override string EnglishName => "asdf";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            Point3d p;
            var e = new IMouseCallback();
            var q = Rhino.Input.RhinoGet.GetPoint("t", true, out p);
            var r = e.Get();
            return Result.Success;
        }
    }
    public class IMouseCallback : Rhino.Input.Custom.GetPoint
    {
        protected override void OnMouseDown(Rhino.Input.Custom.GetPointMouseEventArgs e)
        {
               RhinoApp.WriteLine("MOUSE DOWN");
        }
    }
}