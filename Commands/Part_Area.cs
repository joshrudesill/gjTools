using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace gjTools.Commands
{
    public class Part_Area : Command
    {
        public Part_Area()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static Part_Area Instance { get; private set; }

        public override string EnglishName => "CalcPartArea";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // Collect some objects
            if (RhinoGet.GetMultipleObjects("Select Objects", false, ObjectType.Curve, out ObjRef[] obj) != Result.Success)
                return Result.Cancel;

            // Make some Hatches
            var hatch = MakeHatch(new List<ObjRef>(obj), out double area, out BoundingBox bb);

            // Make the text
            var dt = new DrawTools(doc);
            var ds = dt.StandardDimstyle();
            var pt = bb.GetCorners()[2];
                pt.Y += 0.75;
            var txt = dt.AddText($"Hatch Area: {area} Sq.In", pt, ds, 0.5, 0, 2, 6);

            // Find the layer
            var parentLayer = doc.Layers[obj[0].Object().Attributes.LayerIndex];
            if (parentLayer.ParentLayerId != Guid.Empty)
                parentLayer = doc.Layers.FindId(parentLayer.ParentLayerId);

            // add the objects
            var atts = new ObjectAttributes { LayerIndex = parentLayer.Index };
            doc.Objects.AddText(txt, atts);

            if (hatch.Length > 1)
                atts.AddToGroup(doc.Groups.Add()); // Make hatch a group

            foreach (var h in hatch)
                doc.Objects.AddHatch(h, atts);

            return Result.Success;
        }


        /// <summary>
        /// Makes a hatch object
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="area"></param>
        /// <param name="bb"></param>
        /// <returns>Hatch Array, area & Bounding are byproducts</returns>
        public Hatch[] MakeHatch(List<ObjRef> obj, out double area, out BoundingBox bb)
        {
            var doc = obj[0].Object().Document;
            var crvs = new List<Curve>();
            bb = obj[0].Geometry().GetBoundingBox(true);

            // get curve objects
            foreach (var o in obj)
            {
                crvs.Add(o.Curve());
                bb.Union(o.Geometry().GetBoundingBox(true));
            }
            
            // Make me some hatch
            var hatch = Hatch.Create(crvs, doc.HatchPatterns.FindName("Grid60").Index, 0, 1.5, doc.ModelAbsoluteTolerance);
            
            // calc the area
            area = 0;
            foreach(var h in hatch)
                area += AreaMassProperties.Compute(h).Area;

            area = Math.Round(area, 2);

            return hatch;
        }
    }
}