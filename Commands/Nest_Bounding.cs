using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Display;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.Geometry;
using Rhino.DocObjects;
using Rhino.Commands;

namespace gjTools.Commands
{
    public class Nest_Bounding : Command
    {
        public Nest_Bounding()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static Nest_Bounding Instance { get; private set; }

        public override string EnglishName => "Nest_Bounds";

        public double Spacing = 0.25;
        public double Width = 72;
        public double Height = 48;
        public double Margin = 0.75;

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var DDraw = new CustomDisplay(true);
            var PartGrids = new List<PartGrid>();

            // Collect the sheet info later...
            double block = 0.5;
            var Sheet = new NestGrid(1, (int)(Width / block), (int)(Height / block), block);

            // Collect Parts
            var count = 0;
            while (true)
            {
                count++;
                var res = RhinoGet.GetMultipleObjects($"Select Part #{count} or Enter", true, ObjectType.AnyObject, out ObjRef[] part);
                doc.Objects.UnselectAll();
                if (res == Result.Nothing || part == null)
                    break;
                if (res == Result.Cancel)
                    return res;

                var pg = new PartGrid((byte)count, new List<ObjRef>(part), block);
                    pg.PixelatePart();
                TestAddParts(ref Sheet, ref pg);
                PartGrids.Add(pg);
            }

            // Time to rock and roll
            TestSheetGrid(Sheet, ref DDraw);

            foreach(var pg in PartGrids)
                TestPartGrid(pg, ref DDraw);

            doc.Views.Redraw();
            string str = "";
            RhinoGet.GetString("Enter To Continue", true, ref str);
            DDraw.Dispose();

            return Result.Success;
        }

        public void TestSheetGrid(NestGrid NestBox, ref CustomDisplay DDraw)
        {
            // Temp write the Grid box
            var count = new List<int> { 0, 0 };
            foreach (var row in NestBox.GetGrid)
            {
                foreach (var col in row)
                {
                    var pt = new Point3d(NestBox.Pixel * count[1], NestBox.Pixel * count[0], 0);
                    var pts = new List<Point3d>
                    {
                        pt, new Point3d(pt.X + NestBox.Pixel, pt.Y, 0),
                        new Point3d(pt.X + NestBox.Pixel, pt.Y + NestBox.Pixel, 0),
                        new Point3d(pt.X, pt.Y + NestBox.Pixel, 0), pt
                    };

                    var used = false;
                    if (col > 0)
                        used = true;

                    DDraw.AddPolygon(pts, NestBox.ColorShift(col), System.Drawing.Color.Aquamarine, used, true);
                    count[1]++;
                }
                count[1] = 0;
                count[0]++;
            }

            NestBox.Image(2);
        }

        public void TestPartGrid(PartGrid pg, ref CustomDisplay DDraw)
        {
            var p = pg.Bounds.GetCorners()[3];
            var origX = p.X;
            for (int i = 0; i < pg.GetGrid.Count; i++)
            {
                for (int ii = 0; ii < pg.GetGrid[0].Count; ii++)
                {
                    if (pg.GetGrid[i][ii])
                        DDraw.AddCircle(new Circle(p, pg.Pixel / 2), System.Drawing.Color.Aquamarine);

                    p.X += pg.Pixel;
                }

                p.Y -= pg.Pixel;
                p.X = origX;
            }
        }

        public void TestAddParts(ref NestGrid NestBox, ref PartGrid part)
        {
            var pt = NestBox.FindOpenSpot(part);
            if (pt.Fit)
            {
                NestBox.ReserveSpot(pt, part);
            }
            else
            {
                RhinoApp.WriteLine($"Part #{part.ID} didnt fit, I'll Flip it 180 and try again");
                part.Flip180();
                pt = NestBox.FindOpenSpot(part);
                if (pt.Fit)
                {
                    NestBox.ReserveSpot(pt, part);
                }
                else
                {
                    RhinoApp.WriteLine($"Part #{part.ID} didnt fit again, I'll Turn it 90 and try again");
                    part.Flip90();
                    pt = NestBox.FindOpenSpot(part);
                    if (pt.Fit)
                    {
                        NestBox.ReserveSpot(pt, part);
                    }
                    else
                    {
                        RhinoApp.WriteLine($"Part #{part.ID} didnt fit again, I'll Turn it 180 and try one last time");
                        part.Flip180();
                        pt = NestBox.FindOpenSpot(part);
                        if (pt.Fit)
                        {
                            NestBox.ReserveSpot(pt, part);
                        }
                        else
                        {
                            RhinoApp.WriteLine($"Part #{part.ID} wont fit, No more room");
                        }
                    }
                }
            }

        }

        public void SortPartsByWidth(ref List<PartGrid> parts)
        {
            parts.Sort((x, y) => x.Width.CompareTo(y.Width));
        }





        /// <summary>
        /// Simple 2D Integral Point with bool flag
        /// </summary>
        public struct IntPoint2d
        {
            public int X;
            public int Y;
            public bool Fit;

            public IntPoint2d(int x = 0, int y = 0)
            {
                X = x;
                Y = y;
                Fit = false;
            }
        }

        /// <summary>
        /// Sheet Gridifier
        /// </summary>
        public struct NestGrid
        {
            private double Pixel_Size;
            private List<List<byte>> Grid;
            private List<byte> Ledger;
            private byte _ID;
            public NestGrid(byte id, int Columns, int Rows, double PixelSize = 1.0)
            {
                Pixel_Size = PixelSize;
                Grid = new List<List<byte>>();
                Ledger = new List<byte>();
                _ID = id;

                var row = new List<byte>();
                for (int i = 0; i < Columns; i++)
                    row.Add(0);

                for (int i = 0; i < Rows; i++)
                    Grid.Add(new List<byte>(row));
            }
            public byte ID { get { return _ID; } }
            public List<byte> GetIDList { get { return Ledger; } }
            public double Pixel { get { return Pixel_Size; } }
            public List<List<byte>> GetGrid { get { return Grid; } }
            public int GetWidth { get { return Grid[0].Count; } }
            public int GetHeight { get { return Grid.Count; } }

            public IntPoint2d FindOpenSpotBounding(PartGrid p)
            {
                var h = p.Height;
                var w = p.Width;
                var pt = new IntPoint2d();

                while (!pt.Fit)
                {
                    // Check for Fit
                    if (Grid[pt.Y][pt.X] == 0)
                    {
                        pt.Fit = true;
                        for (var i = pt.Y; i < p.Height; i++)
                            for (int ii = pt.X; ii < p.Width; ii++)
                                if (Grid[i][ii] != 0)
                                    pt.Fit = false;
                    }

                    if (pt.Fit) break;

                    // progress the point
                    pt.Y++;
                    if (Grid.Count == pt.Y + p.Height)
                    {
                        pt.Y = 0;
                        pt.X++;
                    }

                    // Check Break Limit and exit
                    if (pt.X + p.Width == Grid[0].Count)
                        break;
                }

                return pt;
            }

            public IntPoint2d FindOpenSpot(PartGrid p)
            {
                var h = p.Height;
                var w = p.Width;
                var pt = new IntPoint2d();

                while (!pt.Fit)
                {
                    // Check for Fit
                    pt.Fit = true;
                    for (var i = 0; i < p.Height; i++)
                        for (int ii = 0; ii < p.Width; ii++)
                            if (Grid[i + pt.Y][ii + pt.X] != 0 && p.GetGrid[i][ii])
                                pt.Fit = false;

                    if (pt.Fit) break;

                    // progress the point
                    pt.Y++;
                    if (Grid.Count == pt.Y + p.Height)
                    {
                        pt.Y = 0;
                        pt.X++;
                    }

                    // Check Break Limit and exit
                    if (pt.X + p.Width == Grid[0].Count)
                        break;
                }

                return pt;
            }

            public void ReserveSpot(IntPoint2d pt, PartGrid p)
            {
                for (int i = 0; i < p.Height; i++)
                    for (int ii = 0; ii < p.Width; ii++)
                        if (p.GetGrid[i][ii])
                            Grid[pt.Y + i][pt.X + ii] = p.ID;
            }

            public System.Drawing.Bitmap Image(float scale = 1)
            {
                var bmp = new System.Drawing.Bitmap(Grid[0].Count, Grid.Count);

                for (int h = 0; h < Grid.Count; h++)
                    for (int w = 0; w < Grid[0].Count; w++)
                        bmp.SetPixel(w, h, ColorShift(Grid[h][w]));

                var scaled = new System.Drawing.Bitmap(bmp, new System.Drawing.Size((int)(bmp.Width * scale), (int)(bmp.Height * scale)));

                scaled.Save($"C:\\Temp\\NestImage-{_ID}.bmp");

                return bmp;
            }

            public System.Drawing.Color ColorShift(byte code)
            {
                while (code > 5)
                    code -= 5;

                switch (code)
                {
                    case 1: return System.Drawing.Color.Yellow;
                    case 2: return System.Drawing.Color.Blue;
                    case 3: return System.Drawing.Color.Green;
                    case 4: return System.Drawing.Color.Magenta;
                    case 5: return System.Drawing.Color.Red;
                }

                return System.Drawing.Color.Black;
            }
        }

        /// <summary>
        /// Gridify a part for NestGrid
        /// </summary>
        public struct PartGrid
        {
            private double CellSize;
            private List<List<bool>> Grid;
            private BoundingBox _Bounds;
            private List<ObjRef> _Obj;
            private byte _ID;
            private int Rotated;

            public PartGrid(byte PartID, List<ObjRef> part, double PixelSize = 1.0)
            {
                CellSize = PixelSize;
                _ID = PartID;
                Grid = new List<List<bool>>();
                _Obj = new List<ObjRef>();
                Rotated = 0;
                _Bounds = BoundingBox.Empty;

                foreach(var p in part)
                    if (!p.Object().IsDeleted)
                    {
                        Obj.Add(p);
                        _Bounds.Union(p.Geometry().GetBoundingBox(true));
                    }

                int Width = (int)Math.Ceiling(_Bounds.GetEdges()[0].Length / CellSize);
                int Height = (int)Math.Ceiling(_Bounds.GetEdges()[1].Length / CellSize);

                var Row = new List<bool>();
                for (int i = 0; i < Width; i++)
                    Row.Add(true);   // Default the entire block to true until said otherwise

                for (int i = 0; i < Height; i++)
                    Grid.Add(new List<bool>(Row));
            }

            public byte ID { get { return _ID; } }
            public List<ObjRef> Obj { get { return _Obj; } }
            public BoundingBox Bounds { get { return _Bounds; } }
            public List<List<bool>> GetGrid { get { return Grid; } }
            public double Pixel { get { return CellSize; } }
            public int Height { get { return Grid.Count; } }
            public int Width { get { return Grid[0].Count; } }
            public int GetRotate { get { return Rotated; } }
            public bool IsValid { get { if (Obj.Count > 0) return true; else return false; } }

            public bool PixelatePart()
            {
                if (!IsValid)
                    return false;

                var Crv = new List<Curve>();
                foreach (var o in Obj)
                    if (o.Curve() != null)
                        Crv.Add(o.Curve());

                if (Crv.Count == 0)
                    return false;

                var p = Bounds.GetCorners()[3];
                var originX = p.X;
                for (int i = 0; i < Grid.Count; i++)
                {
                    for (int ii = 0; ii < Grid[0].Count; ii++)
                    {
                        Grid[i][ii] = false;

                        foreach (var c in Crv)
                        {
                            if (c.IsClosed)
                            {
                                var cont = c.Contains(p, Plane.WorldXY, 0.25);
                                if (cont == PointContainment.Inside || cont == PointContainment.Coincident)
                                    Grid[i][ii] = true;
                            }
                        }

                        p.X += CellSize;
                    }

                    p.Y -= CellSize;
                    p.X = originX;
                }

                return true;
            }
            private void ReCalcData()
            {
                _Bounds = BoundingBox.Empty;
                foreach (var o in Obj)
                    _Bounds.Union(o.Geometry().GetBoundingBox(true));
                
                Grid.Clear();
                int Width = (int)Math.Ceiling(_Bounds.GetEdges()[0].Length / CellSize);
                int Height = (int)Math.Ceiling(_Bounds.GetEdges()[1].Length / CellSize);

                var Row = new List<bool>();
                for (int i = 0; i < Width; i++)
                    Row.Add(true);   // Default the entire block to true until said otherwise

                for (int i = 0; i < Height; i++)
                    Grid.Add(new List<bool>(Row));
            }
            public void Flip180()
            {
                Grid.Reverse();
                for (var i = 0; i < Grid.Count; i++)
                    Grid[i].Reverse();
                Rotated += 180;
            }
            public void Flip90()
            {
                // Tansposing
                var newGrid = new List<List<bool>>(Grid[0].Count);
                var newRow = new List<bool>(Grid.Count);

                for(int h = 0; h < Grid[0].Count; h++)
                {
                    for(int w = 0; w < Grid.Count; w++)
                        newRow.Add(Grid[w][h]);

                    newGrid.Add(new List<bool>(newRow));
                    newRow.Clear();
                }

                Grid = newGrid;
                Rotated += 90;
            }
        }
    }
}