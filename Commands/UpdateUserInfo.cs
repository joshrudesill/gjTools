using Rhino;
using Rhino.Commands;

namespace gjTools.Commands
{
    public class UpdateUserInfo : Command
    {
        public UpdateUserInfo()
        {
            Instance = this;
        }

        public static UpdateUserInfo Instance { get; private set; }

        public override string EnglishName => "UpdateUserInfo";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var helper = new DialogTools(doc);
            helper.updateUserInfo();
            return Result.Success;
        }
    }
}