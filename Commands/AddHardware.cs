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


        public bool AddTape(RhinoDoc doc)
        {
            var disp = new Rhino.Display.CustomDisplay(true);
            var tapeW = new OptionDouble(0.5); 
            var offset = new OptionDouble(0.125);
            var orient = new OptionToggle(false, "Vertical", "Horizontal");
            var qty = new OptionInteger(2);
            var res = GetResult.NoResult;

            if (RhinoGet.GetOneObject("Select Object", false, ObjectType.Curve, out ObjRef obj) != Result.Success)
                return false;
            doc.Objects.UnselectAll();

            var go = new GetOption();
                go.SetCommandPrompt("Tape Configure");
                go.AcceptNothing(true);
                go.AddOptionDouble("TapeWidth", ref tapeW, "New Tape Width");
                go.AddOptionDouble("OffsetEdges", ref offset, "Set Offset");
                go.AddOptionInteger("Qty", ref qty, "Qty of Tape");
                go.AddOptionToggle("Orientation", ref orient);

            // initial add tape
            addTape(obj.Curve());

            while (true)
            {
                res = go.Get();

                if (res == GetResult.Nothing || res == GetResult.Cancel)
                {
                    disp.Dispose();
                    break;
                }

                disp.Clear();
                    
                foreach (var r in addTape(obj.Curve()))
                {
                    var lines = r.GetEdges();
                    for (var i = 0; i <= 4; i++)
                        disp.AddLine(lines[i], System.Drawing.Color.Aquamarine, 1);
                }
                doc.Views.Redraw();
            }

            disp.Dispose();

            List<BoundingBox> addTape(Curve crv)
            {
                var tapes = new List<BoundingBox>();
                var bb = crv.GetBoundingBox(true);
                var pts = bb.GetCorners();
                
                    
                double space = (bb.GetEdges()[0].Length - (tapeW.CurrentValue / 2)) / (qty.CurrentValue - 1);

                // add first tape
                tapes.Add(new BoundingBox(
                    pts[0], 
                    new Point3d(pts[0].X + tapeW.CurrentValue, pts[3].Y, 0)
                ));

                for (var i = 1; i <= qty.CurrentValue; i++)
                {
                    tapes.Add(new BoundingBox(
                        new Point3d(pts[0].X + (space * i) - (tapeW.CurrentValue / 2), pts[0].Y, 0),
                        new Point3d(pts[0].X + (space * i) + (tapeW.CurrentValue / 2), pts[3].Y, 0)
                    ));
                }

                return tapes;
            }

            return true;
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