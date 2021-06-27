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

                layer = document.Layers[0];
                doc = document;

                obj = new List<RhinoObject>();
                bb = new BoundingBox();
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
            if (outType == null || (outType == "ProtoNestings" && doc.Path == ""))
                return Result.Cancel;

            // Regular Single Page output
            if (outType != "EPExportLegacy" && outType != "EPExport" && outType !=  "Mylar")
            {
                var dial = Dialogs.ShowMultiListBox("Layers", "Select Parts", lt.getAllParentLayersStrings());
                if (dial == null )
                    return Result.Cancel;

                // get proto job path
                string protoPath = sql[3].path;
                if (outType == "ProtoNestings")
                    protoPath = PrototypePath(doc);

                var layer = new List<string>(dial);
                RhinoView currentView = doc.Views.ActiveView;
                RhinoView floatView = CreateViewport(doc);
                currentView.Maximized = true;

                //  Create all PDF objects
                foreach (var l in layer)
                {
                    var page = new PDF(doc);
                    page.pdfName = l;
                    page.layer = lt.CreateLayer(l);

                    // find the right path
                    switch (outType)
                    {
                        case "LocalTemp":
                            page.path = sql[3].path;
                            break;
                        case "MeasuredDrawing":
                            page.path = sql[4].path + l + "\\";
                            break;
                        case "WorkingLocation":
                            page.path = doc.Path.Replace(doc.Name, "");
                            break;
                        case "ProtoNestings":
                            // Check the sticky info
                            page.path = protoPath;
                            break;
                        case "MultiPagePDF":
                            if (doc.Path != "")
                            {
                                page.pdfName = doc.Name;
                                page.path = doc.Path.Replace(doc.Name, "");
                            }
                            else
                            {
                                page.pdfName = "MultiPage";
                                page.path = sql[3].path;
                            }
                            break;
                    }

                    if (outType == "MultiPagePDF")
                        pdfData.Add(page);
                    else
                    {
                        PDFViewport(page, floatView);
                        // select objects
                        foreach (var o in page.obj)
                            doc.Objects.Select(new ObjRef(o));
                        MakeDXF(page.path + page.pdfName + ".dwg");
                    }
                }

                if (outType == "MultiPagePDF")
                    PDFMultiPage(pdfData, floatView);

                // delete the viewport and reset
                floatView.Close();
                doc.Views.ActiveView = currentView;
                currentView.Maximized = true;

                // Set layer back
                ShowAllLayers(doc);
            }
            else if (outType == "Mylar")
            {
                var page = new PDF(doc);
                var layouts = doc.Views.GetPageViews();
                if (layouts.Length == 0)
                    return Result.Cancel;

                var layoutNames = new List<string>();
                foreach (var l in layouts)
                    layoutNames.Add(l.MainViewport.Name);

                var dial = (string)Dialogs.ShowListBox("Layout Maker", "Choose a layout", layoutNames);
                if (dial == null)
                    return Result.Cancel;

                page.pdfName = dial;
                page.layoutName = dial;
                if (doc.Path != "")
                    page.path = doc.Path.Replace(doc.Name, "");
                else
                    page.path = sql[3].path;
                page.outputColor = 2;

                PDFLayout(page);
            }
            
            return Result.Success;
        }




        /// <summary>
        /// Hides all layers aside from the one needed
        /// </summary>
        /// <param name="pdfData"></param>
        public void HideLayers(PDF pdfData)
        {
            var lt = new LayerTools(pdfData.doc);
            var refLayer = lt.CreateLayer("REF");
            pdfData.doc.Layers.SetCurrentLayerIndex(refLayer.Index, true);
            pdfData.layer.IsVisible = true;

            foreach(var l in lt.getAllParentLayers())
            {
                if (l != pdfData.layer && l != refLayer)
                    l.IsVisible = false;
            }
        }

        /// <summary>
        /// Does what it says
        /// </summary>
        /// <param name="doc"></param>
        public void ShowAllLayers(RhinoDoc doc)
        {
            var lt = new LayerTools(doc);
            var parents = lt.getAllParentLayers();
            parents[parents.Count - 1].IsVisible = true;
            doc.Layers.SetCurrentLayerIndex(parents[0].Index, true);
            foreach (var l in parents)
            {
                l.IsVisible = true;
                if (l.Name == "REF")
                    doc.Layers.Delete(l);
            }
        }

        /// <summary>
        /// Finds the job path in rhino Stickies
        /// </summary>
        /// <returns></returns>
        public string PrototypePath(RhinoDoc doc)
        {
            if (doc.Name != "")
            {
                var sql = new SQLTools();
                var ind = new List<int>();
                    ind.Add(1);
                DataStore jobSlot = sql.queryDataStore(ind)[0];
                string jobNumber = sql.queryJobSlots()[jobSlot.intValue - 1].job;
                return doc.Path.Replace(doc.Name, "") + jobNumber + "\\NESTINGS\\";
            }
            return null;
        }

        /// <summary>
        /// Send out the DXF file
        /// </summary>
        /// <param name="fullPath"></param>
        public void MakeDXF(string fullPath)
        {
            RhinoApp.RunScript("_-Export \"" + fullPath + "\" Scheme \"Vomela\" _Enter", false);
        }

        public RhinoView CreateViewport(RhinoDoc doc, int width = 1100, int height = 850)
        {
            var rect = new System.Drawing.Rectangle(30, 30, width, height);
            return doc.Views.Add("PDFExport", DefinedViewportProjection.Top, rect, true);
        }

        /// <summary>
        /// Sends out PDF from viewport objects
        /// </summary>
        /// <param name="pdfData"></param>
        public void PDFViewport(PDF pdfData, RhinoView view)
        {
            // prep the zoom box
            pdfData = LayerBounding(pdfData);
            if (pdfData.obj.Count == 0)
                return;

            ClearPath(pdfData);
            HideLayers(pdfData);

            // do the proper zooming
            view.MainViewport.ZoomBoundingBox(pdfData.bb);

            // Construct the PDF page
            var page = Rhino.FileIO.FilePdf.Create();
            var capture = new ViewCaptureSettings(
                view, 
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
            ClearPath(pdfdata);

            // time to get the layout
            var view = pdfdata.doc.Views.GetPageViews();
            RhinoPageView layout = view[0];
            for (int i = 0; i < view.Length; i++)
                if (view[i].MainViewport.Name == pdfdata.layoutName)
                    layout = view[i];

            var page = Rhino.FileIO.FilePdf.Create();
            var capture = new ViewCaptureSettings(
                layout,
                new System.Drawing.Size((int)(layout.PageWidth * pdfdata.dpi), (int)(layout.PageHeight * pdfdata.dpi)),
                pdfdata.dpi
            );
            capture.OutputColor = pdfdata.colorMode;
            page.AddPage(capture);
            page.Write(pdfdata.path + pdfdata.pdfName + ".pdf");
        }

        /// <summary>
        /// Makes a multi-page PDF file
        /// </summary>
        /// <param name="pdfDatas"></param>
        public void PDFMultiPage(List<PDF> pdfDatas, RhinoView view)
        {
            ClearPath(pdfDatas[0]);
            var page = Rhino.FileIO.FilePdf.Create();

            // start the page loop
            foreach (var p in pdfDatas)
            {
                // prep the zoom box
                PDF pdfData = LayerBounding(p);
                if (pdfData.obj.Count == 0)
                    continue;

                HideLayers(pdfData);

                // do the proper zooming
                view.MainViewport.ZoomBoundingBox(pdfData.bb);

                // Construct the PDF page
                var capture = new ViewCaptureSettings(
                    view,
                    new System.Drawing.Size((int)pdfData.sheetSize[0] * pdfData.dpi, (int)pdfData.sheetSize[1] * pdfData.dpi),
                    pdfData.dpi
                );
                capture.OutputColor = pdfData.colorMode;
                page.AddPage(capture);
            }
            
            page.Write(pdfDatas[0].path + pdfDatas[0].pdfName + ".pdf");
        }

        /// <summary>
        /// Checks if the file path is created or creates it
        /// </summary>
        /// <param name="pdfData"></param>
        public void ClearPath(PDF pdfData)
        {
            if (!System.IO.Directory.Exists(pdfData.path))
                // see if the folder exists or create it
                System.IO.Directory.CreateDirectory(pdfData.path);
            else
                // directory exists, see if the pdf does, then delete
                if (System.IO.File.Exists(pdfData.path + pdfData.pdfName + ".pdf"))
                System.IO.File.Delete(pdfData.path + pdfData.pdfName + ".pdf");
        }

        /// <summary>
        /// Converts the viewport zoom to fit PDF Page
        /// </summary>
        /// <param name="pdfData"></param>
        /// <returns>modified PDF object</returns>
        public PDF LayerBounding(PDF pdfData)
        {
            var objLayers = new List<Layer>();
            if (pdfData.layer.GetChildren() != null)
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
            pdfData.bb = pdfData.obj[0].Geometry.GetBoundingBox(true);
            foreach( var o in pdfData.obj)
                pdfData.bb.Union(o.Geometry.GetBoundingBox(true));
            
            // make it a smidge bigger
            //pdfData.bb.Inflate(pdfData.bb.GetEdges()[1].Length * 0.01);
            return pdfData;
        }
    }
}