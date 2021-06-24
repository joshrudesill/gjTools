using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.UI;
using Rhino.DocObjects;
using Rhino.Display;
using Rhino.Geometry;

namespace gjTools.Commands
{
    [CommandStyle(Style.ScriptRunner)]
    public class PDF_Export : Command
    {
        public PDF_Export()
        {
            Instance = this;
        }

        /// <summary>
        /// Holds all of the data needed to make a PDF file
        /// </summary>
        public struct PDF
        {
            public string pdfName;
            public string path;
            public int dpi;
            public List<double> sheetSize;
            public int outputColor;
            private List<ViewCaptureSettings.ColorMode> _colorMode;
            public string layoutName;

            public Layer layer;
            public RhinoDoc doc;

            public List<RhinoObject> obj;
            public BoundingBox bb;
            public Line zoomObj;

            public PDF(RhinoDoc document)
            {
                pdfName = "";
                path = "";
                dpi = 600;
                sheetSize = new List<double> { 11.0, 8.5 };
                outputColor = 0;
                _colorMode = new List<ViewCaptureSettings.ColorMode> { 
                    ViewCaptureSettings.ColorMode.DisplayColor, 
                    ViewCaptureSettings.ColorMode.PrintColor, 
                    ViewCaptureSettings.ColorMode.BlackAndWhite
                };
                layoutName = "";

                layer = new Layer();
                doc = document;

                obj = new List<RhinoObject>();
                bb = new BoundingBox();
                zoomObj = new Line();
            }

            public ViewCaptureSettings.ColorMode colorMode
            {
                get
                {
                    if (outputColor < 3)
                        return _colorMode[outputColor];
                    else
                        return _colorMode[0];
                }
            }
        }





        ///<summary>Makes my PDF Files</summary>
        public static PDF_Export Instance { get; private set; }

        public override string EnglishName => "gjPDFExport";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var sql = new SQLTools().queryLocations();
            var lt = new LayerTools(doc);
            var pdfData = new List<PDF>();

            var outTypes = new List<string>
            {
                "LocalTemp",
                "MeasuredDrawing",
                "Mylar",
                "MultiPagePDF"
            };

            // add more options if in a 3DM File
            if (doc.Path != "")
                outTypes.AddRange(new List<string>
                {
                    "EPExportLegacy",
                    "EPExport",
                    "ProtoNestings",
                    "WorkingLocation"
                });

            // Get user data
            var outType = (string)Dialogs.ShowListBox("PDF Output", "Choose a Type", outTypes);
            if (outType == null)
                return Result.Cancel;

            // see if we need a layer selector
            if (outType == "LocalTemp")
            {
                var layer = new List<string>();
                layer.AddRange(Dialogs.ShowMultiListBox("Layers", "Select Parts", lt.getAllParentLayersStrings()));
                if (layer.Count == 0)
                    return Result.Cancel;

                //  Create all PDF objects
                foreach (var l in layer)
                {
                    var page = new PDF(doc);
                    page.pdfName = l;
                    page.layer = lt.CreateLayer(l);

                    // TODO: for now assign the temp folder
                    page.path = "D:\\Temp\\";

                    page = ZoomObject(page);
                    doc.Objects.AddLine(page.zoomObj);
                    PDFViewport(page);
                    doc.Views.Redraw();
                }
            }

            return Result.Success;
        }





        /// <summary>
        /// Sends out PDF from viewport objects
        /// </summary>
        /// <param name="pdfData"></param>
        public void PDFViewport(PDF pdfData)
        {
            var view = pdfData.doc.Views.GetViewList(true, false);
            RhinoView top = view[0];
            for (var i = 0; i < view.Length; i++)
                if (view[i].MainViewport.Name == "Top")
                    top = view[i];


            // see if the folder exists or create it
            if (System.IO.Directory.Exists(pdfData.path))
            {
                System.IO.Directory.CreateDirectory(pdfData.path);
            }
            else
            {
                // directory exists, see if the pdf does, then delete
                if (System.IO.File.Exists(pdfData.path + pdfData.pdfName + ".pdf"))
                {
                    System.IO.File.Delete(pdfData.path + pdfData.pdfName + ".pdf");
                }
            }

            // do the proper zooming
            top.MainViewport.ZoomBoundingBox(pdfData.zoomObj.BoundingBox);
            pdfData.doc.Views.Redraw();

            // Construct the PDF page
            var page = Rhino.FileIO.FilePdf.Create();
            var capture = new ViewCaptureSettings(
                top, 
                new System.Drawing.Size((int)pdfData.sheetSize[0] * pdfData.dpi, (int)pdfData.sheetSize[1] * pdfData.dpi),
                pdfData.dpi
            );
            capture.OutputColor = pdfData.colorMode;

            page.AddPage(capture);
            page.Write(pdfData.path + pdfData.pdfName + ".pdf");
        }

        /// <summary>
        /// Sends out PDF from Layouts
        /// </summary>
        /// <param name="pdfdata"></param>
        public void PDFLayout(PDF pdfdata)
        {

        }

        /// <summary>
        /// Makes a multi-page PDF file
        /// </summary>
        /// <param name="pdfData"></param>
        public void PDFMultiPage(List<PDF> pdfData)
        {

        }

        /// <summary>
        /// Converts the viewport zoom to fit PDF Page
        /// </summary>
        /// <param name="pdfData"></param>
        /// <returns>modified PDF object</returns>
        public PDF ZoomObject(PDF pdfData)
        {
            var objLayers = new List<Layer>();
                objLayers.AddRange(pdfData.layer.GetChildren());
                objLayers.Add(pdfData.layer);

            // Get all objects
            foreach(var l in objLayers)
            {
                var obj = pdfData.doc.Objects.FindByLayer(l);
                if (obj != null)
                    pdfData.obj.AddRange(obj);
            }

            // Check if no objects were found
            if (pdfData.obj.Count == 0)
                return pdfData;

            // Update the overall bounding box
            foreach( var o in pdfData.obj)
                pdfData.bb.Union(o.Geometry.GetBoundingBox(true));

            // Check against the viewport resolution
            var currentView = pdfData.doc.Views.ActiveView.ClientRectangle;
        
            double width = pdfData.bb.GetEdges()[0].Length;
            double Height = pdfData.bb.GetEdges()[1].Length;

            // make page sizes scaled to content
            if (width / Height > 1.3)
            {
                // content wider than tall
                width = (width / (pdfData.sheetSize[0] - 0.4)) * pdfData.sheetSize[0];
                Height = (width / (pdfData.sheetSize[0] - 0.4)) * pdfData.sheetSize[1];
            }
            else
            {
                // content taller than wide
                width = (Height / (pdfData.sheetSize[1] - 0.4)) * pdfData.sheetSize[0];
                Height = (Height / (pdfData.sheetSize[1] - 0.4)) * pdfData.sheetSize[1];
            }

            // 1.3 based on the ratio of a landscape page
            if ((currentView.Width / currentView.Height) < 1.3)
            {
                // portrait viewport
                pdfData.zoomObj = new Line(
                    new Point3d(pdfData.bb.Center.X, pdfData.bb.Center.Y - (Height / 2), 0),
                    new Point3d(pdfData.bb.Center.X, pdfData.bb.Center.Y + (Height / 2), 0)
                );
            }
            else
            {
                // landscape viewport
                pdfData.zoomObj = new Line(
                    new Point3d(pdfData.bb.Center.X - (width / 2), pdfData.bb.Center.Y, 0),
                    new Point3d(pdfData.bb.Center.X + (width / 2), pdfData.bb.Center.Y, 0)
                );
            }

            return pdfData;
        }
    }
}