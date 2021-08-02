using Rhino;
using Rhino.Commands;
using System.Collections.Generic;
using Rhino.DocObjects;
using Rhino.Input;

namespace gjTools.Commands
{
    public class SetupPNLayer : Command
    {
        public SetupPNLayer()
        {
            Instance = this;
        }

        public static SetupPNLayer Instance { get; private set; }

        public override string EnglishName => "SetupPNLayer";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            if (RhinoGet.GetMultipleObjects("Select object(s) to setup PN layer", false, ObjectType.AnyObject, out ObjRef[] go) != Result.Success)
                return Result.Cancel;
            LayerTools lt = new LayerTools(doc);
            Layer parent = new Layer();
            bool pnfound = false;
            if(go.Length == 0) { return Result.Failure; }
            foreach (var g in go)
            {
                if (g.Object().ObjectType == ObjectType.Annotation && !pnfound)
                {
                    parent = lt.CreateLayer(g.TextEntity().PlainText.Replace("PN:", ""));
                    var tg = g.Object();
                    tg.Attributes.LayerIndex = parent.Index;
                    tg.CommitChanges();
                    pnfound = true;
                    break;
                }
            }
            if(!pnfound) { return Result.Nothing; }
            foreach (var g in go)
            {
                if (g.Object().ObjectType == ObjectType.Annotation) { continue; }
                var tlayer = doc.Layers[g.Object().Attributes.LayerIndex];
                var tclayer = lt.CreateLayer(tlayer.Name, parent.Name, tlayer.Color);
                var tg = g.Object();
                tg.Attributes.LayerIndex = tclayer.Index;

                tg.CommitChanges();
                
            }
            return Result.Success;
        }
    }
}