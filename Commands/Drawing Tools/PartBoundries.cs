using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;

namespace gjTools.Commands.Drawing_Tools
{
    public class PartBoundries : Command
    {
        public PartBoundries()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static PartBoundries Instance { get; private set; }

        public override string EnglishName => "PartBoundries";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var Disp = new Rhino.Display.CustomDisplay(true);
            var colorStroke = System.Drawing.Color.FromArgb(255, 0, 225, 255);
            var colorFill = System.Drawing.Color.FromArgb(15, 0, 225, 255);
            
            foreach (Layer docLays in doc.Layers)
            {
                if (!docLays.IsVisible || docLays.IsDeleted || docLays.ParentLayerId != Guid.Empty)
                    continue;

                // some exclusion layer names
                if (docLays.Name == "VOMELA_TitleBlock" || docLays.Name == "NestBoxes" || docLays.Name == "Temp")
                    continue; 

                var pcl = new ParentChildLayers(docLays);
                BoundingBox bb = pcl.GetBoundry(doc);

                if (!bb.Equals(BoundingBox.Empty))
                {
                    var pts = new List<Point3d>(bb.GetCorners());
                    Disp.AddPolygon(pts.GetRange(0, 5), colorFill, colorStroke, true, true);
                }
            }
            doc.Views.Redraw();

            string Fuck = "";
            RhinoGet.GetString("Press Enter to Close..", true, ref Fuck);
            Disp.Enabled = false;
            Disp.Dispose();

            doc.Views.Redraw();
            return Result.Success;
        }


        public struct ParentChildLayers
        {
            public Layer Parent;
            public List<Layer> ChildLayers;

            public ParentChildLayers(Layer parentLayer)
            {
                Parent = parentLayer;

                ChildLayers = new List<Layer>();
                var cl = Parent.GetChildren();
                if (cl != null)
                    ChildLayers.AddRange(cl);
            }

            public BoundingBox GetBoundry(RhinoDoc doc)
            {
                BoundingBox bb = BoundingBox.Empty;

                var pObj = doc.Objects.FindByLayer(Parent);
                if (pObj != null)
                    RhinoObject.GetTightBoundingBox(pObj, out bb);

                BoundingBox childbb = BoundingBox.Empty;
                foreach(Layer cl in ChildLayers)
                {
                    var cObj = doc.Objects.FindByLayer(cl);
                    if (cObj != null)
                    {
                        RhinoObject.GetTightBoundingBox(cObj, out childbb);
                        bb.Union(childbb);
                    }
                }

                return bb;
            }
        }
    }
}