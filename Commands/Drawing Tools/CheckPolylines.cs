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
            var BadPart = false;

            for (int i = 0; i < RObj.Count; i++)
            {
                var typ = RObj[i].Geometry.ObjectType;

                if (typ == ObjectType.Curve)
                {
                    var crv = RObj[i].Geometry as Curve;
                    var segs = crv.DuplicateSegments();

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
                    if (!crv.IsPlanar(0.001))
                    {
                        RhinoApp.WriteLine("Curve is not Planar, and that's real bad cowboy...");
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