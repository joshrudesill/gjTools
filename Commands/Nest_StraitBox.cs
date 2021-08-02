using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.DocObjects;

namespace gjTools.Commands
{
    public class Nest_StraitBox : Command
    {
        public Nest_StraitBox()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static Nest_StraitBox Instance { get; private set; }

        public override string EnglishName => "Nest_StraitBox";
        public double space = 0.25;

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            if (RhinoGet.GetNumber("Space Between Parts", false, ref space) != Result.Success)
                return Result.Cancel;
            
            if (RhinoGet.GetMultipleObjects("Select Objects", false, ObjectType.Curve, out ObjRef[] obj) != Result.Success)
                return Result.Cancel;

            MakeNest(obj);

            return Result.Success;
        }

        public void MakeNest(ObjRef[] obj)
        {
            var disp = new Rhino.Display.CustomDisplay(true);
            var rObj = new List<RhinoObject>();

            // get rhinoObject list
            foreach (var o in obj)
                rObj.Add(o.Object());

            RhinoObject.GetTightBoundingBox(rObj, out BoundingBox bb);


        }
    }
}