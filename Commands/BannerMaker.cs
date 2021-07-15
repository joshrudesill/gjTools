using System.Collections.Generic;
using System;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.UI;
using Rhino.Geometry;

namespace gjTools.Commands
{
    public class BannerMaker : Command
    {
        public BannerMaker()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static BannerMaker Instance { get; private set; }

        public override string EnglishName => "gjBannerMaker";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var BData = new Banner();

            if (!Dialogs.ShowEditBox("Banner Maker", "Enter Banner Code", "", false, out string code))
                return Result.Cancel;

            if (code.Length != 15 * 7)
            {
                RhinoApp.WriteLine("That Code Doesnt Look Correct");
                return Result.Cancel;
            }

            var res = Rhino.Input.RhinoGet.GetString("Part Number", false, ref BData.partNum);
            if (res != Result.Success)
                return Result.Cancel;

            ParseCode(code, ref BData);

            // Make the Banner
            MakeBanner(doc, BData);

            return Result.Success;
        }



        public bool MakeBanner(RhinoDoc doc, Banner BData)
        {
            var lt = new LayerTools(doc);

            // Make Live area Box
            var liveArea = AddRectangleLayer(doc, lt, BData._LiveArea, "LiveArea", BData.partNum);

            // make the stitch box
            var stitchBox = AddRectangleLayer(doc, lt, BData.StitchBox(), "StitchBox", BData.partNum);

            // make the stitch box
            var cutSize = AddRectangleLayer(doc, lt, BData.CutSize(), "CutSize", BData.partNum);

            // add grommets
            MakeGrommets(doc, BData, lt);

            doc.Views.Redraw();
            return true;
        }


        /// <summary>
        /// Adds the grommets to the drawing
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="BData"></param>
        /// <param name="lt"></param>
        /// <returns></returns>
        public List<RhinoObject> MakeGrommets(RhinoDoc doc, Banner BData, LayerTools lt)
        {
            var pts = GrommetPoints(BData);
            var gromLay = lt.CreateLayer("Grommets", BData.partNum, System.Drawing.Color.Brown);
            var obj = new List<RhinoObject>();

            foreach (var p in pts)
            {
                var id = doc.Objects.AddCircle(new Circle(p, BData.GromDiameter / 2));
                var cir = doc.Objects.FindId(id);
                cir.Attributes.LayerIndex = gromLay.Index;
                cir.CommitChanges();

                obj.Add(cir);
            }

            return obj;
        }

        /// <summary>
        /// Calcs all the points for the grommets
        /// </summary>
        /// <param name="BData"></param>
        /// <returns></returns>
        public List<Point3d> GrommetPoints(Banner BData)
        {
            var sides = new List<Point3d>();
            var top = new List<Point3d>();
            var bott = new List<Point3d>();
            var all = new List<Point3d>();
            double adjust = 0;
            int gromQty;

            // Check the Top
            if (BData.gromTopSpace > 0)
            {
                if (BData.topFinish == edgeType.pocket)
                    adjust = BData.TopSize;

                gromQty = (int)Math.Round((BData._LiveArea.Width - (BData.gromFromEdge * 2)) / BData.gromTopSpace);
                var pt = new Point3d(BData.gromFromEdge, BData._LiveArea.Corner(3).Y - adjust - BData.gromFromEdge, 0);
                top.Add(pt);

                for (var i = 0; i < gromQty; i++)
                {
                    pt.X += (BData._LiveArea.Width - (BData.gromFromEdge * 2)) / gromQty;
                    top.Add(pt);
                }
                all.AddRange(top);
            }

            // Check the bottom
            if (BData.gromBottSpace > 0)
            {
                if (BData.bottFinish == edgeType.pocket)
                    adjust = BData.BottSize;
                else
                    adjust = 0;

                gromQty = (int)Math.Round((BData._LiveArea.Width - (BData.gromFromEdge * 2)) / BData.gromBottSpace);
                var pt = new Point3d(BData.gromFromEdge, BData._LiveArea.Corner(0).Y + adjust + BData.gromFromEdge, 0);
                bott.Add(pt);

                for (var i = 0; i < gromQty; i++)
                {
                    pt.X += (BData._LiveArea.Width - (BData.gromFromEdge * 2)) / gromQty;
                    bott.Add(pt);
                }
                all.AddRange(bott);
            }

            // Sides
            if (BData.gromSideSpace > 0)
            {
                gromQty = (int)Math.Round((BData._LiveArea.Height - (BData.gromFromEdge * 2)) / BData.gromSideSpace);
                var pt = new Point3d(BData.gromFromEdge, BData._LiveArea.Corner(0).Y + BData.gromFromEdge, 0);

                if (bott.Count == 0)
                    sides.Add(pt);

                for (var i = 0; i < gromQty; i++)
                {
                    pt.Y += (BData._LiveArea.Height - (BData.gromFromEdge * 2)) / gromQty;

                    if (i != gromQty - 1)
                        sides.Add(pt);
                    if (top.Count == 0)
                        sides.Add(pt);
                }

                var qty = sides.Count;
                // Dupe them to the other side
                for (var i = 0; i < qty; i++)
                {
                    pt = new Point3d(sides[i]);
                    pt.X += BData._LiveArea.Width - (BData.gromFromEdge * 2);
                    sides.Add(pt);
                }
                all.AddRange(sides);
            }

            return all;
        }

        /// <summary>
        /// create and add the boxes
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="lt"></param>
        /// <param name="rec"></param>
        /// <param name="layer"></param>
        /// <param name="parentLay"></param>
        /// <returns></returns>
        public RhinoObject AddRectangleLayer(RhinoDoc doc, LayerTools lt, Rectangle3d rec, string layer, string parentLay)
        {
            var color = System.Drawing.Color.DarkGreen;
            if (layer == "StitchBox")
                color = System.Drawing.Color.DarkBlue;
            if (layer == "CutSize")
                color = System.Drawing.Color.DarkRed;

            var box = doc.Objects.FindId(
                doc.Objects.AddRectangle(rec)
            );
            box.Attributes.LayerIndex = lt.CreateLayer(layer, parentLay, color).Index;
            box.CommitChanges();

            return box;
        }

        /// <summary>
        /// Makes sense of the banner code from the banner Form
        /// </summary>
        /// <param name="Code"></param>
        /// <param name="BData"></param>
        public void ParseCode(string Code, ref Banner BData)
        {
            int byt = 7;
            var snip = new List<double>();
            
            for (int i = 1; i < Code.Length / byt; i++)
                snip.Add(double.Parse(Code.Substring(byt * i, byt)));

            BData.LiveArea(snip[0], snip[1]);

            // top finishing
            if (snip[2] > 0 || snip[7] > 0)
            {
                BData.TopSize = (snip[2] > 0) ? snip[2] : snip[7];
                BData.topFinish = (snip[2] > 0) ? edgeType.pocket : edgeType.hem;
            }
            // bottom finishing
            if (snip[3] > 0 || snip[8] > 0)
            {
                BData.BottSize = (snip[3] > 0) ? snip[3] : snip[8];
                BData.bottFinish = (snip[3] > 0) ? edgeType.pocket : edgeType.hem;
            }
            // Side Finishing
            if (snip[9] > 0)
            {
                BData.sideFinish = edgeType.hem;
                BData.SideSize = snip[9];
            }

            // Grommet Data
            BData.gromTopSpace = snip[10];
            BData.gromBottSpace = snip[11];
            BData.gromSideSpace = snip[12];

            // Folded Banner?
            if (snip[13] > 0)
                BData.DoubleSided = true;
        }



        public enum edgeType { raw, hem, pocket }
        public struct Banner
        {
            public string partNum;

            public bool DoubleSided;

            public Rectangle3d _LiveArea;

            public edgeType sideFinish;
            public edgeType topFinish;
            public edgeType bottFinish;
            public double SideSize;
            public double TopSize;
            public double BottSize;

            public double GromDiameter { get { return 0.75; } }
            public double gromFromEdge { get { return 0.563; } }
            public double gromTopSpace;
            public double gromSideSpace;
            public double gromBottSpace;

            public double stitchExtra;
            
            public Rectangle3d LiveArea(double width, double height)
            {
                _LiveArea = new Rectangle3d(Plane.WorldXY, width, height);
                topFinish = edgeType.raw;
                bottFinish = edgeType.raw;
                sideFinish = edgeType.raw;
                stitchExtra = 0.25;
                DoubleSided = false;
                return _LiveArea;
            }

            public Rectangle3d StitchBox()
            {
                var pt0 = _LiveArea.Corner(0);
                var pt2 = _LiveArea.Corner(2);

                if (sideFinish == edgeType.hem)
                {
                    pt0.X += stitchExtra;
                    pt2.X -= stitchExtra;
                }

                if (topFinish == edgeType.hem)
                    pt2.Y -= stitchExtra;
                if (topFinish == edgeType.pocket)
                    pt2.Y -= TopSize;

                if (bottFinish == edgeType.hem)
                    pt0.Y += stitchExtra;
                if (bottFinish == edgeType.pocket)
                    pt0.Y += BottSize;

                return new Rectangle3d(Plane.WorldXY, pt0, pt2);
            }

            public Rectangle3d CutSize()
            {
                var pt0 = _LiveArea.Corner(0);
                var pt2 = _LiveArea.Corner(2);

                if (sideFinish == edgeType.hem)
                {
                    pt0.X -= SideSize / 2;
                    pt2.X += SideSize / 2;
                }

                if (topFinish == edgeType.hem)
                    pt2.Y += TopSize;
                if (topFinish == edgeType.pocket)
                    pt2.Y += TopSize + stitchExtra;

                if (bottFinish == edgeType.hem)
                    pt0.Y -= BottSize;
                if (bottFinish == edgeType.pocket)
                    pt0.Y -= BottSize + stitchExtra;

                return new Rectangle3d(Plane.WorldXY, pt0, pt2);
            }
        }
    }
}