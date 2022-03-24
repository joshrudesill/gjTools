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
            var blurbString = new List<string>();
            var blurbs = SQL.SQLTool.queryCustomBlurbs();

            foreach (var b in blurbs)
                blurbString.Add(b.blurb);
            blurbString.AddRange(new List<string> { "RH", "LH", "E&P Composite Mylar", "Add File Name", "Manage Blurbs" });

            var res = new List<string>( Dialogs.ShowMultiListBox("Text Blurbs", "Select Blurbs", blurbString) );
            if (res == null)
                return Result.Cancel;

            // Do Work
            if (res.Contains("RH") || res.Contains("LH"))
                RHLHSwap(doc, res);
            else if (res.Contains("Manage Blurbs"))
                ManageBlurbs(doc);
            else if (res.Contains("E&P Composite Mylar") && doc.Name != null)
                EPDecoration(doc, "Composite Mylar", doc.Name.Substring(0, doc.Name.Length - 5) + "_MYLAR");
            else if (res.Contains("Add File Name") && doc.Name != null)
                EPDecoration(doc, "Drawing", doc.Name);
            else
                AddBlurb(doc, string.Join(", ", res));

            doc.Views.Redraw();
            return Result.Success;
        }


        /// <summary>
        /// Make a cut-style box for EP
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="RegularTxt"></param>
        /// <param name="BoldTxt"></param>
        /// <returns></returns>
        public bool EPDecoration(RhinoDoc doc, string RegularTxt, string BoldTxt)
        {
            var dt = new DrawTools(doc);
            if (RhinoGet.GetPoint("Place the Block", false, out Point3d pt) != Result.Success)
                return false;
            
            var txt = dt.AddText($"{RegularTxt}\n{BoldTxt}", pt, dt.StandardDimstyle(), 1, 1, 3, 6);
            txt.RichText = "{\\rtf1\\deff0{\\fonttbl{\\f0 Consolas;}}\\f0{\\f0\\fs20\\b0 " +
                $"{RegularTxt}" +
                "\\par}{\\f0\\fs30\\b " +
                $"{BoldTxt}" +
                "\\b0\\par}}";
            txt.MaskEnabled = false;
            txt.MaskFrame = DimensionStyle.MaskFrame.RectFrame;
            txt.MaskOffset = 0.5;
            txt.DrawTextFrame = true;
            var attr = new ObjectAttributes { LayerIndex = doc.Layers.CurrentLayer.Index };

            doc.Objects.AddText(txt, attr);

            return true;
        }

        /// <summary>
        /// Used to present the blurbs to be modified
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="sql"></param>
        public void ManageBlurbs(RhinoDoc doc)
        {
            var blurbs = SQL.SQLTool.queryCustomBlurbs();
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
                        SQL.SQLTool.updateCustomBlurb(new SQL.CustomBlurb(blurbs[i].id, res[i]));

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