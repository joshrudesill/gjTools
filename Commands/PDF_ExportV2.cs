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
    public class PDF_ExportV2 : Command
    {
        public PDF_ExportV2()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static PDF_ExportV2 Instance { get; private set; }

        public override string EnglishName => "PDF_ExportV2";
        public Eto.Drawing.Point PDFwindowPosition = Eto.Drawing.Point.Empty;

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            if (PDFwindowPosition == Eto.Drawing.Point.Empty)
                PDFwindowPosition = new Eto.Drawing.Point((int)MouseCursor.Location.X - 250, 200);

            var win = new PDF.PDF_Dialog(doc, PDFwindowPosition);

            var res = win.ShowForm();
            
            return Result.Success;
        }
    }


}



namespace PDF 
{
    using Eto;
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
        private Button But_Ok;
        private GridView PDFItems;
        private TextBox PDFName;
        private RhinoDoc doc;

        private List<Layer> ParentLayers;
        private List<List<string>> ds_ParentLayers;
        private List<RhinoPageView> PageViews;
        private List<List<string>> ds_PageViews;

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

            But_Ok = new Button { Text = "Ok" };
            Button But_Cancel = new Button { Text = "Cancel" };
            But_Ok.Click += But_Ok_Click;
            But_Cancel.Click += (s, e) => Window.Close(DialogResult.Cancel);

            PDFItems = new GridView
            {
                Size = new Size(250, -1),
                ShowHeader = true,
                Border = BorderType.Line,
                GridLines = GridLines.None,
                AllowMultipleSelection = true,
                Columns = {
                    new GridColumn {
                        HeaderText = "Layer",
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
                Items = { "Working Location", "Local Temp", "Measure Drawing", "Prototype", "E&P", "E&P Legacy" },
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

            PDFName = new TextBox { Enabled = false };

            var NameGrp = new TableLayout
            {
                Padding = new Padding(5),
                Spacing = new Size(5,5),
                Rows =
                {
                    new TableRow(LocationGrp),
                    new TableRow(TypeGrp),
                    new TableRow(ColorGrp),
                    new TableRow(new Label{ Text = "PDF Name:" }),
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
                                new TableRow(null, But_Ok, But_Cancel)
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

            foreach (Layer l in doc.Layers)
            {
                if (l.ParentLayerId == Guid.Empty && !l.IsDeleted && l.Name != "NestBoxes")
                {
                    parents.Add(l);
                    ds_ParentLayers.Add(new List<string> { l.Name });
                }
            }

            var layouts = doc.Views.GetPageViews();
            PageViews = new List<RhinoPageView>();
            if (layouts.Length > 0)
            {
                foreach(var p in layouts)
                {
                    PageViews.Add(p);
                    ds_PageViews.Add(new List<string> { p.PageName });
                }
            }

            ParentLayers = parents;
        }



        //  -----------------------------------------------------------------------------  Events
        private void Radio_SelectedIndexChanged(object sender, EventArgs e)
        {
            var rb = (RadioButtonList)sender;

            if (rb.ID == "Location")
            {
                if (doc.Path == null && rb.SelectedIndex == 0)
                    rb.SelectedIndex = 1;
                return;
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
                    }
                }
                else if (PDFItems.DataStore == ds_PageViews)
                {
                    PDFItems.DataStore = ds_ParentLayers;
                }

                //  Control the PDF Name if it's a multipage
                if (rb.SelectedIndex == 1)
                {
                    PDFName.Enabled = true;
                    if (doc.Name != null)
                        PDFName.Text = doc.Name;
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
                But_Ok.Enabled = true;
            else
                But_Ok.Enabled = false;
        }

        private void But_Ok_Click(object sender, EventArgs e)
        {
            //  Do the pdf thing here

            Window.Close(DialogResult.Ok);
        }






        //  -----------------------------------------------------------------------------  Show Window
        public DialogResult ShowForm()
        {
            LoadControls();

            var thr = new Thread(LoadLayers);
            thr.Start();
            thr.Join();
            PDFItems.DataStore = ds_ParentLayers;

            Window.ShowModal(RhinoEtoApp.MainWindow);

            return Window.Result;
        }




        //  -----------------------------------------------------------------------------  Make PDF Files

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
        public PDFJob(RhinoDoc Document, ViewCaptureSettings.ColorMode PDF_Color = ViewCaptureSettings.ColorMode.DisplayColor, int PDF_Dpi = 400 )
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
            // Make sure the PDF will have a place to write
            ClearPath(Path, FileName);

            // Get Bounds of the objects on the layer
            var RObj = doc.Objects.FindByLayer(OutLayer);
            if (RObj == null) return;

            RhinoObject.GetTightBoundingBox(RObj, out BoundingBox BB);
            
            // Select the objects on the layer
            doc.Objects.UnselectAll();
            var obj = new List<Guid>(RObj.Length);
            for (int i = 0; i < RObj.Length; i++)
                obj.Add(RObj[i].Id);
            doc.Objects.Select(obj, true);

            // do the proper zooming
            var view = doc.Views.Find("Top", true);
                view.MainViewport.ZoomBoundingBox(BB);

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
            var layouts = new List<RhinoPageView>( doc.Views.GetPageViews() );
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
        public void MakeMultiPagePDF(string Path, string FileName, List<Layer> OutLayers, double Width = 11.0, double Height = 8.5)
        {
            var page = Rhino.FileIO.FilePdf.Create();
            ViewCaptureSettings capture;
            ClearPath(Path, FileName);

            // start the page loop
            for (int i = 0; i < OutLayers.Count; i++)
            {
                // Get Bounds of the objects on the layer
                var RObj = doc.Objects.FindByLayer(OutLayers[i]);
                if (RObj == null) continue;

                RhinoObject.GetTightBoundingBox(RObj, out BoundingBox BB);

                // Select the objects on the layer
                doc.Objects.UnselectAll();
                var obj = new List<Guid>(RObj.Length);
                for (int o = 0; o < RObj.Length; o++)
                    obj.Add(RObj[o].Id);
                doc.Objects.Select(obj, true);

                // do the proper zooming
                var view = doc.Views.Find("Top", true);
                view.MainViewport.ZoomBoundingBox(BB);

                // Construct the PDF page
                capture = new ViewCaptureSettings(view, new System.Drawing.Size((int)(Width * dpi), (int)(Height * dpi)), dpi)
                {
                    OutputColor = ColorMode,
                    DrawSelectedObjectsOnly = true,
                    ViewArea = ViewCaptureSettings.ViewAreaMapping.Window
                };
                capture.SetWindowRect(BB.Corner(true, true, true), BB.Corner(false, false, true));
                page.AddPage(capture);
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
                if (System.IO.File.Exists(fullPath))  System.IO.File.Delete(fullPath);
            }
        }
    }
}