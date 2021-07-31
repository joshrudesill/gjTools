using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.UI;
using Rhino.DocObjects;
using Rhino.Input;
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

            var views = doc.Views.GetPageViews();
            var viewStrings = new List<string>();
            foreach (var v in views)
                viewStrings.Add(v.MainViewport.Name);

            /**var form = new DualListDialog("PDF Example", "Out Type", outTypes, "Layer/s to output", lt.getAllParentLayersStrings())
            {
                windowPosition = PDFwindowPosition,
                singleDefaultIndex = 4,
                multiSelectAlternate = viewStrings,
                showTextBox = true,
                txtBoxDefault = "Potato",
                txtBoxLabel = "What's Yummy?"
            };
            form.ShowForm();
            PDFwindowPosition = form.windowPosition;
            RhinoApp.WriteLine($"The Result is: {form.CommandResult()}");
            RhinoApp.WriteLine($"The left selected: {form.GetSingleValue()}");
            RhinoApp.WriteLine($"The right selections: {string.Join(", ", form.GetMultiSelectValue())}");
            **/

            /**var form = new LiebingerDialog()
            {
                defaultPartNumber = "DUT-21-78365A",
                windowPosition = PDFwindowPosition
            };
            form.ShowForm();
            PDFwindowPosition = form.windowPosition;**/

            /**var form = new PrototypeDialog()
            {
                windowPosition = PDFwindowPosition,
                userInfo = new List<string> { "", "", "", "", "", "", "", "", "", "", "", "", "", "", "" }
            };
            form.ShowForm();
            PDFwindowPosition = form.windowPosition;**/

            RhinoGet.GetMultipleObjects("farts", false, ObjectType.Annotation, out ObjRef[] toot);

            return Result.Success;
        }
    }
}