using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;

namespace gjTools
{
    public class NestingBox : Command
    {
        public NestingBox()
        {
            Instance = this;
        }

        public static NestingBox Instance { get; private set; }

        public override string EnglishName => "NestingBoxCS";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var obj = new GetObject();
                obj.SetCommandPrompt("Select Objects");
                obj.GeometryFilter = Rhino.DocObjects.ObjectType.Curve;
                obj.GetMultiple(1, 0);

            if (obj == null)
                return Result.Cancel;

            BoundingBox bb = obj.Object(0).Curve().GetBoundingBox(true);
            

            foreach (var o in obj.Objects())
            {
                o.Curve().GetLength()
                bb.Union(o.Curve().GetBoundingBox(true));
            }

            return Result.Success;
        }
    }
}
