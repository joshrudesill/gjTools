using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.UI;
using Eto;

namespace gjTools.Commands
{
    public class CustomUI : Command
    {
        public CustomUI()
        {
            Instance = this;
        }

        public Eto.Drawing.Point PDFwindowPosition = new Eto.Drawing.Point(300,300);

        ///<summary>The only instance of the MyCommand command.</summary>
        public static CustomUI Instance { get; private set; }

        public override string EnglishName => "gregDialog";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var outTypes = new List<string>
            {
                "LocalTemp",        // 0
                "MeasuredDrawing",  // 1
                "Mylar Color",      // 2
                "Mylar NonColor",   // 3
                "MultiPagePDF",     // 4
                "EPExportLegacy",   // 5
                "EPExport",         // 6
                "ProtoNestings",    // 7
                "WorkingLocation"   // 8
            };
            var lt = new LayerTools(doc);

            var form = new Helpers.DualListDialog("PDF Example", "Out Type", outTypes, "Layer/s to output", lt.getAllParentLayersStrings(), PDFwindowPosition);
            PDFwindowPosition = form.windowPosition;
            RhinoApp.WriteLine($"The Result is: {form.CommandResult()}");
            RhinoApp.WriteLine($"The left selected: {form.GetSingleValue()}");
            RhinoApp.WriteLine($"The right selections: {string.Join(", ", form.GetMultiSelectValue())}");

            return Result.Success;
        }
    }
}