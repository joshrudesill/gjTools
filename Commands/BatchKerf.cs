using System;
using Rhino;
using Rhino.Commands;
using System.Collections.Generic;

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
            public List<Rhino.DocObjects.Layer> cLayers;
            public Rhino.DocObjects.RhinoObject[] oList;
            public LayerRhObjList(Rhino.DocObjects.Layer rLayer, Rhino.DocObjects.RhinoObject[] oList, List<Rhino.DocObjects.Layer> cLayers)
            {
                this.rLayer = rLayer;
                this.oList = oList;
                this.cLayers = cLayers;
            }
        }
        public static BatchKerf Instance { get; private set; }

        public override string EnglishName => "BatchKerf";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            DialogTools d = new DialogTools(doc);
            LayerTools lt = new LayerTools(doc);
            var go = d.selectObjects("");
            var parents = lt.getAllParentLayers();
            var parentsString = new List<string>();
            var layerobj = new List<LayerRhObjList>();
            foreach (var p in parents)
            {
                parentsString.Add(p.Name);
            }
            var la = Rhino.UI.Dialogs.ShowMultiListBox("Layers", "Select a layer..", parentsString);
            parents.Clear();
            foreach(var i in la)
            {
                parents.Add(doc.Layers[doc.Layers.FindByFullPath(i, -1)]);
            }
            foreach (var ly in parents)
            {
                layerobj.Add(new LayerRhObjList(ly, doc.Objects.FindByLayer(ly), lt.getAllCutLayers(ly, false)));
            }
            return Result.Success;
        }
    }
}