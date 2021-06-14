using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;

namespace gjTools
{
    public class gjToolsCommand : Command
    {
        public gjToolsCommand()
        {
            Instance = this;
        }

        public static gjToolsCommand Instance { get; private set; }

        public override string EnglishName => "gjToolsCommand";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            
            return Result.Success;
        }
    }
}
