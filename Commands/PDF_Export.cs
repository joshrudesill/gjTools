using System;
using System.Threading;
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

        ///<summary>The only instance of the MyCommand command.</summary>
        public static PDF_Export Instance { get; private set; }

        public override string EnglishName => "PDFExport";

        public Eto.Drawing.Point PDFwindowPosition = Eto.Drawing.Point.Empty;

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            if (PDFwindowPosition == Eto.Drawing.Point.Empty)
                PDFwindowPosition = new Eto.Drawing.Point((int)MouseCursor.Location.X - 250, 200);

            var win = new PDF.PDF_Dialog(doc, PDFwindowPosition);

            var res = win.ShowForm();
            if (res == Eto.Forms.DialogResult.Cancel)
                return Result.Cancel;

            // Make the PDF Classes
            var PDF = new PDF.PDFJob(doc, win.SelectedColorMode, 400);
            var PDF_Page = new PDF.PDFJob(doc, win.SelectedColorMode, 600);

            // Get the Path
            var path = FileLocations.Paths(doc)[win.SelectedLocation];

            // What type is the PDF
            if (win.SelectedLocation == 4)
            {
                //  -------------------------------------------------------------------------------  EP Output
                var fn = doc.Name.Replace(".3dm", "");
                path = $"{path}{fn}\\";

                // Status bar to show progress
                StatusBar.ShowProgressMeter(0, win.EPItems.Count + win.EPMylar.Count, "Progress: ", false, true);

                // Single Pages, should already be in order
                for (int i = 0; i < win.EPItems.Count; i++)
                {
                    var name = $"{fn}_Page {i + 1}";

                    StatusBar.SetMessagePane($"Sending {name}.pdf");
                    PDF.MakeSinglePagePDF(path, name, win.LayerList[win.EPItems[i]]);
                    StatusBar.UpdateProgressMeter(1, false);
                }
                // Layouts of Mylars
                for (int i = 0; i < win.EPMylar.Count; i++)
                {
                    var layName = win.LayoutList[win.EPMylar[i]].PageName;
                    var name = $"{fn.Substring(0, fn.Length - 1)}_{layName}";

                    StatusBar.SetMessagePane($"Sending {name}");
                    PDF.MakeLayoutPDF(path, name, layName);
                    StatusBar.UpdateProgressMeter(1, false);
                }
            }
            else if (win.SelectedLocation == 3)
            {
                //  -------------------------------------------------------------------------------  Prototype Output
                // Proto Path
                path = FileLocations.Paths(doc)[3];

                // Status Bar
                StatusBar.ShowProgressMeter(0, win.SelectedItems.Count * 2, "Progress: ", false, true);

                for (int i = 0; i < win.SelectedItems.Count; i++)
                {
                    var l = win.SelectedLayerList[i];

                    StatusBar.SetMessagePane($"Saving {l.Name}.pdf");
                    PDF.MakeSinglePagePDF(path + "NESTINGS\\", l.Name, l);
                    StatusBar.UpdateProgressMeter(1, false);

                    MakeCAD_File(doc, path, l.Name);
                    StatusBar.UpdateProgressMeter(1, false);
                }
            }
            else if (win.SelectedType == 0)
            {
                //  -------------------------------------------------------------------------------  Single Pages output
                // Status bar
                StatusBar.ShowProgressMeter(0, win.SelectedItems.Count * 3, "Progress: ", false, true);

                for (var i = 0; i < win.SelectedItems.Count; i++)
                {
                    var name = win.LayerList[win.SelectedItems[i]].Name;
                    var modPath = path;

                    // If measured drawing, nest the path
                    if (win.SelectedLocation == 2)
                        modPath += name + "\\";

                    StatusBar.SetMessagePane($"Saving: {name}");
                    PDF.MakeSinglePagePDF(modPath, name, win.LayerList[win.SelectedItems[i]]);
                    StatusBar.UpdateProgressMeter(1, false);

                    // Working Files
                    MakeCAD_File(doc, modPath, name);
                }
            }
            else if (win.SelectedType == 1)
            {
                //  -------------------------------------------------------------------------------  Multi-Page Output
                PDF.MakeMultiPagePDF(path, win.PDF_Name, win.SelectedLayerList);
            }
            else if (win.SelectedType == 2)
            {
                //  -------------------------------------------------------------------------------  Paper Space layout output
                // Statusbar
                StatusBar.ShowProgressMeter(0, win.SelectedItems.Count, "Progress:", false, true);

                for (int i = 0; i < win.SelectedItems.Count; i++)
                {
                    var name = win.LayoutList[win.SelectedItems[i]];

                    StatusBar.SetMessagePane($"Saving: {name.PageName}");
                    PDF_Page.MakeLayoutPDF(path, name.PageName, name.PageName);
                    StatusBar.UpdateProgressMeter(1, false);
                }
            }

            // Clear the statusbar
            StatusBar.ClearMessagePane();
            StatusBar.HideProgressMeter();

            // Do we want the location open?
            if (res == Eto.Forms.DialogResult.Yes)
                System.Diagnostics.Process.Start("explorer.exe", path);

            return Result.Success;
        }



        /// <summary>
        /// Send out the DWG and 3DM file
        /// </summary>
        /// <param name="fullPath">No Extension in this</param>
        public void MakeCAD_File(RhinoDoc doc, string Path, string FileName, string ext = "dwg")
        {
            if (ext == "dwg")
            {
                // No need for a Rhino file if not dwg
                doc.ExportSelected($"{Path}{FileName}.3dm");
                StatusBar.UpdateProgressMeter(1, false);
            }

            RhinoApp.RunScript($"_-Export \"{Path}{FileName}.{ext}\" Scheme \"Vomela\" _Enter", false);
            StatusBar.UpdateProgressMeter(1, false);
        }
    }



    public class FileLocations
    {
        // Get the location Path
        public static Dictionary<string, string> PathDict = new Dictionary<string, string>(8)
        {
            { "Working Location", "" },
            { "Temp Location", @"D:\Temp\" },
            { "Measured Drawing", @"\\VWS\Cut\MEASURED_DRAWINGS\" },
            { "Prototype", "" },
            { "EP",  @"\\VWS\Cut\OEM-CUT\" },
            { "Default", @"\\VWS\Cut\TEMP-CUT\" },
            { "Router", @"\\VWS\Cut\ROUTER\" }
        };

        public static List<string> Names = new List<string>(8)
        {
            "Working Location",
            "Temp Location",
            "Measured Drawing",
            "Prototype",
            "EP",
            "Default",
            "Router"
        };

        public static List<string> Path = new List<string>(8)
        {
            "",
            @"D:\Temp\",
            @"\\VWS\Cut\MEASURED_DRAWINGS\",
            "",
            @"\\VWS\Cut\OEM-CUT\",
            @"\\VWS\Cut\TEMP-CUT\",
            @"\\VWS\Cut\ROUTER\"
        };

        public static List<string> Paths(RhinoDoc doc)
        {
            if (doc.Path != null)
                Path[0] = doc.Path.Replace(doc.Name, "");

            Path[3] = Path[0] + new SQLTools().queryJobSlots()[0].job + "\\";

            return Path;
        }
    }
}



namespace PDF
{
    using Eto.Drawing;
    using Eto.Forms;

    /// <summary>
    /// Custom Dialog for Creating Choosing PDF outPuts
    /// </summary>
    public class PDF_Dialog
    {
        //  -----------------------------------------------------------------------------  Access Controls
        private Dialog<DialogResult> Window;
        private Point WinLocation;

        private RadioButtonList OutputLocation;
        private RadioButtonList OutputType;
        private RadioButtonList ColorMode;
        private GridView PDFItems;
        private TextBox PDFName;
        private RhinoDoc doc;
        Button But_Open;
        Button But_Ok;

        private List<Layer> ParentLayers;
        private List<List<string>> ds_ParentLayers;
        private int CurrentLayerIndex;
        private List<RhinoPageView> PageViews;
        private List<List<string>> ds_PageViews;
        private List<int> EP_Layers;
        private List<int> EP_Pages;



        public PDF_Dialog(RhinoDoc Document, Point WindowLocation)
        {
            doc = Document;
            WinLocation = WindowLocation;
        }




        //  -----------------------------------------------------------------------------  Build Controls
        private void LoadControls()
        {
            Window = new Dialog<DialogResult>
            {
                Title = "PDF Export",
                Result = DialogResult.Cancel,
                AutoSize = true,
                Minimizable = false,
                Maximizable = false,
                Padding = new Padding(5),
                Resizable = false,
                Topmost = true,
                Location = WinLocation
            };

            But_Open = new Button { Text = "Open", ToolTip = "Ok and Open Location" };
            But_Ok = new Button { Text = "Ok" };
            Button But_Cancel = new Button { Text = "Cancel" };
            But_Open.Click += (s, e) => Window.Close(DialogResult.Yes);
            But_Ok.Click += (s, e) => Window.Close(DialogResult.Ok);
            But_Cancel.Click += (s, e) => Window.Close(DialogResult.Cancel);

            PDFItems = new GridView
            {
                Size = new Size(250, 300),
                ShowHeader = true,
                Border = BorderType.Line,
                GridLines = GridLines.None,
                AllowMultipleSelection = true,
                Columns = {
                    new GridColumn {
                        HeaderText = "Layer/Page to PDF",
                        Editable = false,
                        Expand = true,
                        DataCell = new TextBoxCell(0)
                    }
                }
            };

            int LocIndex = 0;
            if (doc.Path == null)
                LocIndex = 1;

            OutputLocation = new RadioButtonList
            {
                Items = { "Working Location", "Local Temp", "Measure Drawing", "Prototype", "E&P" },
                Orientation = Orientation.Vertical,
                SelectedIndex = LocIndex,
                Spacing = new Size(5, 5),
                ID = "Location"
            };

            OutputType = new RadioButtonList
            {
                Items = { "Single Page", "Multi-Page", "Layout Page" },
                SelectedIndex = 0,
                Orientation = Orientation.Vertical,
                Spacing = new Size(5, 5),
                ID = "Type"
            };

            ColorMode = new RadioButtonList
            {
                Items = { "Display Color", "Black/White" },
                SelectedIndex = 0,
                Orientation = Orientation.Vertical,
                Spacing = new Size(5, 5),
                ID = "Color"
            };

            var LocationGrp = new GroupBox
            {
                Text = "Location",
                Padding = new Padding(8),
                Content = new TableLayout { Rows = { new TableRow(OutputLocation) } }
            };

            var TypeGrp = new GroupBox
            {
                Text = "Type",
                Padding = new Padding(8),
                Content = new TableLayout { Rows = { new TableRow(OutputType) } }
            };

            var ColorGrp = new GroupBox
            {
                Text = "Colors",
                Padding = new Padding(8),
                Content = new TableLayout { Rows = { new TableRow(ColorMode) } }
            };

            PDFName = new TextBox { Enabled = false, PlaceholderText = "PDF FileName" };

            var NameGrp = new TableLayout
            {
                Padding = new Padding(5),
                Spacing = new Size(5, 5),
                Rows =
                {
                    new TableRow(LocationGrp),
                    new TableRow(TypeGrp),
                    new TableRow(ColorGrp),
                    new TableRow(PDFName)
                }
            };

            Window.Content = new TableLayout
            {
                Spacing = new Size(5, 5),
                Padding = new Padding(8),
                Rows =
                {
                    new TableRow(NameGrp, PDFItems),
                    new TableRow( null,
                        new TableLayout
                        {
                            Spacing = new Size(8, 5),
                            Rows = {
                                new TableRow(null, But_Open, But_Ok, But_Cancel)
                            }
                        } )
                }
            };

            //  -------------------------------------  Events
            OutputLocation.SelectedIndexChanged += Radio_SelectedIndexChanged;
            OutputType.SelectedIndexChanged += Radio_SelectedIndexChanged;
            PDFItems.SelectedItemsChanged += PDFItems_SelectedItemsChanged;
        }

        private void LoadLayers()
        {
            var parents = new List<Layer>();
            ds_ParentLayers = new List<List<string>>();
            ds_PageViews = new List<List<string>>();

            // EP List
            var TitleBlk = new List<int>(1);
            var CutPages = new List<int>(12);
            var MylarPrev = new List<int>(12);
            var Mylar = new List<int>(12);

            for (var i = 0; i < doc.Layers.Count; i++)
            {
                var l = doc.Layers[i];
                if (l.ParentLayerId == Guid.Empty && !l.IsDeleted && l.Name != "NestBoxes")
                {
                    parents.Add(l);
                    ds_ParentLayers.Add(new List<string> { l.Name });
                    if (l == doc.Layers.CurrentLayer)
                        CurrentLayerIndex = ds_ParentLayers.Count - 1;

                    // Collect E&P
                    if (l.Name == "Title Block")
                    {
                        TitleBlk.Add(ds_ParentLayers.Count - 1);
                    }
                    else if (l.Name.StartsWith("CUT"))
                    {
                        CutPages.Add(ds_ParentLayers.Count - 1);
                    }
                    else if (l.Name.StartsWith("MYLAR"))
                    {
                        MylarPrev.Add(ds_ParentLayers.Count - 1);
                    }
                }
            }

            var layouts = doc.Views.GetPageViews();
            PageViews = new List<RhinoPageView>();
            if (layouts.Length > 0)
            {
                for (int i = 0; i < layouts.Length; i++)
                {
                    var p = layouts[i];

                    PageViews.Add(p);
                    ds_PageViews.Add(new List<string> { p.PageName });

                    if (p.PageName.StartsWith("MYLAR"))
                        Mylar.Add(ds_PageViews.Count - 1);
                }
            }

            // Update the EP Layers
            EP_Layers = new List<int>(TitleBlk);
            EP_Layers.AddRange(MylarPrev);
            EP_Layers.AddRange(CutPages);
            EP_Pages = Mylar;

            ParentLayers = parents;
        }



        //  -----------------------------------------------------------------------------  Events
        private void Radio_SelectedIndexChanged(object sender, EventArgs e)
        {
            var rb = (RadioButtonList)sender;

            if (rb.ID == "Location")
            {
                if (doc.Path == null && rb.SelectedIndex == 0)
                {
                    rb.SelectedIndex = 1;
                    return;
                }
                if (rb.SelectedIndex == 4)
                {
                    OutputType.SelectedIndex = 0;
                    OutputType.Enabled = false;
                    PDFItems.Enabled = false;
                    PDFItems.SelectedRows = EP_Layers;
                    return;
                }

                // All others will have the controls enabled
                if (!OutputType.Enabled) OutputType.Enabled = true;
                if (!PDFItems.Enabled) PDFItems.Enabled = true;
            }
            if (rb.ID == "Type")
            {
                if (rb.SelectedIndex == 2)
                {
                    if (ds_PageViews.Count == 0)
                    {
                        rb.SelectedIndex = 0;
                    }
                    else
                    {
                        PDFItems.DataStore = ds_PageViews;
                        PDFItems.SelectedRow = 0;
                    }
                }
                else if (PDFItems.DataStore == ds_PageViews)
                {
                    PDFItems.DataStore = ds_ParentLayers;
                    PDFItems.SelectedRow = CurrentLayerIndex;
                }

                //  Control the PDF Name if it's a multipage
                if (rb.SelectedIndex == 1)
                {
                    PDFName.Enabled = true;
                    if (doc.Name != null)
                        PDFName.Text = doc.Name.Replace(".3dm", "");
                    else
                        PDFName.Text = "MultiPage";
                }
                else
                {
                    PDFName.Enabled = false;
                    PDFName.Text = "";
                }
                return;
            }
        }

        private void PDFItems_SelectedItemsChanged(object sender, EventArgs e)
        {
            var rows = new List<int>(PDFItems.SelectedRows);
            if (rows.Count > 0)
            {
                But_Ok.Enabled = true;
                But_Open.Enabled = true;
            }
            else
            {
                But_Ok.Enabled = false;
                But_Open.Enabled = false;
            }
        }




        //  -----------------------------------------------------------------------------  Show Window
        public DialogResult ShowForm()
        {
            LoadControls();

            var thr = new Thread(LoadLayers);
            thr.Start();
            thr.Join();
            PDFItems.DataStore = ds_ParentLayers;
            PDFItems.SelectedRow = CurrentLayerIndex;

            Window.ShowModal(RhinoEtoApp.MainWindow);
            WinLocation = Window.Location;
            return Window.Result;
        }





        //  -----------------------------------------------------------------------------  getters
        /// <summary>
        /// Selected Indexes
        /// </summary>
        public List<int> SelectedItems { get { return new List<int>(PDFItems.SelectedRows); } }
        /// <summary>
        /// Gives the order of a New-Style E&P File
        /// </summary>
        public List<int> EPItems { get { return EP_Layers; } }
        /// <summary>
        /// a List of the Mylars if any for an E&P
        /// </summary>
        public List<int> EPMylar { get { return EP_Pages; } }
        /// <summary>
        /// Parent layers matches the SelectedItems indexes
        /// </summary>
        public List<Layer> LayerList { get { return ParentLayers; } }
        /// <summary>
        /// Get a list of layers that were selected
        /// </summary>
        public List<Layer> SelectedLayerList
        {
            get
            {
                var ll = new List<Layer>();

                for (int i = 0; i < SelectedItems.Count; i++)
                {
                    ll.Add(ParentLayers[SelectedItems[i]]);
                }

                return ll;
            }
        }
        /// <summary>
        /// Page Views matches the SelectedItems indexes
        /// </summary>
        public List<RhinoPageView> LayoutList { get { return PageViews; } }
        /// <summary>
        /// Chosen Name (if Any)
        /// </summary>
        public string PDF_Name { get { return PDFName.Text; } }
        /// <summary>
        /// Selected ColorMode for the ouput PDF Files
        /// </summary>
        public ViewCaptureSettings.ColorMode SelectedColorMode
        {
            get
            {
                if (ColorMode.SelectedIndex == 0)
                    return ViewCaptureSettings.ColorMode.DisplayColor;
                else
                    return ViewCaptureSettings.ColorMode.BlackAndWhite;
            }
        }
        /// <summary>
        /// 0: Working   1: LocTemp   2: MeasDraw   3: Proto   4: EP   5: EP Legacy
        /// </summary>
        public int SelectedLocation { get { return OutputLocation.SelectedIndex; } }
        /// <summary>
        /// 0: Single    1: Multi   2: PageLayout
        /// </summary>
        public int SelectedType { get { return OutputType.SelectedIndex; } }
        /// <summary>
        /// Window Location after exit
        /// </summary>
        public Point WindowLocation { get { return WinLocation; } }
    }


    /// <summary>
    /// Create PDF of all Kinds
    /// </summary>
    public class PDFJob
    {
        //  --------------------------------------------------------------------------------------------------------  PDF Settings
        private int dpi;
        private ViewCaptureSettings.ColorMode ColorMode = ViewCaptureSettings.ColorMode.DisplayColor;
        private RhinoDoc doc;




        //  --------------------------------------------------------------------------------------------------------  PDF Construct
        public PDFJob(RhinoDoc Document, ViewCaptureSettings.ColorMode PDF_Color = ViewCaptureSettings.ColorMode.DisplayColor, int PDF_Dpi = 400)
        {
            dpi = PDF_Dpi;
            ColorMode = PDF_Color;
            doc = Document;
        }




        //  --------------------------------------------------------------------------------------------------------  PDF Creating Methods
        /// <summary>
        /// Sends out a Single Page PDF from Layer Selection
        /// </summary>
        /// <param name="pdfData"></param>
        public void MakeSinglePagePDF(string Path, string FileName, Layer OutLayer, double Width = 11.0, double Height = 8.5)
        {
            if (!OutLayer.IsVisible)
            {
                RhinoApp.WriteLine($"Layer {OutLayer.Name} is Hidden, Skipping...");
                return;
            }

            // Make sure the PDF will have a place to write
            ClearPath(Path, FileName);

            // Get Bounds of the objects on the layer
            var RObj = GetObjects(OutLayer);
            if (RObj.Count == 0)
            {
                RhinoApp.WriteLine($"Layer {OutLayer.Name} Has no Objects, Skipping..");
                return;
            }

            RhinoObject.GetTightBoundingBox(RObj, out BoundingBox BB);

            // Select the objects on the layer
            doc.Objects.UnselectAll();
            var obj = new List<Guid>(RObj.Count);
            for (int i = 0; i < RObj.Count; i++)
                obj.Add(RObj[i].Id);
            doc.Objects.Select(obj, true);

            // do the proper zooming
            var view = doc.Views.Find("Top", true);
            //view.MainViewport.ZoomBoundingBox(BB);

            // Construct the PDF page
            var page = Rhino.FileIO.FilePdf.Create();
            var capture = new ViewCaptureSettings(view, new System.Drawing.Size((int)(Width * dpi), (int)(Height * dpi)), dpi)
            {
                OutputColor = ColorMode,
                DrawSelectedObjectsOnly = true,
                ViewArea = ViewCaptureSettings.ViewAreaMapping.Window
            };

            BB.Inflate(BB.GetEdges()[0].Length * 0.03);
            capture.SetWindowRect(BB.Corner(true, true, true), BB.Corner(false, false, true));
            page.AddPage(capture);
            page.Write($"{Path}{FileName}.pdf");

            capture.Dispose();
        }

        /// <summary>
        /// Sends out a Single Page PDF from Layer Selection
        /// </summary>
        /// <param name="pdfData"></param>
        public void MakeSinglePagePDF(string Path, string FileName, List<RhinoObject> RObj)
        {
            // Make sure the PDF will have a place to write
            ClearPath(Path, FileName);

            // Get Bounds of the objects on the layer
            RhinoObject.GetTightBoundingBox(RObj, out BoundingBox BB);
            double Width = BB.GetEdges()[0].Length;
            double Height = BB.GetEdges()[1].Length;

            // Select the objects on the layer
            doc.Objects.UnselectAll();
            var obj = new List<Guid>(RObj.Count);
            for (int i = 0; i < RObj.Count; i++)
                obj.Add(RObj[i].Id);
            doc.Objects.Select(obj, true);

            // do the proper zooming
            var view = doc.Views.Find("Top", true);
            //view.MainViewport.ZoomBoundingBox(BB);

            // Construct the PDF page
            var page = Rhino.FileIO.FilePdf.Create();

            var capture = new ViewCaptureSettings(view, new System.Drawing.Size((int)(Width * dpi), (int)(Height * dpi)), dpi)
            {
                OutputColor = ColorMode,
                DrawSelectedObjectsOnly = true,
                ViewArea = ViewCaptureSettings.ViewAreaMapping.Window
            };
            
            capture.SetWindowRect(BB.Corner(true, true, true), BB.Corner(false, false, true));
            page.AddPage(capture);
            page.Write($"{Path}{FileName}.pdf");

            capture.Dispose();
        }

        /// <summary>
        /// Sends out Single PDF Made from Page Layout
        /// </summary>
        /// <param name="pdfdata"></param>
        public void MakeLayoutPDF(string Path, string FileName, string LayoutName)
        {
            ClearPath(Path, FileName);

            // Get the page view
            var layouts = new List<RhinoPageView>(doc.Views.GetPageViews());
            var PageView = layouts.Find((x) => x.PageName == LayoutName);

            var page = Rhino.FileIO.FilePdf.Create();
            var capture = new ViewCaptureSettings(PageView, new System.Drawing.Size((int)(PageView.PageWidth * dpi), (int)(PageView.PageHeight * dpi)), dpi)
            {
                OutputColor = ColorMode
            };
            page.AddPage(capture);
            page.Write($"{Path}{FileName}.pdf");

            capture.Dispose();
        }

        /// <summary>
        /// Makes a multi-page PDF file
        /// </summary>
        /// <param name="pdfDatas"></param>
        public void MakeMultiPagePDF(string Path, string FileName, List<Layer> OutLayers, bool IncrRhinoStatBar = true, double Width = 11.0, double Height = 8.5)
        {
            var page = Rhino.FileIO.FilePdf.Create();
            ViewCaptureSettings capture;
            ClearPath(Path, FileName);

            // Status bar
            StatusBar.ShowProgressMeter(0, OutLayers.Count + 20, "Progress: ", false, true);

            // start the page loop
            for (int i = 0; i < OutLayers.Count; i++)
            {
                // If Hidden, skip the layer
                if (!OutLayers[i].IsVisible)
                {
                    RhinoApp.WriteLine($"Layer: {OutLayers[i].Name} is Hidden, Skipping...");
                    StatusBar.UpdateProgressMeter(1, false);
                    continue;
                }

                // Get Bounds of the objects on the layer
                var RObj = GetObjects(OutLayers[i]);
                if (RObj.Count == 0)
                {
                    RhinoApp.WriteLine($"Layer: {OutLayers[i].Name} has no objects");
                    StatusBar.UpdateProgressMeter(1, false);
                    continue;
                }

                RhinoObject.GetTightBoundingBox(RObj, out BoundingBox BB);

                // Select the objects on the layer
                doc.Objects.UnselectAll();
                var obj = new List<Guid>(RObj.Count);
                for (int o = 0; o < RObj.Count; o++)
                    obj.Add(RObj[o].Id);
                doc.Objects.Select(obj, true);

                // do the proper zooming
                var view = doc.Views.Find("Top", true);
                //view.MainViewport.ZoomBoundingBox(BB);

                // Construct the PDF page
                capture = new ViewCaptureSettings(view, new System.Drawing.Size((int)(Width * dpi), (int)(Height * dpi)), dpi)
                {
                    OutputColor = ColorMode,
                    DrawSelectedObjectsOnly = true,
                    ViewArea = ViewCaptureSettings.ViewAreaMapping.Window
                };

                BB.Inflate(BB.GetEdges()[0].Length * 0.03);
                capture.SetWindowRect(BB.Corner(true, true, true), BB.Corner(false, false, true));
                page.AddPage(capture);

                StatusBar.UpdateProgressMeter(1, false);
            }

            page.Write($"{Path}{FileName}.pdf");
        }

        /// <summary>
        /// Checks if the file path is created or creates it
        /// </summary>
        /// <param name="pdfData"></param>
        private void ClearPath(string Path, string Name)
        {
            var fullPath = $"{Path}{Name}.pdf";

            if (!System.IO.Directory.Exists(Path))
            {
                // see if the folder exists or create it
                System.IO.Directory.CreateDirectory(Path);
            }
            else
            {
                // directory exists, see if the pdf does, then delete
                if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
            }
        }

        /// <summary>
        /// Get all Visible Objects from parent layer
        /// </summary>
        /// <param name="lay"></param>
        /// <returns></returns>
        private List<RhinoObject> GetObjects(Layer lay)
        {
            var obj = doc.Objects.FindByLayer(lay);
            var RObj = new List<RhinoObject>();

            if (obj != null)
                RObj.AddRange(obj);

            var childLay = lay.GetChildren();
            if (childLay != null)
            {
                for (int i = 0; i < childLay.Length; i++)
                {
                    if (childLay[i].IsVisible)
                    {
                        var cObj = doc.Objects.FindByLayer(childLay[i]);
                        if (cObj != null)
                            RObj.AddRange(cObj);
                    }
                }
            }
            return RObj;
        }
    }
}