using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.DocObjects;
using Rhino.Input;
using Rhino.UI;

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
            // Text Heigh
            double unitHeight = 1.25;
            if (RhinoGet.GetNumber("Text Height", false, ref unitHeight) != Result.Success)
                return Result.Cancel;
            
            // ffffind the fffffont
            var unitFont = Dialogs.ShowListBox("Fonts", "Select a Font", Font.AvailableFontFaceNames());
            if (unitFont == null)
                return Result.Cancel;

            // get the list of parts
            if (!Dialogs.ShowEditBox("List Import", "Paste Here", "Each Line will be a new part...", true, out string rawInput))
                return Result.Cancel;

            var unitNumbers = new List<string> (rawInput.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries));
            var dt = new DrawTools(doc);
            var txt = dt.AddText("396NONE", Point3d.Origin, dt.StandardDimstyle(), unitHeight);
                txt.Font = Font.FromQuartetProperties((string)unitFont, false, false);

            double height = 0.0;
            double width = 0.0;
            foreach (var u in unitNumbers)
            {
                txt.PlainText = u;
                if (txt.GetBoundingBox(true).GetEdges()[0].Length > width)
                    width = txt.GetBoundingBox(true).GetEdges()[0].Length;
                if (txt.GetBoundingBox(true).GetEdges()[1].Length > height)
                    height = txt.GetBoundingBox(true).GetEdges()[1].Length;
            }

            var box = new Rectangle3d(Plane.WorldXY, width + 0.5, height + 0.5);
            var pt = txt.GetBoundingBox(true).GetCorners()[0];
            txt.Transform(Transform.Translation(0.25 - pt.X, 0.25 - pt.Y, 0));

            var moveTxt = Transform.Translation(0, box.Height, 0);
            // create the text and boxes
            foreach (var u in unitNumbers)
            {
                txt.PlainText = u;
                txt.Transform(moveTxt);
                box.Transform(moveTxt);
                doc.Objects.AddText(txt);
                doc.Objects.AddRectangle(box);
            }

            doc.Views.Redraw();

            return Result.Success;
        }
    }
}