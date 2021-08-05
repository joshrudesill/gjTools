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
    public class Unit_Number_List : Command
    {
        public Unit_Number_List()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static Unit_Number_List Instance { get; private set; }

        public override string EnglishName => "UnitNumberList";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // Get the sized unit number
            if (RhinoGet.GetOneObject("Select the Desired Text", false, ObjectType.Annotation, out ObjRef example) != Result.Success)
                return Result.Cancel;

            var txt = example.TextEntity();
            if (txt == null)
            {
                RhinoApp.WriteLine("That wasnt a text entity, Try again");
                return Result.Cancel;
            }

            // get the list of parts
            if (!Dialogs.ShowEditBox("List Import", "Paste Here", "Each Line will be a new part...", true, out string rawInput))
                return Result.Cancel;

            var unitNumbers = new List<string> (rawInput.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries));

            double height = 0.0;
            double width = 0.0;
            foreach (var u in unitNumbers)
            {
                txt.PlainText = u;

                // explode for better size results
                var explTxt = txt.Explode();
                var crvBB = BoundingBox.Empty;
                foreach(var c in explTxt)
                    crvBB.Union(c.GetBoundingBox(true));

                // Test Dims
                var edges = crvBB.GetEdges();
                if (edges[0].Length > width)
                    width = edges[0].Length;
                if (edges[1].Length > height)
                    height = edges[1].Length;
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