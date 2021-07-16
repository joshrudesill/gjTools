using System;
using System.Collections.Generic;
using Rhino.DocObjects;
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

            if (res.Contains("RH") || res.Contains("LH"))
                RHLHSwap(res);

            return Result.Success;
        }



        public void RHLHSwap(List<string> blurbs)
        {

        }
    }
}