using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;

namespace gjTools.Commands
{
    public class AssignCutType : Command
    {
        public AssignCutType()
        {
            Instance = this;
        }

        public static AssignCutType Instance { get; private set; }

        public override string EnglishName => "AssignCutType";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            DialogTools d = new DialogTools(doc);
            Rhino.Input.Custom.GetObject go = new Rhino.Input.Custom.GetObject();
            go.SetCommandPrompt("Select object(s) to assign cut type");
            Rhino.Input.GetResult gr = go.GetMultiple(0, -1);
            if (gr != Rhino.Input.GetResult.Object)
            {
                RhinoApp.WriteLine("No objects selected. Command canceled");
                return Result.Cancel;
            }
            List<Rhino.DocObjects.RhinoObject> ids = new List<Rhino.DocObjects.RhinoObject>();
            for (int i = 0; i < go.ObjectCount; i++)
                ids.Add(go.Object(i).Object());
            
            var lt = new List<string> { 
                "EYES",
                "VGROOVE", 
                "THRU", 
                "BOARD", 
                "TEXT", 
                "CREASE", 
                "KISS",
                "SCORE",
                "OTHER" };
            var lc = new List<System.Drawing.Color> { 
                System.Drawing.Color.Black,
                System.Drawing.Color.FromArgb(255,140,14,14), 
                System.Drawing.Color.Red,
                System.Drawing.Color.FromArgb(255,0,20,255), 
                System.Drawing.Color.Black,
                System.Drawing.Color.FromArgb(255,170,95,15),
                System.Drawing.Color.FromArgb(255,200,0,200),
                System.Drawing.Color.FromArgb(255,120,30,155),
                System.Drawing.Color.Black
            };

            object lo = Rhino.UI.Dialogs.ShowListBox("Cut Type", "Choose a cut type", lt, lt[2]);
            string ls = "C_" + lo.ToString();
            int li = ids[0].Attributes.LayerIndex;

            var ilayer = doc.Layers[li];
            if (ilayer.ParentLayerId != Guid.Empty)
            {
                var player = doc.Layers.FindId(ilayer.ParentLayerId);
                li = player.Index;
            }
            int sli = d.addLayer(ls, lc[lt.IndexOf(lo.ToString())], li);
            foreach(Rhino.DocObjects.RhinoObject r in ids)
            {
                r.Attributes.LayerIndex = sli;
                r.CommitChanges();
            }
            return Result.Success;
        }
    }
}