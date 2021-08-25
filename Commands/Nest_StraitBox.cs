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
    public struct NestGrid
    {
        public int Qty { get; set; }
        public int Rows { get; set; }
        public int Columns { get; set; }
        public double Spacing { get; set; }
        public bool grouped { get; set; }
        public void ResetQtys()
        {
            Qty = Rows = Columns = 0;
        }
    }


    public class Nest_StraitBox : Command
    {
        public Nest_StraitBox()
        {
            Instance = this;
        }
        
        ///<summary>The only instance of the MyCommand command.</summary>
        public static Nest_StraitBox Instance { get; private set; }

        public override string EnglishName => "Nest_StraitBox";
        public NestGrid Data = new NestGrid { Qty = 0, Rows = 0, Columns = 0, grouped = true, Spacing = 0.25 };
        public Box selectedBB = Box.Empty;

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            if (RhinoGet.GetMultipleObjects("Select Objects", false, ObjectType.Curve, out ObjRef[] obj) != Result.Success)
                return Result.Cancel;

            Data.ResetQtys();
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
            var opt_Gutter = new OptionDouble(Data.Spacing, 0, 100);
            var opt_Group = new OptionToggle(Data.grouped, "UnGrouped", "Grouped");

            // get rhinoObject list
            foreach (var o in obj)
                rObj.Add(o.Object());

            RhinoObject.GetTightBoundingBox(rObj, out BoundingBox bb);

            var gp = new CustomDynamic()
            {
                origin = bb.GetCorners()[0],
                ObjectBB = new Box(bb),
                data = Data
            };
            gp.SetCommandPrompt("Drag a Box, or ");
            gp.AddOptionDouble("Gutter", ref opt_Gutter);
            gp.AddOptionToggle("GroupOutput", ref opt_Group);
            var res = gp.Get();

            while (true)
            {
                if (res == GetResult.Cancel || res == GetResult.Point)
                    break;
                gp.data.Spacing = opt_Gutter.CurrentValue;
                gp.data.grouped = opt_Group.CurrentValue;
                res = gp.Get();
            }

            // time to write the Data
            if (res == GetResult.Point && gp.data.Qty > 1)
            {
                selectedBB = gp.ObjectBB;
                Data = gp.data;
                return true;
            }

            return false;
        }


        public void GridObjCopy(ObjRef[] obj)
        {
            var doc = obj[0].Object().Document;
            var column_Objects = new List<List <Guid>>();

            // Dont Group a single Object
            if (obj.Length == 1)
                Data.grouped = false;

            if (Data.Columns > 1)
            {
                for (var i = 1; i < Data.Columns; i++)
                {
                    var copyObj = Transform.Translation((selectedBB.X.Length + Data.Spacing) * i, 0, 0);
                    var new_obj = DuplicateObj(copyObj);

                    column_Objects.Add(new_obj);
                }
            }

            if (Data.Rows > 1)
            {
                for(var i = 1; i < Data.Rows; i++)
                {
                    var copyObj = Transform.Translation(0, (selectedBB.Y.Length + Data.Spacing) * i, 0);
                    DuplicateObj(copyObj);

                    if (column_Objects.Count > 0)
                        for(var ii = 0; ii < column_Objects.Count; ii++)
                            DuplicateObjId(column_Objects[ii], copyObj);
                }
            }

            List<Guid> DuplicateObj(Transform xForm)
            {
                var new_obj = new List<Guid>();
                foreach (var o in obj)
                    new_obj.Add(o.ObjectId);

                return DuplicateObjId(new_obj, xForm);
            }
            List<Guid> DuplicateObjId(List<Guid> ObjIds, Transform xForm)
            {
                var new_obj = new List<Guid>();
                int grp = -1;

                if (Data.grouped)
                    grp = doc.Groups.Add();

                foreach (var id in ObjIds)
                {
                    var o = doc.Objects.FindId(id);
                    var oo = doc.Objects.Transform(o, xForm, false);
                        o = doc.Objects.FindId(oo);
                        o.Attributes.RemoveFromAllGroups();

                    if (grp != -1)
                        o.Attributes.AddToGroup(grp);

                    o.CommitChanges();
                    new_obj.Add(oo);
                }

                return new_obj;
            }
        }
    }

    public class CustomDynamic : GetPoint
    {
        public Point3d origin;
        public Box ObjectBB;
        public NestGrid data;

        protected override void OnDynamicDraw(GetPointDrawEventArgs e)
        {
            base.OnDynamicDraw(e);
            
            var bb = new Box(Plane.WorldXY, new List<Point3d> { origin, e.CurrentPoint });
            e.Display.DrawBox(bb, System.Drawing.Color.Aqua, 1);

            if (bb.X.Length > ObjectBB.X.Length && bb.Y.Length > ObjectBB.Y.Length)
            {
                // Object does fit within the drag box
                data.Columns = (int)(bb.X.Length / (ObjectBB.X.Length + data.Spacing));
                data.Rows = (int)(bb.Y.Length / (ObjectBB.Y.Length + data.Spacing));
                data.Qty = data.Rows * data.Columns;

                var justBoxes = new List<Box>();
                var justBoxesRow = new List<Box>();

                if (bb.X.Length > ObjectBB.X.Length)
                {
                    for(var i = 0; i < data.Columns; i++)
                    {
                        // columns
                        var copyBox = Transform.Translation((ObjectBB.X.Length + data.Spacing) * i, 0, 0);
                        var cBox = new Box(ObjectBB);
                            cBox.Transform(copyBox);
                        justBoxes.Add(cBox);
                    }
                }
                if (bb.Y.Length > ObjectBB.Y.Length)
                {
                    for(var i = 0; i < data.Rows; i++)
                    {
                        // Rows
                        var copyBox = Transform.Translation(0, (ObjectBB.Y.Length + data.Spacing) * i, 0);
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
                $"Rows: {data.Rows}, Columns: {data.Columns}, Total: {data.Qty}\n" +
                $"Height: {data.Rows * ObjectBB.Y.Length + (data.Spacing * (data.Rows - 1))}in, Width: {data.Columns * ObjectBB.X.Length + (data.Spacing * (data.Columns - 1))}in",
                System.Drawing.Color.DarkRed,
                pt, false, 12, "Consolas"
            );
        }
    }
}