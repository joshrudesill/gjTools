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

            var options = new List<string> { "plainFilm", "Printed", "Router" };
            var optiVal = new List<double> { 0.125, 0.25, 0.5 };
            var offset = 0.125;
            var gs = new GetString();
                gs.SetCommandPrompt("Offset Amount or Specify");
                gs.AcceptNumber(true, true);

            foreach (var o in options)
                gs.AddOption(o);

                gs.SetDefaultString(options[0]);
                gs.SetCommandPromptDefault(options[0]);
            var gType = gs.Get();

            // see what the shit-head typed
            if (gs.CommandResult() != Result.Success)
                return Result.Cancel;

            if (gType == Rhino.Input.GetResult.String)
            {
                if (options.Contains(gs.StringResult().Trim()))
                {
                    offset = optiVal[options.IndexOf(gs.StringResult().Trim())];
                }
                else
                {
                    RhinoApp.WriteLine("Not a Number, try again...");
                    return Result.Cancel;
                }
            }
            else if (gType == Rhino.Input.GetResult.Number)
            {
                offset = gs.Number();
            } else
            {
                return Result.Cancel;
            }

            var lt = new LayerTools(doc);
            var tmplayer = lt.CreateLayer("Temp", System.Drawing.Color.Bisque);

            foreach(var o in obj)
            {
                var offCrv = o.Curve().Offset(new Point3d(-10000, -10000, 0),
                    new Vector3d(0,0,1),
                    offset,
                    0.01,
                    CurveOffsetCornerStyle.Round);
                foreach (var oo in offCrv)
                {
                    Guid obGUID = doc.Objects.AddCurve(oo);
                    lt.AddObjectsToLayer(obGUID, tmplayer);
                }
            }

            RhinoApp.WriteLine("Number Chosen: " + offset);

            doc.Views.Redraw();
            return Result.Success;
        }

        
    }
}