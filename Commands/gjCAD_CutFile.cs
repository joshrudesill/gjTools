using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;

namespace gjTools.Commands
{
    [CommandStyle(Style.ScriptRunner)]
    public class gjCAD_CutFile : Command
    {
        public gjCAD_CutFile()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static gjCAD_CutFile Instance { get; private set; }

        public override string EnglishName => "gjCAD_CutFile";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var dt = new DrawTools(doc);
            var lt = new LayerTools(doc);
            var obj = new List<Rhino.DocObjects.RhinoObject>();

            // Once locations can be gotten from the DB, change this
            var paths = new List<string> { "C:\\Temp\\" };
            var locationNames = new List<string> { "Temp" };

            // File is a 3DM File, path data is available to add working Location
            if (doc.Name != "")
            {
                locationNames.Insert(0, "WorkingLocation");
                paths.Insert(0, doc.Path.Replace(doc.Name, ""));
            }

            var exportLayer = dt.SelParentLayers(false);
            if (exportLayer[0] == null)
                return Result.Cancel;
            var cutLayers = lt.getAllCutLayers(doc.Layers.FindName(exportLayer[0]));

            doc.Objects.UnselectAll(true);
            foreach(var cl in cutLayers)
            {
                var selSett = new Rhino.DocObjects.ObjectEnumeratorSettings();
                    selSett.LayerIndexFilter = cl.Index;
                    selSett.ObjectTypeFilter = Rhino.DocObjects.ObjectType.Curve | Rhino.DocObjects.ObjectType.Annotation;
                    selSett.NormalObjects = true;
                foreach (var o in doc.Objects.GetObjectList(selSett))
                    doc.Objects.Select(o.Id);
            }
            
            RhinoApp.RunScript("_-Export \"C:\\Temp\\Test.dxf\" Scheme \"Vomela\" _Enter", false);

            return Result.Success;
        }
    }
}