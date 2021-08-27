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

            var BData = BannerData.GetAllValues();

            var BGeom = CreateBoxes(BData);

            BGeom = GrommetPoints(BGeom, BData);

            BGeom = CreateGromCircles(BGeom, BData);

            if (BData.Folded)
                BGeom = MakeFolded(BGeom);

            //var dt = new DrawTools(doc);
            //BGeom = CreateTextLabels(dt, BGeom, BData);

            AddAllObjects(doc, BGeom, BData);

            doc.Views.Redraw();
            return Result.Success;
        }

        public void AddAllObjects(RhinoDoc doc, BannerGeom BGeom, BannerDialog.BannerInfo BData)
        {
            var attr = new ObjectAttributes { LayerIndex = doc.Layers.CurrentLayer.Index };
            var lt = new LayerTools(doc);

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

            if (BData.fn_Top != BannerDialog.BannerInfo.Finish.None)
            {
                if (BData.fn_Top == BannerDialog.BannerInfo.Finish.Hem)
                {

                    BGeom.Verbage.Add(dt.AddText("HEM", Point3d.Origin, ds, 1, 3, 1, 0));
                }
            }

            return BGeom;
        }

        public BannerGeom MakeFolded(BannerGeom BGeom)
        {
            var mirror = Transform.Mirror(new Plane(BGeom.LiveBox[0].Max, Vector3d.YAxis));
            
            BGeom.LiveBox.Add(new BoundingBox(BGeom.LiveBox[0].GetCorners(), mirror));
            BGeom.StitBox.Add(new BoundingBox(BGeom.StitBox[0].GetCorners(), mirror));
            BGeom.GromBox.Add(new BoundingBox(BGeom.GromBox[0].GetCorners(), mirror));


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
            {
                var CleanPts = new List<Point3d>();

                foreach(var p in BGeom.GromPts)
                {
                    bool uniq = true;
                    foreach (var pp in CleanPts)
                        if (p.Equals(pp))
                            uniq = false;
                    if (uniq)
                        CleanPts.Add(p);
                }

                BGeom.GromPts = CleanPts;
            }

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
                    gr_MaxVect.Y -= BData.gromEdgeOffset;
                }
                else
                {
                    st_MaxVect.Y -= BData.Size_Top;
                    gr_MaxVect.Y -= BData.Size_Top + BData.gromEdgeOffset;
                    ct_MaxVect.Y += BData.Size_Top + StitchExtra(BData.st_Top);
                }
            }
            if (BData.fn_Bott != BannerDialog.BannerInfo.Finish.None)
            {
                if (BData.fn_Bott == BannerDialog.BannerInfo.Finish.Hem)
                {
                    st_MinVect.Y += 0.25;
                    ct_MinVect.Y -= BData.Size_Bott;
                    gr_MinVect.Y += BData.gromEdgeOffset;
                }
                else
                {
                    st_MinVect.Y += BData.Size_Bott;
                    gr_MinVect.Y += BData.Size_Bott + BData.gromEdgeOffset;
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
                gr_MaxVect.X -= BData.gromEdgeOffset;
                gr_MinVect.X += BData.gromEdgeOffset;
            }

            // Create the boxes
            var BGeom = new BannerGeom
            {
                CutsBox = new BoundingBox(BaseBox.Min + ct_MinVect, BaseBox.Max + ct_MaxVect),
                LiveBox = new List<BoundingBox> { BaseBox },
                StitBox = new List<BoundingBox> { new BoundingBox(BaseBox.Min + st_MinVect, BaseBox.Max + st_MaxVect) },
                GromBox = new List<BoundingBox> { new BoundingBox(BaseBox.Min + gr_MinVect, BaseBox.Max + gr_MaxVect) }
            };

            double StitchExtra(BannerDialog.BannerInfo.Stitch stitch)
            {
                if (stitch == BannerDialog.BannerInfo.Stitch.Single)
                    return 0.25;
                if (stitch == BannerDialog.BannerInfo.Stitch.Double)
                    return 0.5;
                
                return 0;
            }

            return BGeom;
        }
    }
}