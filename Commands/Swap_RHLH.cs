using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.DocObjects;
using Rhino.Input;

namespace gjTools.Commands
{
    public class Swap_RHLH : Command
    {
        public Swap_RHLH()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static Swap_RHLH Instance { get; private set; }

        public override string EnglishName => "SwapRHLH";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // get the objects to swap
            if (RhinoGet.GetMultipleObjects("Select THRU Cut Objects", false, ObjectType.Curve, out ObjRef[] obj) != Result.Success)
                return Result.Cancel;

            foreach(var o in obj)
            {
                var rObj = o.Object();
                var lay = doc.Layers[rObj.Attributes.LayerIndex];

                FlipColorRHLH(lay, rObj);
            }

            doc.Views.Redraw();
            return Result.Success;
        }

        public static void FlipColorRHLH(Layer lay, RhinoObject rObj)
        {
            // Check if a cut object
            if (lay.Name != "C_THRU")
                return;

            // Swap the color
            if (rObj.Attributes.ColorSource == ObjectColorSource.ColorFromLayer)
            {
                rObj.Attributes.ColorSource = ObjectColorSource.ColorFromObject;
                rObj.Attributes.ObjectColor = System.Drawing.Color.DarkGreen;
            }
            else
                rObj.Attributes.ColorSource = ObjectColorSource.ColorFromLayer;

            // set new vals
            rObj.CommitChanges();
        }
    }
}