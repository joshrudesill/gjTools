using System;
using Rhino;
using Rhino.Commands;

namespace gjTools
{
    public class JTest : Command
    {
        public JTest()
        {
            Instance = this;
        }

        public static JTest Instance { get; private set; }
        public override string EnglishName => "JTest";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            SQLHelper helper = new SQLHelper();
            helper.queryLocations();
            return Result.Success;
        }
    }
}