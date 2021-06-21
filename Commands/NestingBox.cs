﻿using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
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

        public override string EnglishName => "gjNestingBox";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var obj = new GetObject();
                obj.SetCommandPrompt("Select Objects");
                obj.GeometryFilter = Rhino.DocObjects.ObjectType.Curve;
                obj.GetMultiple(1, 0);

            if (obj.CommandResult() != Result.Success)
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

            Guid boxID = doc.Objects.AddRectangle(nestBox);

            var lt = new LayerTools(doc);
            lt.AddObjectsToLayer(boxID, "NestBox", cuts.parentLayer.Name);

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

            if (num.CommandResult() != Result.Success) return -1.0;
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
            // build the text values
            string partNumber = "PN: " + cutInfo.parentLayer;
            
            string path = "File Not Saved";
            if (doc.Name != "")
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

            var tool = new DrawTools(doc);
            int ds = tool.StandardDimstyle();

            var textEnt = new List<TextEntity>
            {
                tool.AddText(partNumber, new Point3d(0.1, -0.1, 0), ds, 0.135, 1),
                tool.AddText(path, new Point3d(0.1, -0.35, 0), ds, 0.07),
                tool.AddText(shtSizeInfo, new Point3d(0.1, -0.6, 0), ds, 0.1, 1),
                tool.AddText(itemLine, new Point3d(0.1, -0.8, 0), ds, 0.1, 2)
            };

            BoundingBox tbb = BoundingBox.Empty;
            foreach (TextEntity t in textEnt)
                tbb.Union(t.GetBoundingBox(true));

            textEnt.Add(tool.AddText(timeStamp, tbb.GetCorners()[2], ds, 0.06, 1, 2));

            var box = new Rectangle3d(
                Plane.WorldXY,
                tbb.GetCorners()[0].DistanceTo(tbb.GetCorners()[1]) + 0.2,
                -tbb.GetCorners()[1].DistanceTo(tbb.GetCorners()[2]) - 0.2
            );
            var line = new Line(
                new Point3d(0.1, tbb.Center.Y, 0),
                new Point3d(tbb.GetCorners()[1].X, tbb.Center.Y, 0)
            );

            // move data
            Point3d basePt = nestBox.Corner(0);
            Transform Xmove = Transform.Translation(basePt.X, basePt.Y, 0);

            // scale data
            double scale = 1;
            var allowedWidth = new List<int> { 9, 12, 23, 36, 44, 65, 142, 200 };
            foreach (int i in allowedWidth)
                if (nestBox.Width > i)
                    scale = i / box.Width;
            Transform Xscale = Transform.Scale(basePt, scale);

            // Perform the translates
            box.Transform(Xmove);
            box.Transform(Xscale);
            line.Transform(Xmove);
            line.Transform(Xscale);

            var objIDs = new List<Guid>();

            foreach (var i in textEnt)
            {
                i.Transform(Xmove);
                i.Transform(Xscale);
                i.TextHeight = i.TextHeight * scale;
                objIDs.Add(doc.Objects.Add(i));
            }

            objIDs.Add(doc.Objects.AddRectangle(box));
            objIDs.Add(doc.Objects.AddLine(line));

            doc.Groups.Add(objIDs);
            foreach (Guid i in objIDs)
            {
                var obj = doc.Objects.FindId(i);
                    obj.Attributes.LayerIndex = cutInfo.parentLayer.Index;
                    obj.CommitChanges();
            }
                
            doc.Views.Redraw();
        }
    }
}
