using System;
using Rhino;
using Rhino.Commands;
using Rhino.Input;

namespace gjTools.Commands
{
    public class EP_CreateXML : Command
    {
        public EP_CreateXML()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static EP_CreateXML Instance { get; private set; }

        public override string EnglishName => "EP_CreateXML";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            RhinoApp.WriteLine("Create the E&P XML File (Made for Automation ReportMaker)");

            var CutLayer = doc.Layers.FindByFullPath("CUT::NestBox", -1);
            if (CutLayer == -1)
            {
                RhinoApp.WriteLine("CUT Layer Not Found -or- NestBox is needing to be updated");
                return Result.Cancel;
            }

            var NestBox = doc.Objects.FindByLayer(doc.Layers[CutLayer])[0];
            var NestInfo = NestBox.Attributes.GetUserStrings();
            var qtyUp = (NestInfo.Get("QtyGrp") == "0") ? NestInfo.Get("QtyObj") : NestInfo.Get("QtyGrp");
            string XMLDoc = "<JDF>\n  " +
                $"<DrawingNumber>{doc.Name.Replace(".3dm", "")}</DrawingNumber>\n  " +
                $"<CADSheetWidth>{NestInfo.Get("Width")}</CADSheetWidth>\n  " +
                $"<CADSheetHeight>{NestInfo.Get("Height")}</CADSheetHeight>\n  " +
                $"<CADNumberUp>{qtyUp}</CADNumberUp>\n</JDF>";

            var path = FileLocations.PathDict["EP"] + doc.Name.Replace(".3dm", "\\") + doc.Name.Replace(".3dm", ".xml");
            System.IO.File.WriteAllText(path, XMLDoc);

            RhinoApp.WriteLine($"Wrote XML: {path}");
            return Result.Success;
        }
    }
}