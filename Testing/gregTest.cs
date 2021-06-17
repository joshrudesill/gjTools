using System;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;

namespace gjTools.Testing
{
    public class gregTest : Command
    {
        public gregTest()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static gregTest Instance { get; private set; }

        public override string EnglishName => "gjGregTest";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            Plane pl = new Plane(Plane.WorldXY);

            Rectangle3d rec = new Rectangle3d(pl, 10, 5);
            
            doc.Objects.AddRectangle(rec);

            pl.Rotate(0.26, new Vector3d(0, 0, 1));
            rec.Plane = pl;

            doc.Objects.AddRectangle(rec);

            doc.Views.Redraw();
            return Result.Success;
        }
    }
}