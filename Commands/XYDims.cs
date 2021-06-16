using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
namespace gjTools.Commands
{
    public class XYDims : Command
    {
        public XYDims()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static XYDims Instance { get; private set; }

        public override string EnglishName => "gjXYDims";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            Rhino.Input.Custom.GetObject go = new Rhino.Input.Custom.GetObject();
            go.SetCommandPrompt("Select objects dimension..");
            go.GetMultiple(1, 0);
            List<Rhino.DocObjects.RhinoObject> ids = new List<Rhino.DocObjects.RhinoObject>();
            for (int i = 0; i < go.ObjectCount; i++)
            {
                Rhino.DocObjects.RhinoObject ro = go.Object(i).Object();
                ids.Add(ro);
            }
            BoundingBox bb;
            Rhino.DocObjects.RhinoObject.GetTightBoundingBox(ids, out bb);
            Point3d[] ps = bb.GetCorners();
            Rhino.DocObjects.DimensionStyle ds = doc.DimStyles.Current;
            AnnotationType at = AnnotationType.Rotated;
            var dimension = Rhino.Geometry.LinearDimension.Create(at, ds, Plane.WorldXY, new Vector3d(1,0,0), ps[0], ps[3], new Point3d(0,0,0), 0.0);
            doc.Objects.AddLinearDimension(dimension);
            doc.Views.Redraw();
            return Result.Success;
        }
    }
}