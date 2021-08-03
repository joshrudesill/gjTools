using System;
using Rhino;
using Rhino.Commands;
using gjTools.Helpers;
using Rhino.DocObjects;
using System.Collections.Generic;
using Rhino.Geometry;
namespace gjTools.Commands
{
    public class BatchKerf : Command
    {
        public BatchKerf()
        {
            Instance = this;
        }

        public static BatchKerf Instance { get; private set; }

        public override string EnglishName => "BatchKerf";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var lt = new LayerTools(doc);
            RhinoApp.WriteLine("1");
            var ld = lt.getAllLayerData();
            RhinoApp.WriteLine("1");
            var ldp = new List<string>();
            var ldsorted = new List<LayerData>();
            foreach (var lad in ld)
            {
                ldp.Add(lad.layerdata.Item1.Name);
            }
            var la = Rhino.UI.Dialogs.ShowMultiListBox("Layers", "Select a layer..", ldp);
            RhinoApp.WriteLine("1");
            if (la == null)
            {
                RhinoApp.WriteLine("Cancelled.");
                return Result.Cancel;
            }
            foreach (var j in ld)
            {
                if (new List<string>(la).Contains(j.layerdata.Item1.Name))
                {
                    ldsorted.Add(j);
                }
            }
            foreach(var lds in ldsorted)
            {
                string sta = "";
                foreach (var sl in lds.layerdata.Item2)
                {
                    sta += sl.Item1.Name.Replace("C_", "KERF-") + ": ";
                    int kerf = 0;
                    foreach (var ob in sl.Item2)
                    {
                        if (ob.obRef.Curve() != null)
                        {
                            kerf += (int)ob.obRef.Curve().GetLength();
                        }
                    }
                    sta += kerf.ToString() + "\n";
                }
                var bb = lds.getBoundingBoxofParent();
                var crns = bb.GetCorners();
                Plane plane = doc.Views.ActiveView.ActiveViewport.ConstructionPlane();
                plane.Origin = crns[2];
                doc.Layers.SetCurrentLayerIndex(lds.layerdata.Item1.Index, true);
                doc.Objects.AddText(sta, plane, bb.GetEdges()[2].Length / 500, "Arial", false, false, TextJustification.BottomRight);
            }
            doc.Views.Redraw();
            return Result.Success;
        }
    }
}