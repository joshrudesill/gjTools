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
            int li = 0;
            bool b = true;
            Rhino.UI.Dialogs.ShowSelectLayerDialog(ref li, "Test", false, false, ref b);
            var lo = new Helpers.LayerData(doc.Layers[li], doc);
            RhinoApp.WriteLine("hold");
            return Result.Success;
        }
    }
}