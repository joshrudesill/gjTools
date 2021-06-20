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
            // Once locations can be gotten from the DB, change this
            var paths = new List<string> { "C:\\Temp\\" };
            var locations = new List<string> { "Temp" };
            if (doc.Name != "")
            {
                locations.Insert(0, "WorkingLocation");
                paths.Insert(0, doc.Path);
            }

            RhinoApp.RunScript("_-Export \"C:\\Temp\\Test.dxf\" Scheme \"Vomela\" _Enter", true);

            return Result.Success;
        }
    }
}