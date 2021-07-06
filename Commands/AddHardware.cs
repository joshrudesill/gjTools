using System;
using Rhino;
using Rhino.Commands;
using System.Collections.Generic;
using Rhino.Geometry;
using Rhino.DocObjects;
using Rhino.Input;

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
            var hardwares = new List<string> { "Cleats", "American Girl Cleats" };
            var type = Rhino.UI.Dialogs.ShowListBox("Add Hardware", "Choose a type of hardware to add..", hardwares);

            switch(hardwares.IndexOf((string)type))
            {
                case 0: 
                    addCleats();
                    break;
                case 1: 
                    addAmGirlCleats();
                    break;
            }
            return Result.Success;
        }

        private void addCleats()
        {
            var go = d.selectObject("Select an object to add cleats to");
            var rect = go.Object(0).Curve();
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
            var t1 = dt.AddText("CLEAT", rectta.Center, dt.StandardDimstyle(), 1, 0, 1, 3);
            var t2 = dt.AddText("SPACER", rectta2.Center, dt.StandardDimstyle(), 1, 0, 1, 3);
            RhinoDoc.ActiveDoc.Objects.AddText(t1);
            RhinoDoc.ActiveDoc.Objects.AddText(t2);
            RhinoDoc.ActiveDoc.Views.Redraw();
        }
        private void addAmGirlCleats()
        {

        }
    }
}