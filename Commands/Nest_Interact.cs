using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.DocObjects;

namespace Nest_Interact.Commands
{
    public class Nest_Interact : Command
    {
        public Nest_Interact()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static Nest_Interact Instance { get; private set; }

        public override string EnglishName => "Nest_Interactive";

        private double _Height = 46.0;
        private double _PartSpacing = 0.125;

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // The data object holding the nest
            var SData = new Prog_Classes.StackData { Height = _Height, PartSpacing = _PartSpacing };

            // Custom Get Object Command
            var Height = new OptionDouble(SData.Height, 6, 200);
            var PartSpacing = new OptionDouble(SData.PartSpacing, 0, 1);
            var go = new GetObject()
            {
                AlreadySelectedObjectSelect = true,
                GeometryFilter = ObjectType.Curve,
                GroupSelect = true
            };
            go.SetCommandPrompt($"Select Objects");
            go.AddOptionDouble("SheetHeight", ref Height, "New Sheet Height");
            go.AddOptionDouble("PartSpacing", ref PartSpacing, "New Part Spacing");

            // Get object loop
            while (true)
            {
                var res = go.GetMultiple(1, 20);

                if (go.CommandResult() != Result.Success)
                    return Result.Cancel;

                if (res == GetResult.Option)
                {
                    // Assign the options to the data obj
                    SData.Height = _Height = Height.CurrentValue;
                    SData.PartSpacing = _PartSpacing = PartSpacing.CurrentValue;
                    continue;
                }
                else if (res == GetResult.Object)
                {
                    if (go.ObjectCount == 0)
                        continue;

                    // assign the objects and break
                    var obj = new List<ObjRef>(go.Objects());
                    foreach(var o in obj)
                        if (o.Curve() != null)
                            SData.Crv.Add(o.Curve());

                    break;
                }

                // Completed and Exit
                break;
            }

            // add info to the object
            SData.ProcessPart(doc, go.Object(0).Object().Attributes.LayerIndex);
            SData.OriginalPart.AddRange(go.Objects());

            // Custom GetPoint
            var Draw = new Prog_Classes.TwoStack { SData = SData };
                Draw.SetCommandPrompt("1 = Control UP Point,  2 = Control RIGHT Point,  +/- To Change Qty,   Enter to Accept..");
                Draw.AcceptNothing(true);   // for control points
                Draw.AcceptString(true);    // for adding more qty
                Draw.SetBasePoint(SData.BasePts.Base, true);
                Draw.AcceptNumber(true, false);
            
            // Start the show
            while (true)
            {
                Draw.Get();

                if (Draw.CommandResult() != Result.Success)
                    break;

                // continue the loop for more points
                if (Draw.Result() == GetResult.Point)
                {
                    // Exit the loop
                    if (Draw.Phase == 3)
                    {
                        // Phase 3 ending triggers the object write
                        DrawObjects(SData);
                        break;
                    }

                    // Reset the pointselect
                    if (Draw.PointSelect == 0)
                        continue;

                    if (Draw.PointSelect == 1 && Draw.Phase == 1)
                        SData.BasePts.Up = Draw.Point();
                    else if(Draw.PointSelect == 2 && Draw.Phase == 1)
                        SData.BasePts.Right = Draw.Point();
                    else if (Draw.PointSelect == 1 && Draw.Phase == 2)
                        SData.BasePts.NextCol = Draw.Point();

                    // Reset it to Keep tranform
                    Draw.PointSelect = 0;

                    continue;
                }

                // Number select a control point
                if (Draw.Result() == GetResult.Number)
                {
                    var ContPoint = Draw.Number();
                    if (ContPoint == 1 || ContPoint == 2)
                        Draw.PointSelect = (int)ContPoint;

                    continue;
                }

                // For adding to qty up
                if (Draw.Result() == GetResult.String)
                {
                    if (Draw.StringResult().Trim() == "+")
                        SData.QtyUp++;
                    else if (Draw.StringResult().Trim() == "-")
                        SData.QtyUp--;

                    continue;
                }

                // Nothing means user is finished
                if (Draw.Result() == GetResult.Nothing)
                {
                    // Phase 2 to Place Stack 3 & 4
                    if (Draw.Phase == 1)
                    {
                        // advance Phase and start next point active
                        Draw.Phase++;
                        Draw.PointSelect = 1;
                        Draw.AcceptString(false);
                        Draw.SetBasePoint(SData.GetStack1LeftCenter(), true);
                        Draw.SetCommandPrompt("1 = Control Stack #2,  Enter to Accept..");
                        continue;
                    }

                    //  Phase 3 for filling the rest of the sheet
                    if (Draw.Phase == 2)
                    {
                        Draw.Phase++;
                        Draw.SetCommandPrompt("Drag to Fill..");
                        SData.CalcAllBounds();
                        continue;
                    }

                    // Phase 3 ending triggers the object write
                    DrawObjects(SData);
                    break;
                }

                // No conditions were met
                break;
            }

            // Dispose and Redraw
            Draw.Dispose();
            doc.Views.Redraw();
            return Result.Success;
        }


        /// <summary>
        /// Draw the objects in the stackdata class
        /// </summary>
        /// <param name="SData"></param>
        public void DrawObjects(Prog_Classes.StackData SData)
        {
            // Take away the first element so there isnt a duplicate
            SData.Crv_Stack1.RemoveRange(0, SData.Crv.Count);

            // data from the inputs
            var doc = SData.doc;
            var attr = new ObjectAttributes { LayerIndex = SData.PartLayer };

            // Collect All Objects
            var Stk12 = new List<Curve>(SData.Crv.Count + SData.Crv_Stack1.Count + SData.Crv_Stack2.Count);
            Stk12.AddRange(SData.Crv);
            Stk12.AddRange(SData.Crv_Stack1);
            Stk12.AddRange(SData.Crv_Stack2);

            // Translate vector
            var xform = new Vector3d(SData.BasePts.NextCol - SData.BasePts.StackBase);

            // Fill qty's
            var stkDupes = new List<Curve>();
            for (int i = 1; i < SData.QtyAccross; i++)
            {
                for (int ii = 0; ii < Stk12.Count; ii++)
                {
                    var dupe = Stk12[ii].DuplicateCurve();
                    dupe.Translate(xform * i);
                    stkDupes.Add(dupe);
                }
            }

            // join all
            Stk12.AddRange(stkDupes);

            // Rotate all strait
            var rot = SData.StraitenRotValue();
            for(int i = 0; i < Stk12.Count; i++)
                Stk12[i].Rotate(rot, Vector3d.ZAxis, SData.BBox.All.Min);

            // Add Stack 1
            for (int i = 0; i < Stk12.Count; i++)
                doc.Objects.AddCurve(Stk12[i], attr);

            // Get overall Bounds
            var nBB = BoundingBox.Empty;
            foreach (var o in Stk12)
                nBB.Union(o.GetBoundingBox(true));

            // Move Original to top-center of layout
            var xForm = new Vector3d(nBB.Center - SData.BBox.Single.Center);
            xForm.Y += SData.Height;
            foreach(var o in SData.OriginalPart)
            {
                var rObj = o.Object();
                rObj.Geometry.Translate(xForm);
                rObj.CommitChanges();
            }
        }
    }
}