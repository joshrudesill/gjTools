using System;
using System.IO;
using System.Collections.Generic;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.UI;
using Rhino;
using Rhino.Commands;

namespace gjTools.Commands
{
    public class TextBlurbs : Command
    {
        public TextBlurbs()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static TextBlurbs Instance { get; private set; }

        public override string EnglishName => "gjTextBlurbs";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var sql = new SQLTools();
            var blurbs = sql.queryCustomBlurbs();
            var blurbString = new List<string>();

            foreach (var b in blurbs)
                blurbString.Add(b.blurb);
            blurbString.AddRange(new List<string> { "RH", "LH", "Manage Blurbs" });

            var res = new List<string>( Dialogs.ShowMultiListBox("Text Blurbs", "Select Blurbs", blurbString) );
            if (res == null)
                return Result.Cancel;

            // Do Work
            if (res.Contains("RH") || res.Contains("LH"))
                RHLHSwap(doc, res);
            else if (res.Contains("Manage Blurbs"))
                ManageBlurbs(doc, sql);
            else
                AddBlurb(doc, string.Join(", ", res));

            doc.Views.Redraw();
            return Result.Success;
        }


        public void ManageBlurbs(RhinoDoc doc, SQLTools sql)
        {
            var blurbs = sql.queryCustomBlurbs();
            var ids = new List<int>();
            var strings = new List<string>();

            foreach(var b in blurbs)
            {
                ids.Add(b.id);
                strings.Add(b.blurb);
            }

            // commence changes
            var res = Dialogs.ShowPropertyListBox("Blurb Manager", "Make Changes", ids, strings);
            if (res != null)
            {
                for (int i = 0; i < res.Length; i++)
                    if (blurbs[i].blurb != res[i])
                        sql.updateCustomBlurb(new CustomBlurb(blurbs[i].id, res[i]));

                RhinoApp.WriteLine("Text Blurbs were updated");
            }
        }

        public void RHLHSwap(RhinoDoc doc, List<string> blurbs)
        {
            string designation = "LH";

            if (blurbs.Contains("RH"))
                designation = "RH";

            // if saved file, search the path
            if (doc.Path != null)
            {
                var path = doc.Path.Replace(doc.Name, "");
                
                // do search here and add to the designation
            }


            // add the text
            AddBlurb(doc, designation);
        }

        public void AddBlurb(RhinoDoc doc, string blurb)
        {
            var dt = new DrawTools(doc);
            var ds = dt.StandardDimstyle();

            var res = RhinoGet.GetPoint("Place Text", false, out Point3d pt);
            if (res == Result.Success)
            {
                var txt = dt.AddText(blurb, pt, ds);
                doc.Objects.AddText(txt);
            }
        }
    }
}