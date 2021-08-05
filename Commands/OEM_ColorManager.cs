using System;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Geometry;

namespace gjTools.Commands
{
    public class OEM_ColorManager : Command
    {
        public OEM_ColorManager()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static OEM_ColorManager Instance { get; private set; }

        public override string EnglishName => "OEMColorManager";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var colorDialog = new OEMColorManager();
            colorDialog.ShowForm();

            // See if user wants to place the text for whatever reason
            if (colorDialog.CommandResult == Eto.Forms.DialogResult.Yes)
            {
                var chosen = colorDialog.SelectedColor;
                var dt = new DrawTools(doc);

                if (RhinoGet.GetPoint("Place Color", false, out Point3d pt) != Result.Success)
                    return Result.Cancel;

                var txt = dt.AddText(
                    $"{chosen.colorNum} - {chosen.colorName}",
                    pt, dt.StandardDimstyle()
                );

                doc.Objects.AddText(txt);
            }

            return Result.Success;
        }
    }
}