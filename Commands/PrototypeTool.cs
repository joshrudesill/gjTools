using System;
using Rhino;
using Rhino.Commands;

namespace gjTools.Commands
{
    public class PrototypeTool : Command
    {
        public PrototypeTool()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static PrototypeTool Instance { get; private set; }

        public override string EnglishName => "gjProtoUtility";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var sql = new SQLTools();

            return Result.Success;
        }
    }
}