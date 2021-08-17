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
            LayerTools lt = new LayerTools(doc);
            
            if (RhinoGet.GetMultipleObjects("Select object(s) to setup PN layer", false, ObjectType.AnyObject, out ObjRef[] go) != Result.Success)
                return Result.Cancel;

            var partNumber = FindPNText(go);
            if (partNumber == null)
                return Result.Cancel;

            var pLay = lt.CreateLayer(partNumber);

            foreach(var o in go)
            {
                var rObj = o.Object();
                    rObj.Attributes.LayerIndex = pLay.Index;
                    rObj.CommitChanges();
            }

            return Result.Success;
        }

        public string FindPNText(ObjRef[] obj)
        {
            foreach(var o in obj)
            {
                if (o.TextEntity() != null)
                {
                    var txt = o.TextEntity();
                    if (txt.PlainText.Substring(0, 4) == "PN: ")
                        return txt.PlainText.Substring(3).Trim();
                }
                else
                    continue;
            }

            return null;
        }
    }
}