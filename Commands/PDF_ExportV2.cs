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
    /// <summary>
    /// Holds all of the data needed to make a PDF file
    /// </summary>
    public struct ePDF
    {
        public Layer parentLay;
        public RhinoDoc doc;
        public RhinoView view;

        public string pdfName;
        public string path;

        // if the cad filename differs from PDF
        public bool makeDxf;
        public bool makeDwg;
        public bool IsLayout;
        public string CADFileName;

        public List<double> sheetSize;
        public int PDFcolorMode;
        public int dpi;

        public ePDF(RhinoDoc document, Layer parent)
        {
            pdfName = parent.Name;
            path = "";
            dpi = 600;
            sheetSize = new List<double> { 11.0, 8.5 };
            PDFcolorMode = 0;
            CADFileName = parent.Name;

            makeDwg = true;
            makeDxf = false;
            IsLayout = false;

            parentLay = parent;
            doc = document;
            view = document.Views.ActiveView;
        }

        private List<Layer> GetSubLayers()
        {
            var lays = new List<Layer> { parentLay };
            if (parentLay.GetChildren().Length > 0)
                lays.AddRange(parentLay.GetChildren());
            return lays;
        }
        private List<RhinoObject> GetLayerObjs()
        {
            var ss = new ObjectEnumeratorSettings();
            var objs = new List<RhinoObject>();
            foreach (var l in GetSubLayers())
            {
                ss.LayerIndexFilter = l.Index;
                objs.AddRange(doc.Objects.GetObjectList(ss));
            }
            return objs;
        }
        private List<RhinoObject> GetCutLayerObjs()
        {
            var ss = new ObjectEnumeratorSettings();
            var objs = new List<RhinoObject>();
            foreach (var l in GetSubLayers())
            {
                if (l.Name.Contains("C_"))
                {
                    ss.LayerIndexFilter = l.Index;
                    objs.AddRange(doc.Objects.GetObjectList(ss));
                }
            }
            return objs;
        }
        public void SelectObjects(bool cutLinesOnly = true)
        {
            var ids = new List<Guid>();
            if (cutLinesOnly)
                foreach (var o in GetCutLayerObjs())
                    ids.Add(o.Id);
            else
                foreach (var o in GetLayerObjs())
                    ids.Add(o.Id);
            doc.Objects.Select(ids);
        }
        public BoundingBox AllObjBounding
        {
            get
            {
                RhinoObject.GetTightBoundingBox(GetLayerObjs(), out BoundingBox bb);
                return bb;
            }
        }
        public BoundingBox CutObjBounding
        {
            get
            {
                RhinoObject.GetTightBoundingBox(GetCutLayerObjs(), out BoundingBox bb);
                return bb;
            }
        }
        public ViewCaptureSettings.ColorMode colorMode
        {
            get
            {
                var _colorMode = new List<ViewCaptureSettings.ColorMode> {
                    ViewCaptureSettings.ColorMode.DisplayColor,
                    ViewCaptureSettings.ColorMode.PrintColor,
                    ViewCaptureSettings.ColorMode.BlackAndWhite
                };
                if (PDFcolorMode < 3)
                    return _colorMode[PDFcolorMode];
                else
                    return _colorMode[0];
            }
        }
    }

    [CommandStyle(Style.ScriptRunner)]
    public class ePDF_Export : Command
    {
        public ePDF_Export()
        {
            Instance = this;
        }

        ///<summary>Makes my PDF Files</summary>
        public static ePDF_Export Instance { get; private set; }

        public override string EnglishName => "gjePDFExport";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var sql = new SQLTools();
            var lt = new LayerTools(doc);
            var pdfData = new List<ePDF>();

            var outTypes = new List<string>
            {
                "LocalTemp",        // 0
                "MeasuredDrawing",  // 1
                "Mylar Color",      // 2
                "Mylar NonColor",   // 3
                "MultiPagePDF",     // 4
                "EPExportLegacy",   // 5
                "EPExport",         // 6
                "ProtoNestings",    // 7
                "WorkingLocation"   // 8
            };

            var noWorkingPathOptions = outTypes;
            if (doc.Path.Length < 4)
                noWorkingPathOptions.RemoveRange(5, 4);

            // Get user data
            var outType = (string)Dialogs.ShowListBox("PDF Output", "Choose a Type", noWorkingPathOptions);
            if (outType == null || (outType == "ProtoNestings" && doc.Path == ""))
                return Result.Cancel;
            
            // See if layers need to be chosen
            if (outType == outTypes[0] || outType == outTypes[1] || outType == outTypes[4] || outType == outTypes[7] || outType == outTypes[8])
            {
                var PDFLayers = Dialogs.ShowMultiListBox("PDF Export", "Choose Layers", lt.getAllParentLayersStrings(), new List<string> { doc.Layers.CurrentLayer.Name });
                if (PDFLayers == null)
                    return Result.Cancel;

                foreach(var p in PDFLayers)
                    pdfData.Add( DeterminePath(new ePDF(doc, doc.Layers[doc.Layers.FindByFullPath(p, 0)]), outType, outTypes, sql) );
            }
            
            // See if page layouts need to be chosen
            if (outType == outTypes[2] || outType == outTypes[2])
            {
                var views = doc.Views.GetPageViews();
                var viewStrings = new List<string>();
                foreach(var v in views)
                    viewStrings.Add(v.MainViewport.Name);

                var PDFNames = Dialogs.ShowMultiListBox("PDF Export", "Choose Layouts", viewStrings);
                if (PDFNames == null)
                    return Result.Cancel;

                foreach (var p in PDFNames)
                {
                    var page = new ePDF(doc, doc.Layers[0]);
                        page.IsLayout = true;
                        page.makeDwg = false;
                        page.view = views[viewStrings.IndexOf(p)];
                    page = DeterminePath(page, outType, outTypes, sql);
                    pdfData.Add(page);
                }
            }

            // EPExport to be completed with a function
            // pdfData = EPExport();

            // Sort the output types
            var PDFviewPages = new List<ePDF>();
            var PDFlayoutPages = new List<ePDF>();
            foreach (var p in pdfData)
            {
                if (p.IsLayout)
                    PDFlayoutPages.Add(p);
                else
                    PDFviewPages.Add(p);
            }

            // Viewport PDF Maker
            if (PDFviewPages.Count > 0)
            {
                // change the viewport resolution
                var activeView = doc.Views.ActiveView;
                doc.Views.FourViewLayout(true);
                activeView.Size = new System.Drawing.Size(1100, 850);

                if (outType == outTypes[4])
                {
                    PDFMultiPage(PDFviewPages);
                }
                else
                {
                    foreach (var p in PDFviewPages)
                    {
                        HideLayers(p, lt);
                        PDFViewport(p);
                    }
                }

                ShowAllLayers(lt);
                activeView.Maximized = true;
            }
            
            // Layout PDF Maker
            if (PDFlayoutPages.Count > 0)
            {
                foreach (var p in PDFlayoutPages)
                    PDFLayout(p);
            }

            return Result.Success;
        }




        /// <summary>
        /// create the XML for E&P output
        /// </summary>
        /// <param name="cuts"></param>
        /// <param name="nestBox"></param>
        /// <param name="path"></param>
        /// <param name="fileName"></param>
        public bool MakeEpXML(RhinoDoc doc, string path, string fileName)
        {
            var lt = new LayerTools(doc);
            var cutLayers = lt.getAllCutLayers(lt.CreateLayer("CUT"), true);
            var obj = new List<RhinoObject>();
            RhinoObject nestBox;
            var ss = new ObjectEnumeratorSettings { ObjectTypeFilter = ObjectType.Curve };
            foreach(var l in cutLayers)
            {
                ss.LayerIndexFilter = l.Index;
                if (l.Name == "NestBox")
                    nestBox = doc.Objects.FindByLayer(l)[0];
                else
                    obj.AddRange(doc.Objects.GetObjectList(ss));
            }
            var cuts = new CutSort(obj);
            int count = (cuts.groupCount > 0) ? cuts.groupCount : cuts.obj.Count;

            RhinoObject.GetTightBoundingBox(cuts.GetRhinoObjects, out BoundingBox bb);

            string xml = string.Format("<JDF>\n" +
                "<DrawingNumber>{0}</DrawingNumber>\n" +
                "<CADSheetWidth>{2}</CADSheetWidth>\n" +
                "<CADSheetHeight>{3}</CADSheetHeight>\n" +
                "<CADNumberUp>{1}</CADNumberUp>\n" +
                "</JDF>", fileName, count, Math.Round(bb.GetEdges()[0].Length, 2), Math.Round(bb.GetEdges()[1].Length), 2);

            System.IO.File.WriteAllText(path + fileName + ".xml", xml);
            return true;
        }

        /// <summary>
        /// Hides all layers aside from the one needed
        /// </summary>
        /// <param name="pdfData"></param>
        public void HideLayers(ePDF pdfData, LayerTools lt)
        {
            pdfData.parentLay.IsVisible = true;
            pdfData.doc.Layers.SetCurrentLayerIndex(pdfData.parentLay.Index, true);

            foreach(var l in lt.getAllParentLayers())
                if (l != pdfData.parentLay)
                    l.IsVisible = false;
        }

        /// <summary>
        /// Does what it says
        /// </summary>
        /// <param name="doc"></param>
        public void ShowAllLayers(LayerTools lt)
        {
            var plays = lt.getAllParentLayers();
            foreach (var l in plays)
            {
                l.IsVisible = true;
                if (l == plays[0])
                    lt.doc.Layers.SetCurrentLayerIndex(l.Index, true);
            }
        }

        /// <summary>
        /// Determines the Path that the thing should be sent to
        /// </summary>
        /// <param name="page"></param>
        /// <param name="ot"></param>
        /// <param name="otVal"></param>
        /// <param name="sql"></param>
        /// <returns></returns>
        private ePDF DeterminePath(ePDF page, string ot, List<string> otVal, SQLTools sql)
        {
            var dbLocation = sql.queryLocations();
            var workingLocation = (page.doc.Path != "") ? page.doc.Path.Replace(page.doc.Name, "") : dbLocation[3].path;

            // Prototype path
            if (ot == otVal[7])
            {
                var ind = new List<int> { 1 };
                DataStore jobSlot = sql.queryDataStore(ind)[0];
                string jobNumber = sql.queryJobSlots()[jobSlot.intValue - 1].job;
                page.path = page.doc.Path.Replace(page.doc.Name, "") + jobNumber + "\\NESTINGS\\";
            }

            // Working path or Local Temp if not Saved
            if (ot == otVal[2] || ot == otVal[3] || ot == otVal[4] || ot == otVal[8])
                page.path = workingLocation;

            // LocalTemp
            if (ot == otVal[0]) 
                page.path = dbLocation[3].path;

            // Measured Drawing
            if (ot == otVal[1]) 
                page.path = dbLocation[4].path + page.pdfName + "\\";

            return page;
        }

        /// <summary>
        /// Send out the DXF file
        /// </summary>
        /// <param name="fullPath"></param>
        public void MakeDWG(string fullPath)
        {
            RhinoApp.RunScript("_-Export \"" + fullPath + "\" Scheme \"Vomela\" _Enter", false);
        }

        /// <summary>
        /// Sends out PDF from viewport objects
        /// </summary>
        /// <param name="pdfData"></param>
        public void PDFViewport(ePDF pdfData)
        {
            ClearPath(pdfData);

            // do the proper zooming
            pdfData.view.MainViewport.ZoomBoundingBox(pdfData.AllObjBounding);

            // Construct the PDF page
            var page = Rhino.FileIO.FilePdf.Create();
            var capture = new ViewCaptureSettings(
                pdfData.view, 
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
        public void PDFLayout(ePDF pdfdata)
        {
            ClearPath(pdfdata);
            var layout = (RhinoPageView)pdfdata.view;

            var page = Rhino.FileIO.FilePdf.Create();
            var capture = new ViewCaptureSettings(
                pdfdata.view,
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
        public void PDFMultiPage(List<ePDF> pdfDatas)
        {
            ClearPath(pdfDatas[0]);
            var page = Rhino.FileIO.FilePdf.Create();

            // start the page loop
            foreach (var p in pdfDatas)
            {
                HideLayers(p, new LayerTools(pdfDatas[0].doc));

                // do the proper zooming
                p.view.MainViewport.ZoomBoundingBox(p.AllObjBounding);

                // Construct the PDF page
                var capture = new ViewCaptureSettings(
                    p.view,
                    new System.Drawing.Size((int)p.sheetSize[0] * p.dpi, (int)p.sheetSize[1] * p.dpi),
                    p.dpi
                );
                capture.OutputColor = p.colorMode;
                page.AddPage(capture);
            }
            
            page.Write(pdfDatas[0].path + pdfDatas[0].pdfName + ".pdf");
        }

        /// <summary>
        /// Checks if the file path is created or creates it
        /// </summary>
        /// <param name="pdfData"></param>
        public void ClearPath(ePDF pdfData)
        {
            if (!System.IO.Directory.Exists(pdfData.path))
                // see if the folder exists or create it
                System.IO.Directory.CreateDirectory(pdfData.path);
            else
                // directory exists, see if the pdf does, then delete
                if (System.IO.File.Exists(pdfData.path + pdfData.pdfName + ".pdf"))
                System.IO.File.Delete(pdfData.path + pdfData.pdfName + ".pdf");
        }
    }
}