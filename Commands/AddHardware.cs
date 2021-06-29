using System;
using Rhino;
using Rhino.Commands;
using System.Collections.Generic;
namespace gjTools.Commands
{
    public class AddHardware : Command
    {
        DialogTools d;
        public AddHardware()
        {
            Instance = this;
        }
        public static AddHardware Instance { get; private set; }

        public override string EnglishName => "AddHardware";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            d = new DialogTools(doc);
            var hardwares = new List<string> { "Cleats", "American Girl Cleats" };
            var type = Rhino.UI.Dialogs.ShowListBox("Add Hardware", "Choose a type of hardware to add..", hardwares);

            switch(hardwares.IndexOf((string)type))
            {
                case 0: 
                    addCleats();
                    break;
                case 1: 
                    addAmGirlCleats();
                    break;
            }
            return Result.Success;
        }

        private void addCleats()
        {
            var go = d.selectObject("Select an object to add cleats to");
            var rect = go.Object(0).Curve();
            var bb = rect.GetBoundingBox(true);
            var corners = bb.GetCorners();
            var edges = bb.GetEdges();
        }
        private void addAmGirlCleats()
        {

        }
    }
}