using System;
using Rhino;
using Rhino.Commands;
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
        public struct LayerRhObjList
        {
            public Rhino.DocObjects.Layer rLayer;
            public Rhino.DocObjects.Layer cLayers;
            public Rhino.DocObjects.RhinoObject[] oList;
            public LayerRhObjList(Rhino.DocObjects.Layer rLayer, Rhino.DocObjects.RhinoObject[] oList, Rhino.DocObjects.Layer cLayers)
            {
                this.rLayer = rLayer;
                this.oList = oList;
                this.cLayers = cLayers;
            }
        }
        public struct EmbeddedLO
        {
            public Rhino.DocObjects.Layer pLayer;
            public List<LayerRhObjList> rl;
            public EmbeddedLO(Rhino.DocObjects.Layer pLayer, List<LayerRhObjList> rl)
            {
                this.pLayer = pLayer;
                this.rl = rl;
            }
        }
        public static BatchKerf Instance { get; private set; }

        public override string EnglishName => "gjBatchKerf";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            DialogTools d = new DialogTools(doc);
            LayerTools lt = new LayerTools(doc);
            var parents = lt.getAllParentLayers();
            var parentsString = new List<string>();
            var layerobj = new List<LayerRhObjList>();
            var embeddedl = new List<EmbeddedLO>();
            foreach (var p in parents)
            {
                parentsString.Add(p.Name);
            }
            var la = Rhino.UI.Dialogs.ShowMultiListBox("Layers", "Select a layer..", parentsString);
            if (la == null)
            {
                RhinoApp.WriteLine("Cancelled.");
                return Result.Cancel;
            }
            parents.Clear();
            foreach(var i in la)
            {
                var path = doc.Layers.FindByFullPath(i, -1);
                parents.Add(doc.Layers[path]);
            }
            foreach (var ly in parents)
            {
                var eo = new EmbeddedLO();
                layerobj = new List<LayerRhObjList>();
                var clayers = lt.getAllCutLayers(ly, false);
                if (clayers == null)
                {
                    RhinoApp.WriteLine("FAILED: No cut layers on one or more layers!");
                    return Result.Failure;
                }
                foreach (var cl in clayers)
                {
                    layerobj.Add(new LayerRhObjList(ly, doc.Objects.FindByLayer(cl), cl));
                }
                eo = new EmbeddedLO(ly, layerobj);
                embeddedl.Add(eo);
            }
            foreach (var lo in embeddedl)
            {
                string kerf = "";
                BoundingBox bb;
                var colist = new List<Rhino.DocObjects.RhinoObject>();
                foreach (var rl in lo.rl)
                {
                    kerf += rl.cLayers.Name + ": ";
                    int length = 0;
                    foreach (var cr in rl.oList)
                    {
                        var crv = new Rhino.DocObjects.ObjRef(cr).Curve();
                        if (crv == null)
                        {
                            RhinoApp.WriteLine("FAILED: Non-curve on cut layer! Check for annotations!");
                            return Result.Failure;
                        }
                        length += (int)crv.GetLength();
                        colist.Add(cr);
                    }
                    kerf += length.ToString() + "\n";
                }
                Rhino.DocObjects.RhinoObject.GetTightBoundingBox(colist, out bb);
                var crns = bb.GetCorners();
                Plane plane = doc.Views.ActiveView.ActiveViewport.ConstructionPlane();
                plane.Origin = crns[2];
                doc.Objects.AddText(kerf, plane, bb.GetEdges()[2].Length / 500, "Arial", false, false, TextJustification.BottomRight);
            }
            doc.Views.Redraw();
            return Result.Success;
        }
    }
}