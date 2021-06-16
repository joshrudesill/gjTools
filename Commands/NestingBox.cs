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
            var cuts = new CutOperations(new List<Rhino.DocObjects.ObjRef>(obj.Objects()), doc);
            var cutLays = cuts.CutLayers();

            // DELETE: see if the kerf class is good
            foreach (string c in cutLays)
                RhinoApp.WriteLine(c + ": " + cuts.CutLengthByLayer(c));

            foreach (var o in obj.Objects())
                bb.Union(o.Curve().GetBoundingBox(true));

            // Time to collect the sheet input sizes
            var sheetHeight = numFromUser("Sheet Height", 48.0);
            if (sheetHeight == -1) return Result.Cancel;
            var sheetWidth = numFromUser("Sheet Margin/Length", 1.0);
            if (sheetWidth == -1) return Result.Cancel;

            // DELETE: see if the inputs r gud
            RhinoApp.WriteLine("Dims are: " + sheetWidth + " x " + sheetHeight);
            RhinoApp.WriteLine("Dims are: " + RoundQuarterIn(sheetWidth) + " x " + RoundQuarterIn(sheetHeight));


            return Result.Success;
        }

        public double numFromUser(string message="", double defaultNum=0.0)
        {
            var num = new GetNumber();
                num.SetCommandPrompt(message);
                num.SetDefaultNumber(defaultNum);
            num.Get();

            if (num == null) return -1.0;
            return num.Number();
        }

        public double RoundQuarterIn(double num)
        {
            int wholeNum = (int)num;
            int afterDecimal = (int)((num - wholeNum) * 100);
            var ranges = new List<int> { 10, 35, 60, 85 };
            for (int i = 0;i < 4; i++)
            {
                if (afterDecimal < ranges[i])
                {
                    afterDecimal = 25 * i;
                    break;
                }
            }

            return wholeNum + (afterDecimal * 0.01);
        }
    }
}
