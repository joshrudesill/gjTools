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

        public struct TxtEnt
        {
            public List<Curve> Crvs;
            public BoundingBox BB;
        }

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

            // Parse the unit numbers
            var unitNumbers = new List<string> (rawInput.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries));

            // List of New entities
            var txtEntList = new List<TxtEnt>();
            double width = 0;
            double height = 0;

            // Gather Unit number info
            foreach (var u in unitNumbers)
            {
                var te = TextEntity.CreateWithRichText(
                    txt.RichText.Replace($" {txt.PlainText}", $" {u}"), 
                    Plane.WorldXY, txt.DimensionStyle, false, txt.FormatWidth, 0
                );
                te.Justification = TextJustification.Center;
                te.TextHorizontalAlignment = TextHorizontalAlignment.Center;
                te.TextVerticalAlignment = TextVerticalAlignment.Middle;
                te.TextHeight = txt.TextHeight;

                var crv = new List<Curve>(te.Explode());
                var BB = BoundingBox.Empty;
                foreach (var c in crv)
                    BB.Union(c.GetBoundingBox(true));

                // Make new entry
                txtEntList.Add( new TxtEnt { Crvs = crv, BB = BB } );

                // update the width
                width = (BB.GetEdges()[0].Length > width) ? BB.GetEdges()[0].Length : width;
                height = (BB.GetEdges()[1].Length > height) ? BB.GetEdges()[1].Length : height;
            }

            // Resize, stack and add to the document
            var attr = example.Object().Attributes;
            var plane = Plane.WorldXY;
            for (int i = 0; i < txtEntList.Count; i++)
            {
                var te = txtEntList[i];
                var Rec = new Rectangle3d(plane, width + 0.5, height + 0.5);

                var crvs = te.Crvs;
                for (int c = 0; c < crvs.Count; c++)
                {
                    var crv = crvs[c];
                    crv.Translate(Rec.Center - te.BB.Center);
                    doc.Objects.AddCurve(crv, attr);
                }

                doc.Objects.AddRectangle(Rec, attr);

                plane.OriginY += height + 0.5;
            }

            doc.Views.Redraw();
            return Result.Success;
        }
    }
}