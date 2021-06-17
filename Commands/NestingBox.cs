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
                bb.Union(o.Curve().GetBoundingBox(true));
            Line[] edges = bb.GetEdges();

            // get object cut length object
            var cuts = new CutOperations(new List<Rhino.DocObjects.ObjRef>(obj.Objects()), doc);

            // Time to collect the sheet input sizes
            var sheetHeight = numFromUser("Sheet Height", 48.0);
                if (sheetHeight == -1) return Result.Cancel;
                sheetHeight = SheetEndSize(sheetHeight, edges[3]);

            var sheetWidth = numFromUser("Sheet Margin/Length", 1.0);
                if (sheetWidth == -1) return Result.Cancel;
                sheetWidth = SheetEndSize(sheetWidth, edges[0]);

            Point3d bottomLeft = new Point3d(bb.Center.X - sheetWidth, bb.Center.Y - sheetHeight, 0.0);
            Point3d topRight = new Point3d(bb.Center.X + sheetWidth, bb.Center.Y + sheetHeight, 0.0);
            Rectangle3d nestBox = new Rectangle3d(Plane.WorldXY, bottomLeft, topRight);

            
            doc.Objects.AddRectangle(nestBox);
            CollectInfo(cuts, doc, bb, nestBox);

            doc.Views.Redraw();
            return Result.Success;
        }


        /// <summary>
        /// Makes the decision on sheet size
        /// </summary>
        /// <param name="userVal"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        public double SheetEndSize(double userVal, Line line)
        {
            if (userVal <= 2.95)
                return RoundQuarterIn((userVal * 2) + line.Length) / 2;
            else
                return RoundQuarterIn(userVal) / 2;
        }


        /// <summary>
        /// Asks user for a sheet size number
        /// </summary>
        /// <param name="message"></param>
        /// <param name="defaultNum"></param>
        /// <returns></returns>
        public double numFromUser(string message="", double defaultNum=0.0)
        {
            var num = new GetNumber();
                num.SetCommandPrompt(message);
                num.SetDefaultNumber(defaultNum);
            num.Get();

            if (num == null) return -1.0;
            return num.Number();
        }



        /// <summary>
        /// Rounds the input to quarter inch within tolerance
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        public double RoundQuarterIn(double num)
        {
            int wholeNum = (int)num;
            int afterDecimal = (int)((num - wholeNum) * 100);
            var ranges = new List<int> { 8, 30, 60, 80 };

            if (afterDecimal >= ranges[3])
                return wholeNum + 1;

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


        /// <summary>
        /// Assembles the info from the nesting
        /// </summary>
        /// <param name="cutInfo"></param>
        /// <param name="doc"></param>
        /// <param name="bb"></param>
        /// <param name="nestBox"></param>
        public void CollectInfo(CutOperations cutInfo, RhinoDoc doc, BoundingBox bb, Rectangle3d nestBox)
        {
            // for now, build a text entity in the correct place
            string partNumber = "PN: " + cutInfo.parentLayer;
            
            string path = "File Not Saved";
            if (doc.Name != null)
                path = "Path: " + doc.Path;
            
            string shtSizeInfo = "Sheet Size: " + nestBox.Width + "w x " + 
                nestBox.Height + "h   (Part Area: " + 
                Math.Round(bb.GetEdges()[0].Length, 2) + 
                "w x " + Math.Round(bb.GetEdges()[3].Length, 2) + "h)";
            
            string itemLine = "Items up: ";
            if (cutInfo.groupInd.Count > 0)
                itemLine += cutInfo.groupInd.Count;
            else
                itemLine += cutInfo.CrvObjects.Count;

            itemLine += "    Kerf:";
            var cutNames = cutInfo.CutLayers();
            foreach (string l in cutNames)
                itemLine += " [" + l + ": " + cutInfo.CutLengthByLayer(l) + "]";

            string timeStamp = "GREG\n" + System.DateTime.UtcNow;

            

            // add text here to see all info
            RhinoApp.WriteLine(partNumber);
            RhinoApp.WriteLine(timeStamp);
            RhinoApp.WriteLine(path);
            RhinoApp.WriteLine(shtSizeInfo);
            RhinoApp.WriteLine(itemLine);
        }
    }
}
