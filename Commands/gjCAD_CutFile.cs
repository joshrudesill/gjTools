using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.Input.Custom;

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
            var sql = new SQLTools();

            var selectedObjects = new List<Rhino.DocObjects.ObjRef>();

            // get locations from DB
            var paths = new List<string>();
            var locationNames = new List<string>();
            foreach(var line in sql.queryLocations())
            {
                paths.Add(line.path);
                locationNames.Add(line.locName);
            }

            // File is a 3DM File, path data is available to add working Location
            if (doc.Name != "")
            {
                locationNames.Insert(0, "WorkingLocation");
                paths.Insert(0, doc.Path.Replace(doc.Name, ""));
            }

            // user chooses a cut location
            var chosenLocation = Rhino.UI.Dialogs.ShowListBox("Cut Location", "Choose a Location", locationNames) as string;
            if (chosenLocation == null)
                return Result.Cancel;

            // ask for the layer to send cut file out from
            var layerList = lt.getAllParentLayersStrings();
            var exportLayer = Rhino.UI.Dialogs.ShowListBox("Layers", "Choose a Layer", layerList) as string;
            if (exportLayer == null)
                return Result.Cancel;

            // decide what kind of cut file to make
            doc.Objects.UnselectAll(true);

            chosenLocation = paths[locationNames.IndexOf(chosenLocation)] + exportLayer[0] + ".dxf";
            var cutLayers = lt.getAllCutLayers(doc.Layers.FindName(exportLayer));

            // test for multiple nest boxes
            if (NestBoxCounter(doc, lt.CreateLayer(exportLayer)))
            {
                var go = new GetObject();
                    go.SetCommandPrompt("Multiple Nestings on one Layer Detected, Choose Objects for Cut File");
                    go.GeometryFilter = Rhino.DocObjects.ObjectType.Curve | Rhino.DocObjects.ObjectType.Annotation;
                    go.GetMultiple(1, 0);

                if (go.CommandResult() != Result.Success)
                    return Result.Cancel;

                doc.Objects.UnselectAll();
                foreach (var o in go.Objects())
                {
                    if (lt.ObjLayer(o.ObjectId).Name.Substring(0, 2) == "C_" || lt.ObjLayer(o.ObjectId).Name == "NestBox")
                    {
                        selectedObjects.Add(o);
                        doc.Objects.Select(o.ObjectId);
                    }
                }
            }
            else
            {
                // Select only the objects needed
                foreach (var cl in cutLayers)
                {
                    var selSett = new Rhino.DocObjects.ObjectEnumeratorSettings();
                    selSett.LayerIndexFilter = cl.Index;
                    selSett.ObjectTypeFilter = Rhino.DocObjects.ObjectType.Curve | Rhino.DocObjects.ObjectType.Annotation;
                    selSett.NormalObjects = true;

                    foreach (var o in doc.Objects.GetObjectList(selSett))
                    {
                        doc.Objects.Select(o.Id);
                        selectedObjects.Add(new Rhino.DocObjects.ObjRef(doc, o.Id));
                    }
                }
            }

            // Check that objects are all polylines
            if (dt.CheckPolylines(selectedObjects, true))
            {
                dt.hideDynamicDraw();
                MakeDXF(chosenLocation);
            } else
            {
                var msg = new GetString();
                    msg.SetCommandPrompt("Some Bad Lines are Present, Would you like to Continue?");
                    msg.AddOption("Yes");
                    msg.AddOption("No");
                    msg.SetDefaultString("No");
                    msg.Get();

                dt.hideDynamicDraw();
                if (msg.CommandResult() != Result.Success)
                    return Result.Cancel;

                if (msg.StringResult().Trim() == "Yes")
                    MakeDXF(chosenLocation);
                else
                    RhinoApp.WriteLine("Cancelled, No Cut File sent out...");
            }
            
            return Result.Success;
        }

        /// <summary>
        /// Send out the DXF file
        /// </summary>
        /// <param name="fullPath"></param>
        public void MakeDXF(string fullPath)
        {
            RhinoApp.RunScript("_-Export \"" + fullPath + "\" Scheme \"Vomela\" _Enter", false);
        }

        /// <summary>
        /// Returns true if there is more than one entity on the NestBox Layer
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="parentLayer"></param>
        /// <returns></returns>
        public bool NestBoxCounter(RhinoDoc doc, Rhino.DocObjects.Layer parentLayer)
        {
            int nestLayerIndex = doc.Layers.FindByFullPath(parentLayer.Name + "NestBox", -1);
            if (nestLayerIndex == -1)
            {
                return false;
            } else
            {
                var selSet = new Rhino.DocObjects.ObjectEnumeratorSettings();
                    selSet.LayerIndexFilter = nestLayerIndex;
                    selSet.ObjectTypeFilter = Rhino.DocObjects.ObjectType.Curve;

                var nestCount = doc.Objects.GetObjectList(selSet) as List<Rhino.DocObjects.RhinoObject>;
                if (nestCount.Count > 1)
                    return true;
            }
            return false;
        }

        public void MakeCutNameText(SQLTools sql, string cutType)
        {
            switch (cutType)
            {
                case "Default":
                    // Default Cut
                    var varData = sql.queryVariableData()[0];
                    int nextNumber = varData.cutNumber;

                    varData.cutNumber++;
                    break;
            }

        }
    }
}