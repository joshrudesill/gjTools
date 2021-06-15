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
            var h = new DialogTools(doc);
            h.updateUserInfo();
            return Result.Success;
        }
    }
}