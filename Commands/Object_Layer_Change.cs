using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Input;
using Rhino.UI;

namespace gjTools.Commands
{
    public class Object_Layer_Change : Command
    {
        public Object_Layer_Change()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static Object_Layer_Change Instance { get; private set; }

        public override string EnglishName => "ObjectsLayerChange";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            if (RhinoGet.GetMultipleObjects("Select Objects to change layers", false, ObjectType.AnyObject, out ObjRef[] obj) != Result.Success)
                return Result.Cancel;

            // get the layer list
            var ParentLayers = new List<Layer>();
            foreach (Layer l in doc.Layers)
                if (!l.IsDeleted)
                if (l.ParentLayerId == Guid.Empty && l.Name.Length > 0)
                    ParentLayers.Add(l);

            // get the desired layer
            var chosen = Dialogs.ShowListBox("Change Layer", "Choose a layer to move objects to", ParentLayers);
            if (chosen == null)
                return Result.Cancel;

            Layer chosenLayer = chosen as Layer;
            var lt = new LayerTools(doc);

            foreach (ObjRef o in obj)
            {
                var ro = o.Object();
                var roLay = doc.Layers[ro.Attributes.LayerIndex];

                if (roLay.ParentLayerId == Guid.Empty)
                {
                    ro.Attributes.LayerIndex = chosenLayer.Index;
                    ro.CommitChanges();
                }
                else
                {
                    var newLay = lt.CreateLayer(roLay.Name, chosenLayer.Name, roLay.Color);
                    ro.Attributes.LayerIndex = newLay.Index;
                    ro.CommitChanges();
                }
            }

            return Result.Success;
        }
    }
}