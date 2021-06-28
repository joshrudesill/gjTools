using System;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;

namespace gjTools.Testing
{
    public class gregTest : Command
    {
        public gregTest()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static gregTest Instance { get; private set; }

        public override string EnglishName => "gjGregTest";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            
            return Result.Success;
        }
    }
}