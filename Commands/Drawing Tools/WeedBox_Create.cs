using System;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using System.Collections.Generic;
using Rhino.Geometry;
using Rhino.Input;


namespace gjTools.Commands
{
    public class WeedBox_Create : Command
    {
        public WeedBox_Create()
        {
            Instance = this;
        }

        public static WeedBox_Create Instance { get; private set; }

        public override string EnglishName => "WeedBox_Create";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            if (RhinoGet.GetMultipleObjects("Select Object to Test", false, ObjectType.Curve, out ObjRef[] objRefs) != Result.Success)
                return Result.Cancel;

            // Get the list of curves
            var crvs = new List<Curve>(objRefs.Length);
            foreach (var o in objRefs)
            {
                var crv = o.Curve();

                // Dont include object if it only has 1 segment
                if (crv.DuplicateSegments().Length == 1)
                    continue;

                // make closed box curve
                if (!crv.IsClosed)
                {
                    // create a polylinecurve object from bounding box
                    var pts = crv.GetBoundingBox(true).GetCorners();
                    var corners = new List<Point3d>(5)
                    {
                        pts[0], pts[1], pts[2], pts[3], pts[0]
                    };
                    crv = new PolylineCurve(corners);
                }

                crvs.Add(crv);
            }

            if (crvs.Count == 0)
            {
                RhinoApp.WriteLine("Could not make weedbox based on selection...");
                return Result.Cancel;
            }

            var weedBoxRegions = Curve.CreateBooleanUnion(crvs, 1);
            if (weedBoxRegions.Length == 0)
            {
                RhinoApp.WriteLine("Failed to make weedbox...");
                return Result.Failure;
            }

            var offsetCurve = weedBoxRegions[0].Offset(Plane.WorldXY, 0.1, 0.001, CurveOffsetCornerStyle.Sharp)[0];

            // find the layer that the object is on
            Layer lay = doc.Layers[objRefs[0].Object().Attributes.LayerIndex];
            if (lay.ParentLayerId != Guid.Empty)
                lay = doc.Layers.FindId(lay.ParentLayerId);

            // setup the attributes
            ObjectAttributes attr = new ObjectAttributes()
            {
                Name = "WeedBox",
                LayerIndex = lay.Index
            };

            // get the child layers and make sure the kiss layer isnt already present
            var cLays = lay.GetChildren();
            Layer kLay = null;
            if (cLays != null)
            {
                // check if kiss is part of the list
                foreach (Layer l in cLays)
                {
                    if (l.Name == "C_KISS")
                    {
                        kLay = l;
                        attr.LayerIndex = l.Index;
                        break;
                    }
                }
            }

            // do we need to create?
            if (kLay == null)
            {
                // create the layer
                kLay = new Layer()
                {
                    Name = "C_KISS",
                    Color = System.Drawing.Color.FromArgb(255,200,0,200),
                    ParentLayerId = lay.Id
                };
                attr.LayerIndex = doc.Layers.Add(kLay);
            }

            // add to the document
            doc.Objects.AddCurve(offsetCurve, attr);
            doc.Views.Redraw();

            return Result.Success;
        }
    }
}