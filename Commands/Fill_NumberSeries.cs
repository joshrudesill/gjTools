using System;
using Rhino;
using Rhino.Commands;

namespace gjTools.Commands
{
    public class Fill_NumberSeries : Command
    {
        public Fill_NumberSeries()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static Fill_NumberSeries Instance { get; private set; }

        public override string EnglishName => "FillNumberSeries";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {


            return Result.Success;
        }
    }
}