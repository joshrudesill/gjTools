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

            var rl = new List<RhObjLayer>();
            var ro = new List<Rhino.DocObjects.RhinoObject>();

            var parent = new Rhino.DocObjects.Layer();

            bool pnfound = false;
            for (int i = 0; i < go.ObjectCount; i++)
            {
                if (go.Object(i).Object().ObjectType == Rhino.DocObjects.ObjectType.Annotation && !pnfound)
                {
                    var t = go.Object(i).TextEntity().PlainText;
                    if (t.Substring(0, 3) == "PN:")
                    {
                        pnfound = true;
                    }
                    var tf = t.Remove(0, 4);
                    parent  = lt.CreateLayer(tf);
                }
                else if(lt.isObjectOnCutLayer(go.Object(i).Object()))
                {
                    var o = go.Object(i).Object();
                    var l = lt.ObjLayer(go.Object(i).Object());
                    rl.Add(new RhObjLayer(o, l));
                }
                else
                {
                    ro.Add(go.Object(i).Object());
                }
            }
            foreach (var o in rl)
            {
                lt.CreateLayer(o.l.Name, parent.Name);
            }
            foreach (var o in ro)
            {
                lt.AddObjectsToLayer(o, parent);
            }
            return Result.Success;
        }
    }
}