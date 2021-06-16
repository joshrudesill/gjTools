using System;
using Rhino;
using Rhino.Commands;

namespace gjTools.Testing
{
    public class Fun : Command
    {
        public Fun()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static Fun Instance { get; private set; }

        public override string EnglishName => "Fun";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            string s = @"  
                    _ _____           _     
                   (_)_   _|         | |    
              __ _  _  | | ___   ___ | |___ 
             / _` || | | |/ _ \ / _ \| / __|
            | (_| || | | | (_) | (_) | \__ \
             \__, || | \_/\___/ \___/|_|___/
              __/ |/ |                      
             |___/__/
                                  ";


            Rhino.UI.Dialogs.ShowMessage(s, "V1");

            return Result.Success;
        }
    }
}