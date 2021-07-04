using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.DocObjects;
using Rhino.Input;

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
            if (RhinoGet.GetMultipleObjects("Select object(s) to get dims", false, ObjectType.AnyObject, out ObjRef[] go) != Result.Success)
                return Result.Cancel;

            List<RhinoObject> ids = new List<RhinoObject>();
            for (int i = 0; i < go.Length; i++)
                ids.Add(go[i].Object());
            
            BoundingBox bb;
            RhinoObject.GetTightBoundingBox(ids, out bb);
            Point3d[] ps = bb.GetCorners();     
            DimensionStyle ds = doc.DimStyles.Current;
            AnnotationType at = AnnotationType.Rotated;
            string s = "Dimension Level";       
            double dimlevel = 1;                
            RhinoGet.GetNumber(s, true, ref dimlevel, 0, 3);
                                                
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