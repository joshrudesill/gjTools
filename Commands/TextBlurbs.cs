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

        public override string EnglishName => "TextBlurbs";

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


        /// <summary>
        /// Used to present the blurbs to be modified
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="sql"></param>
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

        /// <summary>
        /// Checks the path for the company and determines their version of RH/LH
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="blurbs"></param>
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
                if (path.Contains("PACIFIC COACHWORKS") || path.Contains("THOR MOTORCOACH"))
                    designation = (designation == "RH") ? "CS/RH" : "RS/LH";
                else if (path.Contains("NORTHWOOD MFG") || path.Contains("ECLIPSE RV"))
                    designation = (designation == "RH") ? "PS/RH" : "DS/LH";
                else if (path.Contains("JAYCO"))
                    designation = (designation == "RH") ? "DS/RH" : "ODS/LH";
                else if (path.Contains("OUTDOORS RV"))
                    designation = (designation == "RH") ? "DS/RH" : "RS/LH";
            }

            // add the text
            AddBlurb(doc, designation);
        }

        /// <summary>
        /// Makes the Text entity and adds to the document
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="blurb"></param>
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