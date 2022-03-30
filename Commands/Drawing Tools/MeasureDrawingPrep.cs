using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.DocObjects;

namespace gjTools.Commands.Drawing_Tools
{
    public class MeasureDrawingPrep : Command
    {
        public MeasureDrawingPrep()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static MeasureDrawingPrep Instance { get; private set; }

        public override string EnglishName => "MeasureDrawingPrep";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            MeasureDrawingPrepare(doc);

            return Result.Success;
        }


        /// <summary>
        /// Layer out the Measured drawings
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public bool MeasureDrawingPrepare(RhinoDoc doc)
        {
            doc.Objects.UnselectAll();

            var txt = doc.Objects.FindByLayer("C_PTNO");
            var divPoint = new List<Point3d>();
            var lt = new LayerTools(doc);
            var md = new List<MeasureDrawing>
            {
                new MeasureDrawing { topLeftPt = new Point3d(-10, -1, 0) }
            };  // add an empty to the list

            // check the object for PN
            foreach (var t in txt)
            {
                var oref = new ObjRef(t).TextEntity();

                // if casting success
                if (oref == null)
                    continue;

                // if a PN Tag
                if (oref.PlainText.Substring(0, 3) != "PN:")
                    continue;

                md.Add(new MeasureDrawing
                {
                    pnTxt = oref,
                    pnTxtObj = t,
                    topLeftPt = new Point3d(-10, oref.GetBoundingBox(true).GetCorners()[2].Y + 0.25, 0)
                });
            }

            // Sort the point by Y coord
            md.Sort((x, y) => x.topLeftPt.Y.CompareTo(y.topLeftPt.Y));

            var garbageLays = new List<Layer>();

            // sort the parts
            for (int i = 1; i < md.Count; i++)
            {
                var mdObj = md[i];
                var ptObj = doc.Objects.FindByCrossingWindowRegion(
                    doc.Views.ActiveView.MainViewport,
                    mdObj.GetDiagRegion(md[i - 1].topLeftPt),
                    true,
                    ObjectType.Curve
                );

                // make pn layer
                mdObj.pnLayer = lt.CreateLayer(mdObj.GetPN);

                // add to garbage
                if (!garbageLays.Contains(doc.Layers[mdObj.pnTxtObj.Attributes.LayerIndex]))
                    garbageLays.Add(doc.Layers[mdObj.pnTxtObj.Attributes.LayerIndex]);

                // assign the text string to the pn layer
                mdObj.pnTxtObj.Attributes.LayerIndex = mdObj.pnLayer.Index;
                mdObj.pnTxtObj.CommitChanges();

                // add the objects and remove the pn text
                mdObj.cutObjs = new List<RhinoObject>(ptObj);

                // reassign the layer
                foreach (var o in mdObj.cutObjs)
                {
                    if (o == mdObj.pnTxtObj)
                        continue;

                    var curLay = doc.Layers[o.Attributes.LayerIndex];

                    var newLayer = lt.CreateLayer(
                        curLay.Name,
                        mdObj.pnLayer.Name,
                        curLay.Color
                    );

                    if (!garbageLays.Contains(curLay))
                        garbageLays.Add(curLay);

                    o.Attributes.LayerIndex = newLayer.Index;
                    o.CommitChanges();
                }

                md[i] = mdObj;
            }

            // remove the element that's empty
            md.RemoveAt(0);

            // delete the old layers
            if (garbageLays.Count > 0)
                foreach (var l in garbageLays)
                    doc.Layers.Delete(l.Index, true);

            return true;
        }



        public struct MeasureDrawing
        {
            public TextEntity pnTxt;
            public RhinoObject pnTxtObj;
            public Layer pnLayer;

            public List<RhinoObject> cutObjs;
            public Point3d topLeftPt;

            public string GetPN
            {
                get { return pnTxt.PlainText.Replace("PN:", "").Trim(); }
            }
            public List<Point3d> GetDiagRegion(Point3d pt)
            {
                return new List<Point3d> {
                topLeftPt,
                new Point3d(topLeftPt.X + 110, topLeftPt.Y, 0),
                new Point3d(pt.X + 110, pt.Y, 0),
                new Point3d(pt.X, pt.Y, 0)
            };
            }
        }
    }
}