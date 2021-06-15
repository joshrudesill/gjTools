using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;

namespace gjTools
{
    public class NestingBox : Command
    {
        public NestingBox()
        {
            Instance = this;
        }

        public static NestingBox Instance { get; private set; }

        public override string EnglishName => "NestingBox";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            
            return Result.Success;
        }
    }
}
