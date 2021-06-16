using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
namespace gjTools.Commands
{
#region Class
    public class XYDims : Command
    {
        public XYDims()
        {
            Instance = this;
        }

        public static XYDims Instance { get; private set; }

        public override string EnglishName => "gjXYDims";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            Rhino.Input.Custom.GetObject go = new Rhino.Input.Custom.GetObject();
            go.SetCommandPrompt("Select objects to dimension..");
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
            string s = "Dimension Level";       
            double dimlevel = 1;                
            Rhino.Input.RhinoGet.GetNumber(s, true, ref dimlevel, 0, 3);
                                                
            Plane p = new Plane(new Point3d(0, 0, 0), new Point3d(0, 1, 0), new Point3d(1, 0, 0));
            var dimension = LinearDimension.Create(at, ds, p, new Vector3d(1,0,0), ps[0], ps[3], new Point3d(ps[0].X - (2 * dimlevel), 0, 0), 0.0);
                                                
            p.Rotate(Math.PI / 2, new Vector3d(0, 0, 1));
            var dimensionX = LinearDimension.Create(at, ds, p, new Vector3d(1, 0, 0), ps[0], ps[1], new Point3d(0, ps[0].Y - (2 * dimlevel), 0), 0.0);
                                                
            doc.Objects.AddLinearDimension(dimension);
            doc.Objects.AddLinearDimension(dimensionX);
                                                    
            doc.Views.Redraw();                     
            return Result.Success;                  
        }                                           
    }                                               
}                                                   
#endregion                                          