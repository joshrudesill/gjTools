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
            Rhino.Input.Custom.GetObject go = new Rhino.Input.Custom.GetObject();
            go.SetCommandPrompt("Select objects to get smallest rotation..");
            go.Get();
            var ro = go.Object(0);
            var vec = new Point3d(5, 5, 0);
            var xf = Transform.Rotation(Math.PI, vec);
            Guid id = doc.Objects.Transform(ro, xf, false);


            doc.Views.Redraw();
            return Result.Success;
        }
    }
}