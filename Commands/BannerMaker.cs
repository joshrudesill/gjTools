using System.Collections.Generic;
using System;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.UI;
using Rhino.Geometry;
using Eto;

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

        public override string EnglishName => "BannerMaker";
        private Eto.Drawing.Point FormWindowPosition = Eto.Drawing.Point.Empty;
        private BannerDialog BannerData;

        /// <summary>
        /// Stores the Geometry to have created
        /// </summary>
        public struct BannerGeom
        {
            public BoundingBox CutsBox;
            public List<BoundingBox> LiveBox;
            public List<BoundingBox> StitBox;
            public List<BoundingBox> GromBox;
            public List<Point3d> GromPts;
            public List<TextEntity> Verbage;
            public List<Circle> GromCir;
        }
        
        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            if (FormWindowPosition == Eto.Drawing.Point.Empty)
                FormWindowPosition = new Eto.Drawing.Point((int)MouseCursor.Location.X - 250, 200);

            // Keep the entire form in memory and re-use
            if (BannerData == null)
                BannerData = new BannerDialog { windowPosition = FormWindowPosition };

            BannerData.ShowForm();
            FormWindowPosition = BannerData.windowPosition;

            if (BannerData.CommandResult != Eto.Forms.DialogResult.Ok)
                return Result.Cancel;

            var BData = BannerData.GetAllValues();      // get the info from the form

            var BGeom = CreateBoxes(BData);             // make the inital box

            BGeom = GrommetPoints(BGeom, BData);        // get all the points for the groms

            BGeom = CreateGromCircles(BGeom, BData);    // use the points and make the circles

            if (BData.Folded)
                BGeom = MakeFolded(BGeom, BData);       // Folded banner gets extra boxes


            var dt = new DrawTools(doc);
            BGeom = CreateTextLabels(dt, BGeom, BData); // Part number and banner specs

            AddAllObjects(doc, BGeom, BData);           // add the shit to the document

            doc.Views.Redraw();
            return Result.Success;
        }

        public void AddAllObjects(RhinoDoc doc, BannerGeom BGeom, BannerDialog.BannerInfo BData)
        {
            var lt = new LayerTools(doc);
            var parentLayer = lt.CreateLayer(BData.PartNumber);
            var attr = new ObjectAttributes { LayerIndex = parentLayer.Index };

            foreach (var t in BGeom.Verbage)
                doc.Objects.AddText(t, attr);

            attr.LayerIndex = lt.CreateLayer("LiveArea", BData.PartNumber, System.Drawing.Color.DarkOliveGreen).Index;
            foreach (var b in BGeom.LiveBox)
                doc.Objects.AddRectangle(new Rectangle3d(Plane.WorldXY, b.Min, b.Max), attr);

            attr.LayerIndex = lt.CreateLayer("Stitch", BData.PartNumber, System.Drawing.Color.DarkBlue).Index;
            foreach (var b in BGeom.StitBox)
                doc.Objects.AddRectangle(new Rectangle3d(Plane.WorldXY, b.Min, b.Max), attr);

            attr.LayerIndex = lt.CreateLayer("Cut", BData.PartNumber, System.Drawing.Color.Red).Index;
            doc.Objects.AddRectangle(new Rectangle3d(Plane.WorldXY, BGeom.CutsBox.Min, BGeom.CutsBox.Max), attr);

            if (BGeom.GromCir.Count > 0)
            {
                attr.LayerIndex = lt.CreateLayer("Grommet", BData.PartNumber, System.Drawing.Color.SaddleBrown).Index;
                foreach (var c in BGeom.GromCir)
                    doc.Objects.AddCircle(c, attr);
            }
        }

        public BannerGeom CreateTextLabels(DrawTools dt, BannerGeom BGeom, BannerDialog.BannerInfo BData)
        {
            var ds = dt.StandardDimstyle();

            // Part Number
            BGeom.Verbage = new List<TextEntity> { dt.AddText($"PN: {BData.PartNumber}", BGeom.LiveBox[0].Center, ds, 2, 1, 1, 3) };

            // Get the insertion point
            Point3d pt = BGeom.LiveBox[BGeom.LiveBox.Count - 1].Center;
                    pt.X += BData.Width * 0.65;

            // Helper Function
            string FinishBlurb(BannerDialog.BannerInfo.Stitch stitch, BannerDialog.BannerInfo.Finish finish, double size)
            {
                var str = "";
                if (stitch == BannerDialog.BannerInfo.Stitch.Single || stitch == BannerDialog.BannerInfo.Stitch.Double)
                    str += $"{stitch}-Stitched ";
                else if (stitch == BannerDialog.BannerInfo.Stitch.Weld)
                    str += $"{stitch}ed ";

                if (finish == BannerDialog.BannerInfo.Finish.None)
                    str += "Raw Edge";
                else
                    str += $"{size}\" {finish}";

                return str;
            }

            // Add the Banner information in text form
            string txtBlob = $"Top: {FinishBlurb(BData.st_Top, BData.fn_Top, BData.Size_Top)}\n" +
                $"Sides: {FinishBlurb(BData.st_Side, BData.fn_Side, BData.Size_Side)}\n" +
                $"Bottom: {FinishBlurb(BData.st_Bott, BData.fn_Bott, BData.Size_Bott)}\n\n" +
                $"Grommets:\nTop: {BData.gromQty_Top}x\nSide: {BData.gromQty_Side}x/Side\nBottom: {BData.gromQty_Bott}x" +
                $"\nTotal: {BGeom.GromCir.Count}x";

            BGeom.Verbage.Add(dt.AddText(txtBlob, pt, ds, 1));

            return BGeom;
        }

        public BannerGeom MakeFolded(BannerGeom BGeom, BannerDialog.BannerInfo BData)
        {
            var mirror = Transform.Mirror(new Plane(BGeom.LiveBox[0].Max, Vector3d.YAxis));
            
            BGeom.LiveBox.Add(new BoundingBox(BGeom.LiveBox[0].GetCorners(), mirror));
            BGeom.StitBox.Add(new BoundingBox(BGeom.StitBox[0].GetCorners(), mirror));
            BGeom.GromBox.Add(new BoundingBox(BGeom.GromBox[0].GetCorners(), mirror));

            var GromCircles = new List<Circle>(BGeom.GromCir);
            foreach (var c in GromCircles)
            {
                var mir_Cir = new Circle(c.Center, c.Radius);
                mir_Cir.Transform(mirror);
                BGeom.GromCir.Add(mir_Cir);
            }

            // change the Cut Size
            double extra = (BData.fn_Bott == BannerDialog.BannerInfo.Finish.Hem) ? BData.Size_Bott : StitchExtra(BData.st_Bott);
            BGeom.CutsBox.Max = new Point3d(BGeom.CutsBox.Max.X, BGeom.LiveBox[1].Max.Y + extra, 0);

            return BGeom;
        }

        public BannerGeom CreateGromCircles(BannerGeom BGeom, BannerDialog.BannerInfo BData)
        {
            BGeom.GromCir = new List<Circle>();

            foreach (var p in BGeom.GromPts)
                BGeom.GromCir.Add(new Circle(p, BData.gromDiameter / 2));

            return BGeom;
        }

        public BannerGeom GrommetPoints(BannerGeom BGeom, BannerDialog.BannerInfo BData)
        {
            BGeom.GromPts = new List<Point3d>();

            // Generate the points
            if (BData.gromQty_Top > 1)
                BGeom.GromPts.AddRange(DivLine(BData.gromQty_Top, BGeom.GromBox[0].GetEdges()[2]));
            if (BData.gromQty_Bott > 1)
                BGeom.GromPts.AddRange(DivLine(BData.gromQty_Bott, BGeom.GromBox[0].GetEdges()[0]));
            if (BData.gromQty_Side > 1)
            {
                BGeom.GromPts.AddRange(DivLine(BData.gromQty_Side, BGeom.GromBox[0].GetEdges()[1]));
                BGeom.GromPts.AddRange(DivLine(BData.gromQty_Side, BGeom.GromBox[0].GetEdges()[3]));
            }

            // Remove Duplicates
            if (BGeom.GromPts.Count > 0)
                BGeom.GromPts = new List<Point3d>(Point3d.CullDuplicates(BGeom.GromPts, 0.01));

            List<Point3d> DivLine(int qty, Line l)
            {
                var pts = new List<Point3d>();

                for(var i = 0; i < qty; i++)
                    pts.Add(l.PointAtLength((l.Length / (qty - 1)) * i));

                return pts;
            };

            return BGeom;
        }

        public BannerGeom CreateBoxes(BannerDialog.BannerInfo BData)
        {
            // Starting Point (Basically the live area)
            var BaseBox = new BoundingBox(Point3d.Origin, new Point3d(BData.Width, BData.Height, 0));

            // Setup the vectors
            var st_MaxVect = new Vector3d(0, 0, 0);
            var st_MinVect = new Vector3d(0, 0, 0);
            var ct_MaxVect = new Vector3d(0, 0, 0);
            var ct_MinVect = new Vector3d(0, 0, 0);
            var gr_MaxVect = new Vector3d(0, 0, 0);
            var gr_MinVect = new Vector3d(0, 0, 0);

            // Populate Vectors
            if (BData.fn_Top != BannerDialog.BannerInfo.Finish.None)
            {
                if (BData.fn_Top == BannerDialog.BannerInfo.Finish.Hem)
                {
                    st_MaxVect.Y -= 0.25;
                    ct_MaxVect.Y += BData.Size_Top;
                }
                else
                {
                    st_MaxVect.Y -= BData.Size_Top;
                    gr_MaxVect.Y -= BData.Size_Top;
                    ct_MaxVect.Y += BData.Size_Top + StitchExtra(BData.st_Top);
                }
            }
            if (BData.fn_Bott != BannerDialog.BannerInfo.Finish.None)
            {
                if (BData.fn_Bott == BannerDialog.BannerInfo.Finish.Hem)
                {
                    st_MinVect.Y += 0.25;
                    ct_MinVect.Y -= BData.Size_Bott;
                }
                else
                {
                    st_MinVect.Y += BData.Size_Bott;
                    gr_MinVect.Y += BData.Size_Bott;
                    if (BData.Folded)
                        ct_MinVect.Y -= StitchExtra(BData.st_Bott);
                    else
                        ct_MinVect.Y -= BData.Size_Bott + StitchExtra(BData.st_Bott);
                }
            }
            if (BData.fn_Side == BannerDialog.BannerInfo.Finish.Hem)
            {
                st_MaxVect.X -= 0.25;
                st_MinVect.X += 0.25;
                ct_MaxVect.X += BData.Size_Side;
                ct_MinVect.X -= BData.Size_Side;
            }

            gr_MaxVect.Y -= BData.gromEdgeOffset;
            gr_MinVect.Y += BData.gromEdgeOffset;
            gr_MaxVect.X -= BData.gromEdgeOffset;
            gr_MinVect.X += BData.gromEdgeOffset;

            // Create the boxes
            var BGeom = new BannerGeom
            {
                CutsBox = new BoundingBox(BaseBox.Min + ct_MinVect, BaseBox.Max + ct_MaxVect),
                LiveBox = new List<BoundingBox> { BaseBox },
                StitBox = new List<BoundingBox> { new BoundingBox(BaseBox.Min + st_MinVect, BaseBox.Max + st_MaxVect) },
                GromBox = new List<BoundingBox> { new BoundingBox(BaseBox.Min + gr_MinVect, BaseBox.Max + gr_MaxVect) }
            };

            return BGeom;
        }

        /// <summary>
        /// returns the proper extra material per the stitch
        /// </summary>
        /// <param name="stitch"></param>
        /// <returns></returns>
        public double StitchExtra(BannerDialog.BannerInfo.Stitch stitch)
        {
            if (stitch == BannerDialog.BannerInfo.Stitch.Single || stitch == BannerDialog.BannerInfo.Stitch.Weld)
                return 0.25;
            else if (stitch == BannerDialog.BannerInfo.Stitch.Double)
                return 0.5;

            return 0;
        }
    }
}