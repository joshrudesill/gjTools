using System;
using Rhino;
using Rhino.Commands;

namespace gjTools.Commands
{
    public class ZundEyes : Command
    {
        public ZundEyes()
        {
            Instance = this;
        }

        public static ZundEyes Instance { get; private set; }

        public override string EnglishName => "ZundEyes";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {

            return Result.Success;
        }
    }
}