using System.Collections.Generic;
using System;
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
        /// <summary> Qty, rows, columns </summary>
        public List<int> data = new List<int> { 1, 1, 1 };
        public Box selectedBB = Box.Empty;

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            if (RhinoGet.GetMultipleObjects("Select Objects", false, ObjectType.Curve, out ObjRef[] obj) != Result.Success)
                return Result.Cancel;

            // Reset things
            data[0] = data[1] = data[2] = 1;
            selectedBB = Box.Empty;
            doc.Objects.UnselectAll();

            if (MakeNest(obj))
                GridObjCopy(obj);

            return Result.Success;
        }



        public bool MakeNest(ObjRef[] obj)
        {
            var rObj = new List<RhinoObject>();

            // Command options
            var s_Gutter = new OptionDouble(space, 0, 10);

            // get rhinoObject list
            foreach (var o in obj)
                rObj.Add(o.Object());

            RhinoObject.GetTightBoundingBox(rObj, out BoundingBox bb);

            var gp = new CustomDynamic()
            {
                origin = bb.GetCorners()[0],
                ObjectBB = new Box(bb),
                gutter = space
            };
            gp.SetCommandPrompt("Drag a Box, or ");
            gp.AddOptionDouble("Gutter", ref s_Gutter);
            var res = gp.Get();

            while (true)
            {
                if (res == GetResult.Cancel || res == GetResult.Point)
                    break;
                space = gp.gutter = s_Gutter.CurrentValue;
                res = gp.Get();
            }

            // time to write the Data
            if (res == GetResult.Point && gp.data[0] > 1)
            {
                selectedBB = gp.ObjectBB;
                data = gp.data;
                return true;
            }
            
            return false;
        }

        public void GridObjCopy(ObjRef[] obj)
        {
            var doc = obj[0].Object().Document;
            var colObj = new List<Guid>();
            
            // copy to columns
            if (data[2] > 1)
            {
                for(var i = 1; i < data[2]; i++)
                {
                    var copyObj = Transform.Translation((selectedBB.X.Length + space) * i, 0, 0);
                    foreach (var o in obj)
                        colObj.Add(doc.Objects.Transform(o, copyObj, false));
                }
            }
            if (data[1] > 1)
            {
                for (var i = 1; i < data[1]; i++)
                {
                    var copyObj = Transform.Translation(0, (selectedBB.Y.Length + space) * i, 0);
                    foreach (var o in obj)
                        doc.Objects.Transform(o, copyObj, false);
                    foreach (var o in colObj)
                        doc.Objects.Transform(o, copyObj, false);
                }
            }
        }
    }

    public class CustomDynamic : GetPoint
    {
        public Point3d origin;
        public Box ObjectBB;
        public double gutter;
        /// <summary>
        /// Qty, rows, columns
        /// </summary>
        public List<int> data = new List<int> { 1, 1, 1 };

        protected override void OnDynamicDraw(GetPointDrawEventArgs e)
        {
            base.OnDynamicDraw(e);
            
            var bb = new Box(Plane.WorldXY, new List<Point3d> { origin, e.CurrentPoint });
            e.Display.DrawBox(bb, System.Drawing.Color.Aqua, 1);

            if (bb.X.Length > ObjectBB.X.Length && bb.Y.Length > ObjectBB.Y.Length)
            {
                // Object does fit within the drag box
                data[2] = (int)(bb.X.Length / (ObjectBB.X.Length + gutter));
                data[1] = (int)(bb.Y.Length / (ObjectBB.Y.Length + gutter));
                data[0] = data[1] * data[2];

                var justBoxes = new List<Box>();
                var justBoxesRow = new List<Box>();

                if (bb.X.Length > ObjectBB.X.Length)
                {
                    for(var i = 0; i < data[2]; i++)
                    {
                        // columns
                        var copyBox = Transform.Translation((ObjectBB.X.Length + gutter) * i, 0, 0);
                        var cBox = new Box(ObjectBB);
                            cBox.Transform(copyBox);
                        justBoxes.Add(cBox);
                    }
                }
                if (bb.Y.Length > ObjectBB.Y.Length)
                {
                    for(var i = 0; i < data[1]; i++)
                    {
                        // Rows
                        var copyBox = Transform.Translation(0, (ObjectBB.Y.Length + gutter) * i, 0);
                        foreach (var r in justBoxes)
                        {
                            var cBox = new Box(r);
                                cBox.Transform(copyBox);
                            justBoxesRow.Add(cBox);
                        }
                    }
                    justBoxes.AddRange(justBoxesRow);
                }

                // draw the boxes
                foreach (var b in justBoxes)
                    e.Display.DrawBox(b, System.Drawing.Color.Aquamarine, 1);
            }

            // set the information right
            var pt = new Point3d(e.CurrentPoint);
                pt.Y -= 2;
                pt.X += 2;
            e.Display.Draw2dText(
                $"Rows: {data[1]}, Columns: {data[2]}, Total: {data[0]}\n" +
                $"Height: {data[1] * ObjectBB.Y.Length + (gutter * (data[1] - 1))}in, Width: {data[2] * ObjectBB.X.Length + (gutter * (data[2] - 1))}in",
                System.Drawing.Color.DarkRed,
                pt, false, 12, "Consolas"
            );
        }
    }
}