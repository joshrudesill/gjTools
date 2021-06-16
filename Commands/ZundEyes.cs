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

        public override string EnglishName => "ZundEyesgj";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            const Rhino.DocObjects.ObjectType filter = Rhino.DocObjects.ObjectType.Curve;
            Rhino.DocObjects.ObjRef objref;
            Result rc = Rhino.Input.RhinoGet.GetOneObject("Select curve to divide", false, filter, out objref);
            Curve crv = objref.Curve();
            BoundingBox bb = crv.GetBoundingBox(true);

            //----Layering----//
            int layer = objref.Object().Attributes.LayerIndex;
            Rhino.DocObjects.Layer pli = doc.Layers[layer];
            Rhino.DocObjects.Layer l1 = new Rhino.DocObjects.Layer();
            l1.Name = "C_EYES";
            l1.ParentLayerId = pli.Id;
            int l1index = doc.Layers.Add(l1);
            RhinoApp.WriteLine(doc.Layers.SetCurrentLayerIndex(l1index, true).ToString());
            //--------//
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
            
            for (int i = 0; i < numEyesT + 1; i++)
            {
                Circle c1 = new Circle(first, 0.125);
                doc.Objects.AddCircle(c1);
                var cu = c1.ToNurbsCurve();
                var hatches = Hatch.Create(cu, doc.HatchPatterns.CurrentHatchPatternIndex, 0, 1.0, 1.0);
                doc.Objects.AddHatch(hatches[0]);
                for (int j = 0; j < numEyesS; j++)
                {
                    first.Y -= spacingS;
                    Circle c2 = new Circle(first, 0.125);
                    doc.Objects.AddCircle(c2);
                    var cu1 = c2.ToNurbsCurve();
                    var hatches1 = Hatch.Create(cu1, doc.HatchPatterns.CurrentHatchPatternIndex, 0, 1.0, 1.0);
                    doc.Objects.AddHatch(hatches1[0]);
                }
                first.Y = corners[3].Y - 0.65;
                first.X += spacingT;
            }

            doc.Views.Redraw();
            return Result.Success;
        }
    }
}