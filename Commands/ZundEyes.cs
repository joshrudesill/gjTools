using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using System;
namespace gjTools.Commands
{
    public class ZundEyes : Command
    {
        public ZundEyes()
        {
            Instance = this;
        }

        public static ZundEyes Instance { get; private set; }

        public override string EnglishName => "ZundEyes";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            const Rhino.DocObjects.ObjectType filter = Rhino.DocObjects.ObjectType.Curve;
            Rhino.DocObjects.ObjRef objref;
            Result rc = Rhino.Input.RhinoGet.GetOneObject("Select box to add eyes to..", false, filter, out objref);
            Curve crv = objref.Curve();
            BoundingBox bb = crv.GetBoundingBox(true);

            //----Layering----//
            
            
            int layer = objref.Object().Attributes.LayerIndex;
            Rhino.DocObjects.Layer pli = doc.Layers[layer];
            if (pli.ParentLayerId != Guid.Empty)
            {
                var player = doc.Layers.FindId(pli.ParentLayerId);
                pli = player;
            }
            Rhino.DocObjects.Layer l1 = new Rhino.DocObjects.Layer();
            l1.Name = "C_EYES";
            l1.ParentLayerId = pli.Id;
            int l1index = doc.Layers.Add(l1);

            Rhino.DocObjects.Layer l2 = new Rhino.DocObjects.Layer();
            l2.Name = "EYE_FILL";
            l2.ParentLayerId = pli.Id;
            int l2index = doc.Layers.Add(l2);

            // Get corners
            Point3d[] corners = bb.GetCorners();

            //------ Math -------//
            double topD = Math.Abs(corners[3].X - corners[2].X);
            double numEyesT = Math.Floor((topD - 1.3) / 24);
            double spacingT = (topD - 1.3) / (numEyesT);
            //----------------------
            double sideD = Math.Abs(corners[3].Y - corners[0].Y);
            double numEyesS = Math.Floor((sideD - 1.3) / 24);
            double spacingS = (sideD - 1.3) / (numEyesS);
            //--------------------------------

            Point3d first = new Point3d((corners[3].X + 0.65), (corners[3].Y - 0.65), 0);
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
            Circle fc = new Circle(new Point3d(corners[1].X - 1.65, corners[1].Y + 0.65, 0), 0.125);
            createHatchOnLayer(fc, l2index, l1index, doc);

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
    }
}