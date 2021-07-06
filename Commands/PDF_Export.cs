using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.UI;
using Rhino.DocObjects;
using Rhino.Display;
using Rhino.Geometry;


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
    public bool multiPage;
    public bool makeCADFile;
    public string CADFileName;

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
        multiPage = false;
        makeCADFile = true; // make one by default
        CADFileName = pdfName + ".dwg";

        layer = document.Layers[0];
        doc = document;

        obj = new List<RhinoObject>();
        bb = new BoundingBox();
    }
    private List<Layer> GetSubLayers()
    {
        var lays = new List<Layer> { layer };
        if (layer.GetChildren().Length > 0)
            lays.AddRange(layer.GetChildren());
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
            if (outputColor < 3)
                return _colorMode[outputColor];
            else
                return _colorMode[0];
        }
    }
    public List<RhinoObject> OnlyCutLayerObjects
    {
        get
        {
            var cl = layer.GetChildren();
            var cutObj = new List<RhinoObject>();
            var ind = new List<int>();
            if (cl.Length == 0)
                return cutObj;

            foreach (var lay in cl)
                if (lay.Name.Substring(0, 2) == "C_")
                    ind.Add(lay.Index);

            var ss = new ObjectEnumeratorSettings { ObjectTypeFilter = ObjectType.Curve | ObjectType.Annotation };

            for (int i = 0; i < ind.Count; i++)
            {
                ss.LayerIndexFilter = ind[i];
                cutObj.AddRange(doc.Objects.GetObjectList(ss));
            }
            
            return cutObj;
        }
    }
}


namespace gjTools.Commands
{
    [CommandStyle(Style.ScriptRunner)]
    public class PDF_Export : Command
    {
        public PDF_Export()
        {
            Instance = this;
        }

        ///<summary>Makes my PDF Files</summary>
        public static PDF_Export Instance { get; private set; }

        public override string EnglishName => "gjPDFExport";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var sql = new SQLTools().queryLocations();
            var lt = new LayerTools(doc);
            var pdfData = new List<PDF>();
            var pdfLayout = new List<PDF>();

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
                            page.multiPage = true;
                            page.makeCADFile = false;
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

                    pdfData.Add(page);
                }
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

                pdfLayout.Add(page);
            }
            else if (outType == "EPExport")
            {
                // output the New E&P within rhino
                var cutLayers = new List<Layer>();
                var mylarLayers = new List<Layer>();
                var mylarLayouts = new List<RhinoView>();
                var fileName = doc.Name.Substring(0, doc.Name.Length - 4);
                var path = sql[5].path + fileName + "\\";
                var parentLayers = lt.getAllParentLayers();
                
                foreach (Layer l in parentLayers)
                {
                    if (l.Name.Contains("CUT"))
                        cutLayers.Add(l);
                    if (l.Name.Contains("MYLAR"))
                        mylarLayers.Add(l);
                    if (l.Name == "Title Block")
                    {
                        var pdf = new PDF(doc);
                        pdf.layer = l;
                        pdf.obj.AddRange(doc.Objects.FindByLayer(l));
                        pdf.path = path;
                        pdf.pdfName = fileName + "_Page 1";
                        pdf.makeCADFile = false;

                        pdfData.Add(pdf);
                    }
                }

                // to maintain order of pdf files
                foreach(Layer l in mylarLayers)
                {
                    int pageNumber = pdfData.Count + 1;
                    var pdf = new PDF(doc);
                    pdf.layer = l;
                    pdf.pdfName = fileName + "_Page " + pageNumber;
                    pdf.path = path;
                    pdf.makeCADFile = false;
                    pdf.obj.AddRange(doc.Objects.FindByLayer(l));

                    pdfData.Add(pdf);
                }
                foreach (Layer l in cutLayers)
                {
                    int pageNumber = pdfData.Count + 1;
                    var pdf = new PDF(doc);
                    pdf.layer = l;
                    pdf.pdfName = fileName + "_Page " + pageNumber;
                    pdf.path = path;
                    pdf.CADFileName = fileName.Substring(0, fileName.Length - 1) + "_" + l.Name + ".dxf";
                    pdf.obj.AddRange(doc.Objects.FindByLayer(l));

                    pdfData.Add(pdf);
                }

                foreach (RhinoView layout in doc.Views.GetPageViews())
                    if (layout.MainViewport.Name.Contains("MYLAR"))
                    {
                        var pdf = new PDF(doc);
                        pdf.layoutName = layout.MainViewport.Name;
                        pdf.pdfName = fileName.Substring(0, fileName.Length - 1) + "_" + pdf.layoutName;
                        pdf.path = path;
                        pdf.outputColor = 2;

                        pdfLayout.Add(pdf);
                    }

                ClearPath(pdfData[0]);
                ShowAllLayers(doc);
                doc.Export(path + fileName + ".3dm");
                MakeEpXML(doc, path, fileName);
            }


            // Make the pdf files
            if (pdfData.Count > 0)
            {
                RhinoView currentView = doc.Views.ActiveView;
                RhinoView floatView = CreateViewport(doc);

                if (pdfData[0].multiPage)
                    PDFMultiPage(pdfData, floatView);
                else
                    foreach (var pdf in pdfData)
                    {
                        PDFViewport(pdf, floatView);
                        if (pdf.makeCADFile)
                        {
                            doc.Objects.UnselectAll();
                            var selOb = pdf.obj;
                            if (pdf.CADFileName.Contains(".dxf"))
                                selOb = pdf.OnlyCutLayerObjects;

                            foreach (var o in selOb)
                                doc.Objects.Select(o.Id);

                            MakeDWG(pdf.path + pdf.CADFileName);
                            //doc.ExportSelected(pdf.path + pdf.pdfName + ".3dm");
                        }
                    }

                // delete the viewport and reset
                floatView.Close();
                doc.Views.ActiveView = currentView;
                currentView.Maximized = true;

                // Set layers back
                ShowAllLayers(doc);
                doc.Views.Redraw();
            }
            // Make layouts if they exist
            if (pdfLayout.Count > 0)
            {
                RhinoApp.WriteLine("Writing {0} Mylar Files, please Wait..", pdfLayout.Count);
                foreach (PDF pdf in pdfLayout)
                {
                    PDFLayout(pdf);
                    RhinoApp.WriteLine("Created: {0}.pdf", pdf.pdfName);
                }
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
        public void MakeDWG(string fullPath)
        {
            RhinoApp.RunScript("_-Export \"" + fullPath + "\" Scheme \"Vomela\" _Enter", false);
        }

        /// <summary>
        /// New floating viewport
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
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