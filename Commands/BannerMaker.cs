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

        public override string EnglishName => "gjBannerMaker";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var BData = new Banner();

            // Ask if banner is custom or coded
            var Mode = (string)Dialogs.ShowComboListBox("Banner Maker", "Choose Banner Method", new List<string> { "Use Code", "Manual Mode" });
            if (Mode == null)
                return Result.Cancel;

            if (Mode == "Use Code")
            {
                var code = "FORCAD:56.000073.00002.00000000000000000010000000000000300000001.00000000000018.291318.291323.95800000001";
                var res = Rhino.Input.RhinoGet.GetString("Enter Code", false, ref code);
                if (res != Result.Success)
                    return Result.Cancel;

                BData = ParseCode(code, BData);
            }

            return Result.Success;
        }


        public Banner ParseCode(string Code, Banner BData)
        {
            int byt = 7;
            var snip = new List<string>();
            
            for (int i = 1; i < Code.Length / byt; i++)
            {
                var str = Code.Substring(byt * i, byt);
                snip.Add(str);
            }
            Dialogs.ShowListBox("Test", "Test", snip);

            return BData;
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

            public double gromFromEdge { get { return 0.563; } }
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