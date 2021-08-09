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
                    addCleats2(doc);
                    break;
                case 1: 
                    addAmGirlCleats(doc);
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
            var strips = new List<PolylineCurve>();

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

            while (true)
            {
                if (res == GetResult.Nothing || res == GetResult.Cancel)
                {
                    disp.Dispose();
                    break;
                }

                disp.Clear();

                strips = addTape(obj.Curve());
                foreach (var r in strips)
                {
                    var segs = r.DuplicateSegments();
                    foreach(var c in segs)
                        disp.AddCurve(c, System.Drawing.Color.Aquamarine, 2);
                }

                doc.Views.Redraw();
                res = go.Get();
            }

            RhinoApp.WriteLine($"The result: {res}");
            if (res == GetResult.Nothing)
            {
                // make the strips real
                foreach(var pl in strips)
                {
                    var pline = doc.Objects.FindId(doc.Objects.AddCurve(pl));
                    var play = doc.Layers[obj.Object().Attributes.LayerIndex];
                    if (play.ParentLayerId != Guid.Empty)
                        play = doc.Layers.FindId(play.ParentLayerId);

                    pline.Attributes.LayerIndex = play.Index;
                    pline.CommitChanges();
                }
            }

            List<PolylineCurve> addTape(Curve crv)
            {
                var tapes = new List<PolylineCurve>();
                var tapeLines = new List<Line>();
                var oCrv = crv.Offset(Plane.WorldXY, -offset.CurrentValue, doc.ModelAbsoluteTolerance, CurveOffsetCornerStyle.Sharp);
                var bb = oCrv[0].GetBoundingBox(true);
                foreach (var c in oCrv)
                {
                    bb.Union(c.GetBoundingBox(true));
                    disp.AddCurve(c, System.Drawing.Color.LightGray, 1);
                }

                var pts = bb.GetCorners();
                double space = (bb.GetEdges()[0].Length - tapeW.CurrentValue) / (qty.CurrentValue - 1);

                // Check Orientation
                if (!orient.CurrentValue)
                {   // Vertical Tape
                    // add first tape
                    tapeLines.Add(new Line(
                        pts[0], 
                        new Point3d(pts[0].X, pts[3].Y, 0)
                    ));

                    for (var i = 1; i < qty.CurrentValue; i++)
                    {
                        tapeLines.Add(new Line(
                            new Point3d(pts[0].X + (space * i), pts[0].Y, 0),
                            new Point3d(pts[0].X + (space * i), pts[3].Y, 0)
                        ));
                    }
                }
                else
                {   //  Horizontal Tape
                    space = (bb.GetEdges()[1].Length - tapeW.CurrentValue) / (qty.CurrentValue - 1);
                    // add first tape
                    tapeLines.Add(new Line(
                        new Point3d(pts[1].X, pts[0].Y, 0),
                        pts[0]
                    ));

                    for (var i = 1; i < qty.CurrentValue; i++)
                    {
                        tapeLines.Add(new Line(
                            new Point3d(pts[1].X, pts[0].Y + (space * i), 0),
                            new Point3d(pts[0].X, pts[0].Y + (space * i), 0)
                        ));
                    }
                }

                foreach (var l in tapeLines)
                    tapes.Add(LineToPoly(l));

                return tapes;
            }

            PolylineCurve LineToPoly(Line l)
            {
                var offLine = new Line(l.From, l.To);
                if (l.FromY == l.ToY)
                {
                    offLine.FromY = l.FromY + tapeW.CurrentValue;
                    offLine.ToY = l.ToY + tapeW.CurrentValue;
                }
                else
                {
                    offLine.FromX = l.FromX + tapeW.CurrentValue;
                    offLine.ToX = l.ToX + tapeW.CurrentValue;
                }
                return new PolylineCurve(new List<Point3d> { l.From, l.To, offLine.To, offLine.From, l.From });
            }

            return true;
        }

        public bool addCleats2(RhinoDoc doc)
        {
            double roundQuarter(double len) { return (int)(len * 4) / 4; }

            if (RhinoGet.GetOneObject("Select Object", false, ObjectType.Curve, out ObjRef obj) != Result.Success)
                return false;

            int heightThreshold = 26;
            int maxCleatLength = 96;
            var dt = new DrawTools(doc);

            var bb = new Box(obj.Geometry().GetBoundingBox(true));
            var cleats = new List<Rectangle3d>();
            var labels = new List<string> { "CLEAT", "SPACER" };
            var consPlane = new Plane(new Point3d(bb.Center.X, bb.GetCorners()[3].Y, 0), Vector3d.ZAxis);

            double width = (bb.X.Length + 4 > maxCleatLength) ? maxCleatLength : roundQuarter(bb.X.Length - 4);
            double height = 2;

            consPlane.OriginX -= width / 2;

            if (bb.Y.Length > heightThreshold)  // Needs cleats and spacers
            {
                // Move Plane
                consPlane.OriginY -= roundQuarter(bb.Y.Length / 3 - height);
                cleats.Add(new Rectangle3d(consPlane, width, height));

                // Move Plane
                consPlane.OriginY -= roundQuarter(bb.Y.Length / 2 - height * 2);
                cleats.Add(new Rectangle3d(consPlane, width, height));
            }
            else    // Only one Cleat
            {
                consPlane.OriginY -= roundQuarter(bb.Y.Length / 2);
                cleats.Add(new Rectangle3d(consPlane, width, height));
            }

            // Get the layer
            var lay = doc.Layers[obj.Object().Attributes.LayerIndex];
            if (lay.ParentLayerId != Guid.Empty)
                lay = doc.Layers.FindId(lay.ParentLayerId);
            var attr = new ObjectAttributes { LayerIndex = lay.Index };

            // add the cleats
            foreach (var r in cleats)
            {
                doc.Objects.AddRectangle(r, attr);
                doc.Objects.AddText(dt.AddText(labels[cleats.IndexOf(r)], r.Center, dt.StandardDimstyle(), 1.5, 0, 1, 3), attr);
            }
            return true;
        }

        private void addCleats(RhinoDoc doc)
        {
            if (RhinoGet.GetOneObject("Select an Object to add Cleats to", false, ObjectType.Curve, out ObjRef crv) == Result.Success)
            {
                var rect = crv.Curve();
                int pli = 0;
                Guid gi = crv.ObjectId;

                var lay = doc.Layers[crv.Object().Attributes.LayerIndex];
                var player = lay.ParentLayerId;
                if (player != Guid.Empty)
                {
                    var lt = doc.Layers.FindId(player);
                    pli = lt.Index;
                }
                else
                {
                    pli = crv.Object().Attributes.LayerIndex;
                }
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
                var oa = new ObjectAttributes();
                oa.LayerIndex = pli;
                RhinoDoc.ActiveDoc.Objects.AddRectangle(rectta, oa);
                RhinoDoc.ActiveDoc.Objects.AddRectangle(rectta2, oa);
                Plane p = RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.ConstructionPlane();
                p.Origin = rectta.Center;
                Plane p2 = RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.ConstructionPlane();
                p2.Origin = rectta2.Center;
                DrawTools dt = new DrawTools(RhinoDoc.ActiveDoc);
                var t1 = dt.AddText("CLEAT", rectta.Center, dt.StandardDimstyle(), 0.1, 0, 1, 3);
                dt.AddText("CLEAT", rectta2.Center, dt.StandardDimstyle(), 0.1, 0, 1, 3);
                RhinoDoc.ActiveDoc.Objects.AddText("CLEAT", p, 0.1, "Arial", false, false, TextJustification.MiddleCenter, oa);
                RhinoDoc.ActiveDoc.Objects.AddText("SPACER", p2, 0.1, "Arial", false, false, TextJustification.MiddleCenter, oa);
            }
        }

        private bool addAmGirlCleats(RhinoDoc doc)
        {
            if (RhinoGet.GetOneObject("Select Object", false, ObjectType.Curve, out ObjRef obj) != Result.Success)
                return false;

            var bb = new Box(obj.Object().Geometry.GetBoundingBox(true));
            var pt = bb.GetCorners();
            var plne = new Plane(pt[3], Vector3d.ZAxis);
            var cleats = new List<Rectangle3d>();

            plne.OriginX += 1;
            plne.OriginY -= 3;
            cleats.Add(new Rectangle3d(plne, 2, 2));

            plne.OriginY -= bb.Y.Length - 5;
            cleats.Add(new Rectangle3d(plne, 2, 2));

            var lay = doc.Layers[obj.Object().Attributes.LayerIndex];
            if (lay.ParentLayerId != Guid.Empty)
                lay = doc.Layers.FindId(lay.ParentLayerId);

            var attr = new ObjectAttributes { LayerIndex = lay.Index };
            var mirror = Transform.Mirror(new Plane(bb.Center, Vector3d.XAxis));

            foreach(var c in cleats)
                doc.Objects.Transform(doc.Objects.AddRectangle(c, attr), mirror, false);

            return true;
        }
    }
}