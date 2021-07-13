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

            // Enter Code
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
            var snip = new List<double>();
            
            for (int i = 1; i < Code.Length / byt; i++)
                snip.Add(double.Parse(Code.Substring(byt * i, byt)));

            BData.LiveArea(snip[0], snip[1]);

            // top finishing
            if (snip[2] > 0 || snip[7] > 0)
            {
                BData.TopSize = (snip[2] > 0) ? snip[2] : snip[7];
                BData.topFinish = (snip[2] > 0) ? edgeType.pocket : edgeType.hem;
            }
            // bottom finishing
            if (snip[3] > 0 || snip[8] > 0)
            {
                BData.BottSize = (snip[3] > 0) ? snip[3] : snip[8];
                BData.bottFinish = (snip[3] > 0) ? edgeType.pocket : edgeType.hem;
            }
            // Side Finishing
            if (snip[9] > 0)
            {
                BData.sideFinish = edgeType.hem;
                BData.SideSize = snip[9];
            }

            // Grommet Data
            BData.gromTopSpace = snip[10];
            BData.gromBottSpace = snip[11];
            BData.gromSideSpace = snip[12];

            // Folded Banner?
            if (snip[13] > 0)
                BData.DoubleSided = true;

            return BData;
        }

        public enum edgeType { raw, hem, pocket }
        public struct Banner
        {
            public string partNum;

            public bool DoubleSided;

            private Rectangle3d _CutSize;
            private Rectangle3d _LiveArea;
            private Rectangle3d _Stitch;

            public edgeType sideFinish;
            public edgeType topFinish;
            public edgeType bottFinish;
            public double SideSize;
            public double TopSize;
            public double BottSize;

            public double GromDiameter { get { return 0.75; } }
            public double gromFromEdge { get { return 0.563; } }
            public double gromTopSpace;
            public double gromSideSpace;
            public double gromBottSpace;

            public double stitchExtra;
            
            public Rectangle3d LiveArea(double width, double height)
            {
                _LiveArea = new Rectangle3d(Plane.WorldXY, width, height);
                topFinish = edgeType.raw;
                bottFinish = edgeType.raw;
                sideFinish = edgeType.raw;
                stitchExtra = 0.25;
                DoubleSided = false;
                return _LiveArea;
            }
        }
    }
}