using System;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.DocObjects;
namespace gjTools.Commands
{
    public class MakeParentLayerActive : Command
    {
        public MakeParentLayerActive()
        {
            Instance = this;
        }

        public static MakeParentLayerActive Instance { get; private set; }

        public override string EnglishName => "MakeParentLayerActive";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            if (RhinoGet.GetMultipleObjects("Select Object(s) to change parent layer", false, ObjectType.Curve | ObjectType.Annotation, out ObjRef[] obj) != Result.Success)
                return Result.Cancel;

            // get the layer
            Layer lay = doc.Layers[obj[0].Object().Attributes.LayerIndex];
            lay = (lay.ParentLayerId == Guid.Empty) ? lay : doc.Layers.FindId(lay.ParentLayerId);
            
            // set the layer
            doc.Layers.SetCurrentLayerIndex(lay.Index, true);

            // print the layer
            RhinoApp.Write($"Current Layer: {lay.Name}");
            
            return Result.Success;
        }
    }
}