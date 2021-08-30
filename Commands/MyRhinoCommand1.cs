using System;
using Rhino;
using Rhino.Commands;

namespace gjTools.Commands
{
    public class MyRhinoCommand1 : Command
    {
        public MyRhinoCommand1()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static MyRhinoCommand1 Instance { get; private set; }

        public override string EnglishName => "MyRhinoCommand1";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // TODO: complete command.
            return Result.Success;
        }
    }
}