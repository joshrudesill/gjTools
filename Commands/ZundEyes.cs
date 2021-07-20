using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using System;
using Rhino.DocObjects;
using System.Collections.Generic;
namespace gjTools.Commands
{
    public class ZundEyes : Command
    {
        public ZundEyes()
        {
            Instance = this;
        }
        int spacingDivNumber = 24;
        double spacingFromSide = (0.65 * 2);

        int l2index;
        int l1index;

        double topD;
        double numEyesT;
        double spacingT;

        double sideD;
        double numEyesS;
        double spacingS;

        ObjRef nestboxref;
        public static ZundEyes Instance { get; private set; }

        public override string EnglishName => "ZundEyes";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            const ObjectType filter = ObjectType.Curve;
            List<RhinoObject> robj = new List<RhinoObject>();
            Result rc = Rhino.Input.RhinoGet.GetMultipleObjects("Select box(es) to add eyes to..", false, filter, out ObjRef[] objref);
            if (rc != Result.Success) { return Result.Cancel; }
            BoundingBox bb = objref[0].Geometry().GetBoundingBox(true);
            bool nestbox = false;
            nestboxref = objref[0];
            foreach (var or in objref)
            {
                bb.Union(or.Geometry().GetBoundingBox(true));
                if (!nestbox && doc.Layers[or.Object().Attributes.LayerIndex].Name == "NestBox")
                {
                    nestbox = true;
                    nestboxref = or;
                }
            }
            //----Layering----//

            createLayers(doc);

            // Get corners
            Point3d[] corners = bb.GetCorners();

            //------ Math -------//
            calcPlacement(corners);

            //----------------Draw Eyes----------------//
            drawEyes(corners, doc);

            doc.Views.Redraw();
            return Result.Success;
        }



        /// <summary>
        /// Internal use only. Private function
        /// </summary>
        /// <param name="c"></param>
        /// <param name="layer1"></param>
        /// <param name="layer2"></param>
        /// <param name="doc"></param>
        void createHatchOnLayer(Circle c, int layer1, int layer2, RhinoDoc doc)
        {
            doc.Objects.AddCircle(c);
            var cu = c.ToNurbsCurve();
            var hatches = Hatch.Create(cu, doc.HatchPatterns.CurrentHatchPatternIndex, 0, 1.0, 1.0);
            doc.Layers.SetCurrentLayerIndex(layer1, true);
            doc.Objects.AddHatch(hatches[0]);
            doc.Layers.SetCurrentLayerIndex(layer2, true);
        }
        void createLayers(RhinoDoc doc)
        {
            int layer = nestboxref.Object().Attributes.LayerIndex;
            Layer pli = doc.Layers[layer];
            if (pli.ParentLayerId != Guid.Empty)
            {
                var player = doc.Layers.FindId(pli.ParentLayerId);
                pli = player;
            }
            Layer l1 = new Layer();
            l1.Name = "C_EYES";
            l1.ParentLayerId = pli.Id;
            l1index = doc.Layers.Add(l1);

            Layer l2 = new Layer();
            l2.Name = "EYE_FILL";
            l2.ParentLayerId = pli.Id;
            l2index = doc.Layers.Add(l2);
        }
        void calcPlacement(Point3d[] corners)
        {
            topD = Math.Abs(corners[3].X - corners[2].X);
            numEyesT = Math.Floor((topD - spacingFromSide) / spacingDivNumber);
            if (numEyesT < 1) { numEyesT = 1; }
            spacingT = (topD - spacingFromSide) / (numEyesT);
            //----------------------
            sideD = Math.Abs(corners[3].Y - corners[0].Y);
            numEyesS = Math.Floor((sideD - spacingFromSide) / spacingDivNumber);
            if (numEyesS < 1) { numEyesS = 1; }
            if (numEyesS > 2) { numEyesS = 2; }
            spacingS = (sideD - spacingFromSide) / (numEyesS);
        }
        void drawEyes(Point3d[] corners, RhinoDoc doc)
        {
            Point3d first = new Point3d((corners[3].X + (spacingFromSide / 2)), (corners[3].Y - (spacingFromSide / 2)), 0);
            doc.Layers.SetCurrentLayerIndex(l1index, true);
            for (int i = 0; i < numEyesT + 1; i++)
            {
                Circle c1 = new Circle(first, 0.125);
                createHatchOnLayer(c1, l2index, l1index, doc);
                for (int j = 0; j < numEyesS; j++)
                {
                    first.Y -= spacingS;
                    Circle c2 = new Circle(first, 0.125);
                    createHatchOnLayer(c2, l2index, l1index, doc);
                }
                first.Y = corners[3].Y - 0.65;
                first.X += spacingT;
            }
            Circle fc = new Circle(new Point3d(corners[1].X - 1 - (spacingFromSide / 2), corners[1].Y + (spacingFromSide / 2), 0), 0.125);
            createHatchOnLayer(fc, l2index, l1index, doc);
        }
    }
}