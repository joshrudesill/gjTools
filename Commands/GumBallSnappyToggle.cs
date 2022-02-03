using Rhino;
using Rhino.Commands;

namespace gjTools.Commands
{
    public class GumBallSnappyToggle : Command
    {
        public GumBallSnappyToggle()
        {
            Instance = this;
        }

        public static GumBallSnappyToggle Instance { get; private set; }

        public override string EnglishName => "GumBallSnappyToggle";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            if (Rhino.ApplicationSettings.ModelAidSettings.SnappyGumballEnabled)
            {
                Rhino.ApplicationSettings.ModelAidSettings.SnappyGumballEnabled = false;
                RhinoApp.WriteLine("GumBall SnappyDrag is Disabled.");
            }
            else
            {
                Rhino.ApplicationSettings.ModelAidSettings.SnappyGumballEnabled = true;
                RhinoApp.WriteLine("GumBall SnappyDrag is Enabled.");
            }

            return Result.Success;
        }
    }
}