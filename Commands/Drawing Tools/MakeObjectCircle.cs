using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input.Custom;
using Rhino.Input;

namespace gjTools.Commands.Drawing_Tools
{
    public class MakeObjectCircle : Command
    {
        public MakeObjectCircle()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static MakeObjectCircle Instance { get; private set; }

        public override string EnglishName => "MakeObjectCircle";
        private double m_Tolerance = 0.05;

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var go = new GetObject();
            go.SetCommandPrompt($"Select Items to Convert to Circles <Tolerance = {m_Tolerance}>");
            go.AcceptNumber(true, true);
            go.GeometryFilter = ObjectType.Curve;

            // start the get loop
            while(true)
            {
                var res = go.GetMultiple(1, 0);

                if (res == GetResult.Cancel)
                    return Result.Cancel;

                if (res == GetResult.Number)
                {
                    m_Tolerance = go.Number();
                    go.SetCommandPrompt($"Select Items to Convert to Circles <Tolerance = {m_Tolerance}>");
                    continue;
                }

                if (res == GetResult.Object)
                    break;
            }

            // time to convert to circles
            var oRef = new List<ObjRef>(go.Objects());
            doc.Objects.UnselectAll();

            foreach(ObjRef obj in oRef)
            {
                var crv = obj.Curve();
                if (crv.TryGetCircle(out Circle ConvCircle, m_Tolerance))
                {
                    doc.Objects.Replace(obj, ConvCircle);
                    doc.Objects.Select(obj);
                }
            }

            doc.Views.Redraw();
            return Result.Success;
        }
    }
}