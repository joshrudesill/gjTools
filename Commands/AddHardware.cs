using System;
using Rhino;
using Rhino.Commands;
using System.Collections.Generic;
using Rhino.Geometry;
using Rhino.DocObjects;
using Rhino.Input;
using Rhino.Input.Custom;

namespace gjTools.Commands
{
    public class AddHardware : Command
    {
        public AddHardware()
        {
            Instance = this;
        }
        public static AddHardware Instance { get; private set; }

        public override string EnglishName => "AddHardware";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var hardwares = new List<string> { "Cleats", "American Girl Cleats", "Tape" };
            var type = Rhino.UI.Dialogs.ShowListBox("Add Hardware", "Choose a type of hardware to add..", hardwares);

            switch(hardwares.IndexOf((string)type))
            {
                case 0: 
                    addCleats();
                    break;
                case 1: 
                    addAmGirlCleats();
                    break;
                case 2:
                    AddTape(doc);
                    break;
            }
            return Result.Success;
        }


        public void AddTape(RhinoDoc doc)
        {
            var disp = new Rhino.Display.CustomDisplay(true);
            var tapeW = new OptionDouble(0.5); 
                tapeW.CurrentValue = tapeW.InitialValue;
            var offset = new OptionDouble(0.125);
                offset.CurrentValue = offset.InitialValue;
            var orient = new OptionToggle(false, "Vertical", "Horizontal");
                orient.CurrentValue = orient.InitialValue;
            var qty = new OptionInteger(2);
                qty.CurrentValue = qty.InitialValue;
            Curve obj = null;
            var res = GetResult.NoResult;

            var go = new GetObject() { GeometryFilter = ObjectType.Curve };
                go.SetCommandPrompt("Select Objects");
                go.AcceptNothing(true);
                go.AddOptionDouble("TapeWidth", ref tapeW, "New Tape Width");
                go.AddOptionDouble("OffsetEdges", ref offset, "Set Offset");
                go.AddOptionInteger("Qty", ref qty, "Qty of Tape");
                go.AddOptionToggle("Orientation", ref orient);

            res = go.Get();

            while (true)
            {
                if (res == GetResult.Nothing || res == GetResult.Cancel)
                {
                    disp.Dispose();
                    break;
                }

                // Collect object and deselect it to continue loop
                if (res == GetResult.Object)
                {
                    obj = go.Object(0).Curve();
                    doc.Objects.UnselectAll();
                }

                if (obj != null)
                {
                    disp.Clear();
                    disp.AddCurve(obj, System.Drawing.Color.Red, 1);

                    foreach (var r in addTape(obj))
                    {
                        var lines = r.GetEdges();
                        for (var i = 0; i <= 4; i++)
                            disp.AddLine(lines[i], System.Drawing.Color.Blue, 1);
                    }
                }

                res = go.Get();
            }

            disp.Dispose();

            List<BoundingBox> addTape(Curve crv)
            {
                var tapes = new List<BoundingBox>();
                var bb = crv.GetBoundingBox(true);
                    
                double space = (bb.GetEdges()[0].Length - tapeW.CurrentValue) / qty.CurrentValue;

                // add first tape
                var pt1 = bb.GetCorners()[0];
                var pt2 = bb.GetCorners()[3];
                tapes.Add(new BoundingBox(
                    pt1, 
                    new Point3d(pt1.X + tapeW.CurrentValue, pt2.Y, 0)
                ));

                for (var i = 1; i <= qty.CurrentValue; i++)
                {
                    var pts = tapes[0].GetCorners();
                    tapes.Add(new BoundingBox(
                        new Point3d(pts[0].X + (space * i) - (tapeW.CurrentValue / 2), pts[0].Y, 0),
                        new Point3d(pts[0].X + (space * i) + (tapeW.CurrentValue / 2), pts[3].Y, 0)
                    ));
                }

                return tapes;
            }
        }


        private void addCleats()
        {
            if (RhinoGet.GetOneObject("Select an Object to add Cleats to", false, ObjectType.Curve, out ObjRef crv) == Result.Success)
            {
                var rect = crv.Curve();
                var bb = rect.GetBoundingBox(true);
                var corners = bb.GetCorners();
                var edges = bb.GetEdges();
                var x1 = corners[3].X + 2;
                var x2 = corners[2].X - 2;
                var y1 = corners[3].Y - (edges[1].Length / 3) - 1;
                var y2 = corners[2].Y - (edges[1].Length / 3) + 1;
                Rectangle3d rectta = new Rectangle3d(Plane.WorldXY, new Point3d(x1, y1, 0), new Point3d(x2, y2, 0));
                if (rectta.Width > 96)
                {
                    var diff = (rectta.Width - 96) / 2;
                    rectta = new Rectangle3d(Plane.WorldXY, new Point3d(x1 + diff, y1, 0), new Point3d(x2 - diff, y2, 0));
                }
                Rectangle3d rectta2 = new Rectangle3d(Plane.WorldXY, new Point3d(x1, y1 - (edges[1].Length / 3), 0), new Point3d(x2, y2 - (edges[1].Length / 3), 0));
                if (rectta2.Width > 96)
                {
                    var diff = (rectta2.Width - 96) / 2;
                    rectta2 = new Rectangle3d(Plane.WorldXY, new Point3d(x1 + diff, y1 - (edges[1].Length / 3), 0), new Point3d(x2 - diff, y2 - (edges[1].Length / 3), 0));
                }
                RhinoDoc.ActiveDoc.Objects.AddRectangle(rectta);
                RhinoDoc.ActiveDoc.Objects.AddRectangle(rectta2);
                Plane p = RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.ConstructionPlane();
                p.Origin = rectta.Center;
                Plane p2 = RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.ConstructionPlane();
                p2.Origin = rectta2.Center;
                DrawTools dt = new DrawTools(RhinoDoc.ActiveDoc);
                var t1 = dt.AddText("CLEAT", rectta.Center, dt.StandardDimstyle(), 0.1, 0, 1, 3);
                dt.AddText("CLEAT", rectta2.Center, dt.StandardDimstyle(), 0.1, 0, 1, 3);
                RhinoDoc.ActiveDoc.Objects.AddText("CLEAT", p, 0.1, "Arial", false, false, TextJustification.MiddleCenter);
                RhinoDoc.ActiveDoc.Objects.AddText("SPACER", p2, 0.1, "Arial", false, false, TextJustification.MiddleCenter);
            }
        }
        private void addAmGirlCleats()
        {

        }
    }
}