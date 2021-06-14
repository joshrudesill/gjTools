using System;
using Rhino;
using Rhino.Geometry;
using Rhino.Input.Custom;
using Rhino.Commands;
using System.Collections;
using System.Collections.Generic;

namespace gjTools
{
    public class DrawingTools : Command
    {
        public DrawingTools()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static DrawingTools Instance { get; private set; }

        public override string EnglishName => "drawingTools";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // ask for input
            var options = new List<string>(2);
            options.Add("Part Boundries");
            options.Add("Check for Polylines");

            string operation = (string)Rhino.UI.Dialogs.ShowListBox("Part Operations", "Choose Operation", options);
            if (operation == null)
                return Result.Cancel;


            // make part boundries
            if (operation == (string)options[0])
            {
                var gt = new genTools();

                List<string> selections = gt.SelParentLayers(doc, true);

                RhinoApp.Write("You Chose ");
                foreach (string sel in selections)
                    RhinoApp.Write(sel + ", ");
            }


            // Check for bad polylines
            if (operation == (string)options[1])
            {
                var objs = new GetObject();
                    objs.SetCommandPrompt("Select Objects to Check");
                    objs.GeometryFilter = Rhino.DocObjects.ObjectType.Curve;
                    objs.GetMultiple(1, 0);
                
                if (objs.CommandResult() != Result.Success)
                    return objs.CommandResult();

                var gt = new genTools();

                for ( int i = 0; i < objs.Objects().Length; i++)
                {
                    if (!gt.CheckPolylines(objs.Object(i).Curve(), doc, false))
                        break;
                }
            }

            return Result.Success;
        }
    }
}