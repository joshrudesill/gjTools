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

            var rl = new List<RhObjLayer>();
            var ro = new List<RhinoObject>();


            Layer parent = new Layer();
            bool pnfound = false;
            for (int i = 0; i < go.Length; i++)
            {
                if (go[i].Object().ObjectType == ObjectType.Annotation && !pnfound)
                {
                    var t = go[i].TextEntity().PlainText;
                    if (t.Substring(0, 3) == "PN:")
                    {
                        pnfound = true;
                    }
                    var tf = t.Remove(0, 3);
                    parent  = lt.CreateLayer(tf);
                }
                else if(lt.isObjectOnCutLayer(go[i].Object()))
                {
                    var o = go[i].Object();
                    var l = lt.ObjLayer(go[i].Object());
                    rl.Add(new RhObjLayer(o, l));
                }
                else
                {
                    ro.Add(go[i].Object());
                }
            }
            foreach (var o in rl)
            {
                lt.CreateLayer(o.l.Name, parent.Name);
            }
            foreach (var o in ro)
            {
                lt.AddObjectsToLayer(o, parent);
                RhinoApp.WriteLine("adding to parent layer");
            }
            return Result.Success;
        }
    }
}