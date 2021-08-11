using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.UI;
using Rhino.DocObjects;
using Rhino.Display;
using Rhino.Geometry;
using Eto;


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
        dpi = 400;
        sheetSize = new List<double> { 11.0, 8.5 };
        PDFcolorMode = 0;
        CADFileName = parent.Name;

        makeDwg = true;
        makeDxf = false;
        IsLayout = false;

        parentLay = parent;
        doc = document;
        view = document.Views.Find("Top", true);
    }

    private List<Layer> GetSubLayers()
    {
        var lays = new List<Layer> { parentLay };
        if (parentLay.GetChildren() != null)
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
            bb.Inflate(bb.GetEdges()[0].Length * 0.02);
            return bb;
        }
    }
    public BoundingBox CutObjBounding
    {
        get
        {
            RhinoObject.GetTightBoundingBox(GetCutLayerObjs(), out BoundingBox bb);
            bb.Inflate(bb.GetEdges()[0].Length * 0.02);
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
            return _colorMode[PDFcolorMode];
        }
    }
}

namespace gjTools.Commands
{
    [CommandStyle(Rhino.Commands.Style.ScriptRunner)]
    public class EPDF_Export : Command
    {
        public EPDF_Export()
        {
            Instance = this;
        }

        ///<summary>Makes my PDF Files</summary>
        public static EPDF_Export Instance { get; private set; }

        public override string EnglishName => "PDFExport";
        public Eto.Drawing.Point PDFwindowPosition = Eto.Drawing.Point.Empty;

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            if (PDFwindowPosition == Eto.Drawing.Point.Empty)
                PDFwindowPosition = new Eto.Drawing.Point((int)MouseCursor.Location.X - 250, 200);

            int currentLayer = doc.Layers.CurrentLayerIndex;
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

            var noWorkingPathOptions = new List<string>(outTypes);
            if (doc.Path == null)
                noWorkingPathOptions.RemoveRange(5, 4);
            
            // get page views
            var views = doc.Views.GetPageViews();
            var viewStrings = new List<string>();
            foreach (var v in views)
                viewStrings.Add(v.MainViewport.Name);

            // present the dialog for input
            var pdfDialog = new DualListDialog("PDF Exporter", "Output Type", noWorkingPathOptions, "Layers/Layout Select", lt.getAllParentLayersStrings())
            {
                windowPosition = PDFwindowPosition,
                singleDefaultIndex = 0,
                multiSelectAlternate = viewStrings
            };
            pdfDialog.ShowForm();
            PDFwindowPosition = pdfDialog.windowPosition;

            if (pdfDialog.CommandResult() != Eto.Forms.DialogResult.Ok)
                return Result.Cancel;

            // Get user data
            var outType = pdfDialog.GetSingleValue();
            var PDFNames = pdfDialog.GetMultiSelectValue();
            
            // See if layers need to be chosen
            if (outType == outTypes[0] || outType == outTypes[1] || outType == outTypes[4] || outType == outTypes[7] || outType == outTypes[8])
                foreach(var p in PDFNames)
                    pdfData.Add( DeterminePath(new ePDF(doc, doc.Layers[doc.Layers.FindByFullPath(p, 0)]), outType, outTypes, sql) );
            
            // See if page layouts need to be chosen
            if (outType == outTypes[2] || outType == outTypes[3])
            {
                foreach (var p in pdfDialog.GetMultiSelectAlternateValue())
                {
                    var page = new ePDF(doc, doc.Layers[0]);
                        page.IsLayout = true;
                        page.makeDwg = false;
                        page.view = views[viewStrings.IndexOf(p)];
                        page.pdfName = page.view.MainViewport.Name;
                    page = DeterminePath(page, outType, outTypes, sql);
                    pdfData.Add(page);
                }
            }

            // EPExport to be completed with a function
            if (outType == outTypes[6])
                pdfData = EPExport(doc, lt, sql);

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
                var activeView = doc.Views.Find("Top", true);
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

            // Make a CAD file?
            foreach (var p in pdfData)
            {
                doc.Objects.UnselectAll();
                if (p.makeDxf)
                {
                    p.SelectObjects(true);
                    MakeDWG(p.path + p.CADFileName + ".dxf");
                }
                if (p.makeDwg && outType != outTypes[4])
                {
                    p.SelectObjects(false);
                    MakeDWG(p.path + p.CADFileName + ".dwg");
                    doc.ExportSelected(p.path + p.pdfName + ".3dm");
                }
            }

            doc.Layers.SetCurrentLayerIndex(currentLayer, true);
            doc.Views.ActiveView.MainViewport.ZoomExtents();
            return Result.Success;
        }



        /// <summary>
        /// assembles the plot files in a specific order
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="lt"></param>
        /// <param name="sql"></param>
        /// <returns></returns>
        public List<ePDF> EPExport(RhinoDoc doc, LayerTools lt, SQLTools sql)
        {
            var pdfData = new List<ePDF>();
            var cut = new List<ePDF>();
            var mylar = new List<ePDF>();
            var fn = doc.Name.Substring(0, doc.Name.Length - 4);
            var path = sql.queryLocations()[5].path + fn + "\\";
            int pNo = 2;

            foreach (var l in lt.getAllParentLayers())
            {
                if (l.Name == "Title Block")
                    pdfData.Add(new ePDF(doc, l) {
                        makeDwg = false,
                        path = path,
                        pdfName = fn + "_Page 1",
                        dpi = 150
                    });
            }
            foreach (var l in lt.getAllParentLayers())
            { 
                if (l.Name.Contains("MYLAR"))
                {
                    mylar.Add(new ePDF(doc, l)
                    {
                        makeDwg = false,
                        path = path,
                        pdfName = fn + "_Page " + pNo,
                        dpi = 150
                    });
                    pNo++;
                }
            }
            foreach (var l in lt.getAllParentLayers())
            {
                if (l.Name.Contains("CUT"))
                {
                    cut.Add(new ePDF(doc, l)
                    {
                        makeDwg = false,
                        makeDxf = true,
                        CADFileName = fn.Substring(0, fn.Length - 1) + "_" + l.Name,
                        path = path,
                        pdfName = fn + "_Page " + pNo,
                        dpi = 150
                    });
                    pNo++;
                }
            }
            foreach(var v in doc.Views.GetPageViews())
            {
                pdfData.Add(new ePDF(doc, doc.Layers[0]) { 
                    makeDwg = false,
                    path = path,
                    pdfName = fn.Substring(0, fn.Length - 1) + "_" + v.MainViewport.Name,
                    IsLayout = true,
                    view = v,
                    dpi = 72
                });
            }
            pdfData.AddRange(mylar);
            pdfData.AddRange(cut);
            MakeEpXML(doc, path, fn);
            return pdfData;
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
            var CutLay = doc.Layers.FindByFullPath("CUT", -1);
            var allLays = doc.Layers[CutLay].GetChildren();
            var nestLay = doc.Layers.FindByFullPath("CUT::NestBox", -1);

            if (CutLay == -1 || nestLay == -1 || allLays == null)
                return false;

            var nestBox = doc.Objects.FindByLayer(doc.Layers[nestLay])[0];
            var bb = nestBox.Geometry.GetBoundingBox(true).GetEdges();
            var obj = new List<RhinoObject>();
            foreach(var l in allLays)
            {
                var o = doc.Objects.FindByLayer(l);
                if (o.Length > 0 && l.Name.Substring(0,2) == "C_")
                    obj.AddRange(o);
            }
            var cuts = new CutSort(obj);
            var qty = (cuts.groupCount > 0) ? cuts.groupCount : cuts.obj.Count;

            string xml = "<JDF>\n" +
                $"<DrawingNumber>{fileName}</DrawingNumber>\n" +
                $"<CADSheetWidth>{bb[0].Length}</CADSheetWidth>\n" +
                $"<CADSheetHeight>{bb[1].Length}</CADSheetHeight>\n" +
                $"<CADNumberUp>{qty}</CADNumberUp>\n" +
                "</JDF>";

            if (!System.IO.Directory.Exists(path))
                System.IO.Directory.CreateDirectory(path);
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
                if (l.Name == "NestBoxes")
                    continue;
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
            var workingLocation = (page.doc.Path != null) ? page.doc.Path.Replace(page.doc.Name, "") : dbLocation[3].path;

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
                new System.Drawing.Size((int)(pdfData.sheetSize[0] * pdfData.dpi), (int)(pdfData.sheetSize[1] * pdfData.dpi)),
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
                var capture = new ViewCaptureSettings(
                    p.view,
                    new System.Drawing.Size((int)(p.sheetSize[0] * p.dpi), (int)(p.sheetSize[1] * p.dpi)),
                    p.dpi
                )
                {
                    OutputColor = p.colorMode
                };
                // Construct the PDF page
                page.AddPage(capture);
            }

            var pdfName = pdfDatas[0];
            if (pdfDatas[0].doc.Name != null)
                pdfName.pdfName = pdfDatas[0].doc.Name.Replace(".3dm", "");

            page.Write(pdfName.path + pdfName.pdfName + ".pdf");
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