using System.IO;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.UI;
using Rhino.DocObjects;

namespace gjTools.Commands
{
    public class MD_Import : Command
    {
        public MD_Import()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static MD_Import Instance { get; private set; }

        public override string EnglishName => "MDImport";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            if (doc.Path == null)
            {
                RhinoApp.WriteLine("Save your File in the CAD folder of the job first");
                return Result.Cancel;
            }

            var fileList = GetFileList(doc.Path.Replace(doc.Name, ""));

            var res = Dialogs.ShowMultiListBox("Files", "Choose Files to Import", fileList);
            if (res == null)
                return Result.Cancel;

            

            return Result.Success;
        }


        public void ImportFile(RhinoDoc doc, string basePath, List<string> fileNames)
        {
            var lt = new LayerTools(doc);

            // lock current layers
            var tmpLay = lt.CreateLayer("Tmp");
            doc.Layers.SetCurrentLayerIndex(tmpLay.Index, true);
            foreach (var l in lt.getAllParentLayers())
                if (l.Name == "Tmp")
                    l.IsLocked = true;

            foreach (var fn in fileNames)
            {
                if (doc.Import(basePath + fn))
                {
                    var parentLayer = lt.CreateLayer(fn);
                    foreach(var l in lt.getAllParentLayers())
                    {
                        if (!l.IsLocked)
                            l.ParentLayerId = parentLayer.Id;
                    }
                    parentLayer.IsLocked = true;
                }
                else
                {
                    // File didnt open
                    RhinoApp.WriteLine($"{fn} didnt contain geometry");
                }
            }
        }

        public List<string> GetFileList(string basePath)
        {
            var fileList = Directory.EnumerateFiles($"{basePath}FullSize_CutLinesOnly\\");
            var OutList = new List<string>(fileList);

            return OutList;
        }
    }
}