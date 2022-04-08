using System;
using Rhino;
using Rhino.Commands;
using Rhino.Input.Custom;
using System.Collections.Generic;
using Rhino.Geometry;

namespace gjTools.Commands
{
    public class PartOffset : Command
    {
        public PartOffset()
        {
            Instance = this;
        }
        public static PartOffset Instance { get; private set; }
        public override string EnglishName => "PartOffset";
        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var options = new List<string> { "FilmPlain", "Printed", "Router" };
            var optiVal = new List<double> { 0.125, 0.25, 0.5 };
            var offset = 0.125;

            // get objects
            var go = new GetObject();
                go.SetCommandPrompt("Select Objects to Offset");
                go.GeometryFilter = Rhino.DocObjects.ObjectType.Curve;
                go.AlreadySelectedObjectSelect = true;
                go.GetMultiple(1, 0);

            if (go.CommandResult() != Result.Success)
                return go.CommandResult();

            var obj = new List<Rhino.DocObjects.ObjRef>();
            foreach (var o in go.Objects())
                obj.Add(o);

            // get option or offset value
            var gs = new GetString();
                gs.SetCommandPrompt("Offset Amount or Specify");
                gs.AcceptNumber(true, true);

            foreach (var o in options)
                gs.AddOption(o);

                gs.SetDefaultString(options[0]);
                gs.SetCommandPromptDefault(options[0]);
                gs.AcceptNothing(true);
            var gType = gs.Get();

            if (gs.CommandResult() != Result.Success)
                return Result.Cancel;

            // see what asshat typed
            if (gType == Rhino.Input.GetResult.Option)
                offset = optiVal[gs.OptionIndex() - 1];
            else if (gType == Rhino.Input.GetResult.Number)
                offset = gs.Number();
            else if (gType == Rhino.Input.GetResult.Nothing)
                offset = optiVal[0];
            else
                return Result.Cancel;

            var lt = new LayerTools(doc);
            var tmplayer = lt.CreateLayer("Temp", System.Drawing.Color.FromArgb(255,150,140,50));

            // Add the actual offset
            foreach(var o in obj)
            {
                var offCrv = o.Curve().Offset(new Point3d(-100000, -100000, 0),
                    new Vector3d(0,0,1),
                    offset,
                    0.05,
                    CurveOffsetCornerStyle.Round);
                foreach (var oo in offCrv)
                {
                    Guid obGUID = doc.Objects.AddCurve(oo);
                    lt.AddObjectsToLayer(obGUID, tmplayer);
                }
            }

            doc.Views.Redraw();
            return Result.Success;
        }
    }
}