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
            var lt = new LayerTools(doc);
            if (RhinoGet.GetMultipleObjects("Select Object(s) to change parent layer", false, ObjectType.Curve | ObjectType.Annotation, out ObjRef[] obj) != Result.Success)
                return Result.Cancel;

            var li = lt.ObjLayer(obj[0].Object());
            
            if(li.ParentLayerId == Guid.Empty)
            {
                RhinoApp.WriteLine(li.Name.ToString());
                doc.Layers.SetCurrentLayerIndex(li.Index, true);
            } 
            else
            {
                RhinoApp.WriteLine(Layer.GetParentName(li.FullPath).ToString());
                var name = Layer.GetParentName(li.FullPath);
                doc.Layers.SetCurrentLayerIndex(doc.Layers.FindName(name).Index, true);
            }
            
            return Result.Success;
        }
    }
}