using System;
using Rhino;
using Rhino.Commands;
using Rhino.UI;

namespace gjTools.Commands
{
    public class CustomUI : Command
    {
        public CustomUI()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static CustomUI Instance { get; private set; }

        public override string EnglishName => "gregDialog";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var form = new Helpers.DualListDialog(doc);
            form.Makeform();

            return Result.Success;
        }
    }
}