using System;
using Rhino;
using Rhino.Commands;
using System.Collections.Generic;
using Rhino.Geometry;
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
            Rhino.Input.Custom.GetObject go = new Rhino.Input.Custom.GetObject();
            go.SetCommandPrompt("Select object to get smallest rotation..");
            Rhino.Input.GetResult gr = go.GetMultiple(0, -1);
            if (gr != Rhino.Input.GetResult.Object)
            {
                RhinoApp.WriteLine("No objects selected. Command canceled");
                return Result.Cancel;
            }
            List<Rhino.DocObjects.RhinoObject> ids = new List<Rhino.DocObjects.RhinoObject>();
            for (int i = 0; i < go.ObjectCount; i++)
            {
                Rhino.DocObjects.RhinoObject robj = go.Object(i).Object();
                ids.Add(robj);
            }
            BoundingBox bb;
            Rhino.DocObjects.RhinoObject.GetTightBoundingBox(ids, out bb);
            List<BRotation> brl = new List<BRotation>();
            brl.Add(new BRotation(0, bb.Area));
            double rotation = (2 * Math.PI) / 7200;
            BoundingBox bbt;
            for (int j = 0; j < 3600; j++)
            {
                List<Rhino.DocObjects.RhinoObject> ol = new List<Rhino.DocObjects.RhinoObject>();
                List<Rhino.DocObjects.RhinoObject> ol2 = new List<Rhino.DocObjects.RhinoObject>(ids);
                var xf = Transform.Rotation(rotation * j, bb.Center);
                RhinoApp.WriteLine(ids.Count.ToString());
                foreach (var ob in ol2)
                {
                    Guid id = doc.Objects.Transform(ob, xf, true);
                    var toadd = doc.Objects.FindId(id);
                    if (toadd == null)
                    {
                        RhinoApp.WriteLine("Null");
                    }
                    ol.Add(toadd);
                }
                ol2 = ids;
                RhinoApp.WriteLine(ol.Count.ToString());
                Rhino.DocObjects.RhinoObject.GetTightBoundingBox(ol, out bbt);
                brl.Add(new BRotation(j * rotation, bbt.Area));
            }
            double height = 10000000000000080085;
            int index = 0;
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
            doc.Views.Redraw(); 
            return Result.Success;
        }
    }
}