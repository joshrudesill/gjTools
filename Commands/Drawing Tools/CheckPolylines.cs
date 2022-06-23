using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.DocObjects;
using Rhino.Input;

namespace gjTools.Commands.Drawing_Tools
{
    public class CheckPolylines : Command
    {
        public CheckPolylines()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static CheckPolylines Instance { get; private set; }

        public override string EnglishName => "CheckPolylines";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            if (RhinoGet.GetMultipleObjects("Select Objects to Check", false, ObjectType.Curve, out ObjRef[] oRefs) != Result.Success)
                return Result.Cancel;

            var rObjs = new List<RhinoObject>(oRefs.Length);
            foreach (ObjRef o in oRefs)
                rObjs.Add(o.Object());

            // custom show
            var Disp = new Rhino.Display.CustomDisplay(false);
            bool BadParts = CheckPolyLines(rObjs, doc, Disp);

            // stop for contemplation
            if (BadParts)
            {
                Disp.Enabled = true;
                doc.Views.Redraw();

                string Fuck = "";
                RhinoGet.GetString("Enter to Continue...", true, ref Fuck);

                Disp.Enabled = false;
            }
            else
                RhinoApp.WriteLine("Your Selections are Polylines, nothing to show you here.");

            Disp.Dispose();
            doc.Views.Redraw();
            return Result.Success;
        }




        /// <summary>
        /// Highlight Bad Lines (if any) (All inputs are Assumed Curves or text)
        /// </summary>
        /// <param name="RObj"></param>
        /// <returns></returns>
        public bool CheckPolyLines(List<RhinoObject> RObj, RhinoDoc doc, Rhino.Display.CustomDisplay Disp)
        {
            var c_red = System.Drawing.Color.OrangeRed;
            var c_blue = System.Drawing.Color.Blue;
            var c_brn = System.Drawing.Color.MediumPurple;
            var BadPart = false;

            for (int i = 0; i < RObj.Count; i++)
            {
                var typ = RObj[i].Geometry.ObjectType;

                if (typ == ObjectType.Curve)
                {
                    var crv = RObj[i].Geometry as Curve;
                    var segs = crv.DuplicateSegments();

                    // polylines are supposed to be rational, so lets test that
                    {
                        var nrb = crv.ToNurbsCurve();

                        // if rational the pointsize cant equal dimension
                        if (nrb.Points.PointSize == nrb.Dimension && nrb.Degree != 1)
                        {
                            RhinoApp.WriteLine("Blue Curves are Irrational, and need to be Converted");
                            Disp.AddCurve(nrb, c_blue, 5);
                            BadPart = true;
                        }
                    }

                    // Replace Circle
                    if (crv.IsCircle())
                    {
                        // Convert to circle
                        crv.TryGetCircle(out Circle Cir, 0.05);

                        if (Cir.IsValid)
                            doc.Objects.Replace(RObj[i].Id, Cir);
                        continue;
                    }

                    for (int ii = 0; ii < segs.Length; ii++)
                    {
                        Curve s = segs[ii];

                        if (!s.IsLinear() && !s.IsArc())
                        {
                            Disp.AddCurve(s, c_red, 5);
                            BadPart = true;
                        }
                    }

                    // Testing to see if the Curve is planar
                    if (!crv.IsInPlane(Plane.WorldXY))
                    {
                        RhinoApp.WriteLine("Purple Curves are not Planar");
                        Disp.AddCurve(crv, c_brn, 5);
                    }
                }
                else if (typ == ObjectType.Annotation)
                {
                    var te = RObj[i] as TextObject;

                    if (te == null)
                        BadPart = true;
                }
            }

            return BadPart;
        }
    }
}