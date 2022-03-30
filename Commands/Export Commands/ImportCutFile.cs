using System;
using Rhino;
using Rhino.Commands;
using Rhino.UI;
using Rhino.Input.Custom;

namespace gjTools.Commands
{
    public class ImportCutFile : Command
    {
        public ImportCutFile()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static ImportCutFile Instance { get; private set; }

        public override string EnglishName => "ImportCutFile";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var fileDialog = new OpenFileDialog
            {
                Filter = "Cut Files - (*.dxf;*.3dm)",
                Title = "Import Cut File",
                InitialDirectory = "\\\\VWS\\Cut"
            };

            if (!fileDialog.ShowOpenDialog())
                return Result.Cancel;
            
            doc.Import(fileDialog.FileName);

            doc.Views.Redraw();
            return Result.Success;
        }
    }




}