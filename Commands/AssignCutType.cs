using System.Drawing;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.DocObjects;

namespace gjTools.Commands
{
    struct RhObjLayer
    {
        public RhinoObject r;
        public Layer l;
        public RhObjLayer(RhinoObject r, Layer l)
        {
            this.r = r;
            this.l = l;
        }
    }
    // This command has been tested and is error proof. Ready for release.
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
            var lt = new LayerTools(doc);
            if (RhinoGet.GetMultipleObjects("Select Objects To Assign a Cut Type", false, ObjectType.Curve | ObjectType.Annotation, out ObjRef[] obj) != Result.Success)
                return Result.Cancel;
            
            var cutName = new List<string> {
                "EYES",
                "VGROOVE", 
                "THRU", 
                "BOARD", 
                "TEXT", 
                "CREASE", 
                "KISS",
                "SCORE",
                "OTHER" };

            var cutColor = new List<Color> { 
                Color.Black,
                Color.FromArgb(255,140,14,14), 
                Color.Red,
                Color.FromArgb(255,0,20,255), 
                Color.Black,
                Color.FromArgb(255,170,95,15),
                Color.FromArgb(255,200,0,200),
                Color.FromArgb(255,120,30,155),
                Color.Black
            };

            var cutType = (string)Rhino.UI.Dialogs.ShowListBox("Cut Type", "Choose a cut type", cutName, cutName[2]);
            if (cutType is null)
                return Result.Cancel;
            
            foreach (var o in obj)
            {
                var subObj = o.Object();
                var objLayer = doc.Layers[subObj.Attributes.LayerIndex];
                if (objLayer.ParentLayerId != System.Guid.Empty)
                    objLayer = doc.Layers.FindId(objLayer.ParentLayerId);

                Layer cutLayer = lt.CreateLayer("C_" + cutType, objLayer.Name, cutColor[cutName.IndexOf(cutType)]);

                subObj.Attributes.ColorSource = ObjectColorSource.ColorFromLayer;
                subObj.Attributes.PlotColorSource = ObjectPlotColorSource.PlotColorFromLayer;
                subObj.Attributes.LayerIndex = cutLayer.Index;
                subObj.CommitChanges();
            }
            
            return Result.Success;
        }
    }
}