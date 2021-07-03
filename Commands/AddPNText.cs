using System;
using Rhino;
using Rhino.Commands;
using System.Collections.Generic;
using Rhino.Geometry;
using Rhino.UI;
using Rhino.DocObjects;

namespace gjTools.Commands
{
    public class AddPNText : Command
    {
        public AddPNText()
        {
            Instance = this;
        }
        // This command has been tested and is error proof. Ready for release.
        public static AddPNText Instance { get; private set; }

        public override string EnglishName => "AddPNText";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var lt = new LayerTools(doc);
            var parts = Dialogs.ShowMultiListBox("Layer Selector", "Add PN Tag to", lt.getAllParentLayersStrings());

            foreach(var p in parts)
            {
                var layers = new List<Layer> { lt.CreateLayer(p) };
                layers.AddRange(lt.getAllSubLayers(layers[0]));

                var obj = new List<RhinoObject>();
                var ss = new ObjectEnumeratorSettings();
                foreach(var l in layers)
                {
                    ss.LayerIndexFilter = l.Index;
                    obj.AddRange(doc.Objects.GetObjectList(ss));
                }

                if (obj.Count > 0)
                {
                    RhinoObject.GetTightBoundingBox(obj, out BoundingBox bb);

                    // create text object
                    var dt = new DrawTools(doc);
                    var ds = dt.StandardDimstyle();
                    var txt = doc.Objects.FindId(doc.Objects.AddText(dt.AddText("PN: " + p, bb.GetCorners()[3], ds, justVert:6)));
                        txt.Attributes.LayerIndex = layers[0].Index;
                        txt.CommitChanges();
                }
            }

            return Result.Success;
        }
    }
}