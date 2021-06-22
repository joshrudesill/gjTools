using System;
using Rhino;
using Rhino.Commands;
using System.Collections.Generic;
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
            DialogTools d = new DialogTools(doc);
            LayerTools lt = new LayerTools(doc);
            var go = d.selectObjects("Select object(s) to setup PN layer");
            if (go == null)
            {
                RhinoApp.WriteLine("No objects selected. Command canceled");
                return Result.Cancel;
            }
            List<RhObjLayer> rl = new List<RhObjLayer>();
            for (int i = 0; i < go.ObjectCount; i++)
            {
                if (go.Object(i).Object().ObjectType == Rhino.DocObjects.ObjectType.Annotation)
                {
                    var t = go.Object(i).TextEntity().PlainText;
                    var tf = t.Remove(0, 4);
                    var parent  = lt.CreateLayer(tf);
                }
                else
                {
                    var o = go.Object(i).Object();
                    var l = lt.ObjLayer(go.Object(i).Object());
                    rl.Add(new RhObjLayer(o, l));
                }
            }
            return Result.Success;
        }
    }
}