using System;
using Rhino;
using Rhino.Commands;

namespace gjTools.Commands
{
    public class BatchKerf : Command
    {
        public BatchKerf()
        {
            Instance = this;
        }

        public static BatchKerf Instance { get; private set; }

        public override string EnglishName => "BatchKerf";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            
            return Result.Success;
        }
    }
}