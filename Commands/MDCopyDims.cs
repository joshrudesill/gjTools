using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using System;
using Rhino.DocObjects;
using System.Collections.Generic;

namespace gjTools.Commands
{
    public class MDCopyDims : Command
    {
        public MDCopyDims()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static MDCopyDims Instance { get; private set; }

        public override string EnglishName => "MDCopyDims";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            Result dims = Rhino.Input.RhinoGet.GetMultipleObjects("Select everything that will need to be copied and layered.",  false, ObjectType.AnyObject, out ObjRef[] objref);
            if (dims != Result.Success) { return Result.Cancel; }
            doc.Objects.UnselectAll();

            Result cutobj = Rhino.Input.RhinoGet.GetMultipleObjects("Select only thru cut", false, ObjectType.AnyObject, out ObjRef[] cutref);
            if (cutobj != Result.Success) { return Result.Cancel; }

            var i = objref.Length;
            var w = cutref.Length;
            return Result.Success;
        }
    }
}