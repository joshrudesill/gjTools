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
        public override string EnglishName => "asdf";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var h = new HelperFunctions(doc);
            h.showColorPallete();
            return Result.Success;
        }
    }
}