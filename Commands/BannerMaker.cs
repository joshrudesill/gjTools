using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.UI;
using Rhino.Geometry;

namespace gjTools.Commands
{
    public class BannerMaker : Command
    {
        public BannerMaker()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static BannerMaker Instance { get; private set; }

        public override string EnglishName => "MyRhinoCommand1";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // Ask if banner is custom or coded
            Dialogs.ShowComboListBox("Banner Maker", "Choose Banner Method", )

            return Result.Success;
        }

        public enum edgeType { raw, hem, pocket }
        public struct Banner
        {
            public string partNum;

            private Rectangle3d _CutSize;
            private Rectangle3d _LiveArea;
            private Rectangle3d _Stitch;

            public edgeType sideFinish;
            public edgeType topFinish;
            public edgeType bottFinish;
            public double SideSize;
            public double TopSize;
            public double BottSize;

            public double gromFromEdge { get { return 0.516; } }
            public double stitchExtra { get { return 0.25; } }
            public double GromDiameter { get { return 0.75; } }
            public Rectangle3d LiveArea(double width, double height)
            {
                _LiveArea = new Rectangle3d(Plane.WorldXY, width, height);
                return _LiveArea;
            }
        }
    }
}