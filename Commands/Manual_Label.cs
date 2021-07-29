using System;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Geometry;

namespace gjTools.Commands
{
    public class Manual_Label : Command
    {
        public Manual_Label()
        {
            Instance = this;
        }

        public string partNumber = "DUT-21-78365A";

        ///<summary>The only instance of the MyCommand command.</summary>
        public static Manual_Label Instance { get; private set; }

        public override string EnglishName => "LiebingerManual";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var mouse = Eto.Forms.Mouse.Position;
            mouse.X -= 150;
            mouse.Y -= 100;

            var form = new LiebingerDialog()
            {
                defaultPartNumber = partNumber,
                windowPosition = new Eto.Drawing.Point(mouse)
            };
            form.ShowForm();
            partNumber = form.GetCurrentPartNumber();

            if (form.CommandResult() != Eto.Forms.DialogResult.Ok)
                return Result.Cancel;

            if (RhinoGet.GetPoint("Place Label", false, out Point3d pt) != Result.Success)
                return Result.Cancel;

            var pTool = new PrototypeTool();
            var lt = new LayerTools(doc);
            var lay = lt.CreateLayer("C_TEXT", doc.Layers.CurrentLayer.Name);

            pTool.PlaceProductionLabel(doc, form.LabelInfo, lay, pt);

            return Result.Success;
        }
    }
}