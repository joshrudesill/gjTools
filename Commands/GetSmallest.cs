using System;
using Rhino;
using Rhino.Commands;
using System.Collections.Generic;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.DocObjects;

namespace gjTools.Commands
{
    struct BRotation
    {
        public BRotation(double rotation, double area)
        {
            this.rotation = rotation;
            this.area = area;
        }
        public double rotation;
        public double area;
    }
    public class GetSmallest : Command
    {
        public GetSmallest()
        {
            Instance = this;
        }
        /// <summary>
        /// instance
        /// </summary>
        public static GetSmallest Instance { get; private set; }
        /// <summary>
        /// eng name
        /// </summary>
        public override string EnglishName => "gjGetSmallest";

        /// <summary>
        /// Class
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            if (RhinoGet.GetMultipleObjects("Select object to get smallest rotation..", false, ObjectType.AnyObject, out ObjRef[] ids) != Result.Success)
                return Result.Cancel;

            List<BRotation> brl = new List<BRotation>();

            BoundingBox bb;
            BoundingBox bbt;

            var idsol = new List<RhinoObject>();
            foreach (var o in ids)
                idsol.Add(o.Object());
            
            RhinoObject.GetTightBoundingBox(idsol, out bb);
            brl.Add(new BRotation(0, bb.Area));
            
            double rotation = (2 * Math.PI) / 7200;
            double height = 10000000000000080085;
            int index = 0;

            for (int j = 0; j < 3600; j++)
            {
                idsol.Clear();
                var xf = Transform.Rotation(rotation * j, bb.Center);
                foreach (var ob in ids)
                {
                    var o1 = ob.Curve();
                    o1.Transform(xf);
                    idsol.Add(doc.Objects.FindId(doc.Objects.AddCurve(o1)));
                }
                RhinoObject.GetTightBoundingBox(idsol, out bbt);
                brl.Add(new BRotation(j * rotation, bbt.Area));
                foreach(var u in idsol)
                {
                    doc.Objects.Delete(u);
                }
            }

            for (int i = 0; i < brl.Count; i++)
            {
                if (brl[i].area < height)
                {
                    height = brl[i].area;
                    index = i;
                }
            }

            var r = Transform.Rotation(brl[index].rotation, bb.Center);
            foreach (var i in ids)
            {
                doc.Objects.Transform(i, r, true);
            }

            RhinoApp.WriteLine("Rotated " + brl[index].rotation.ToString() + " radians");
            RhinoApp.WriteLine("Area (bounding box) is now equal to " + brl[index].area.ToString() + "inches squared");

            doc.Views.Redraw(); 
            return Result.Success;
        }
    }
}