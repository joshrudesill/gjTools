using System;
using System.Collections.Generic;
using System.Threading;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.DocObjects;
using Rhino.UI;
using Rhino.Input;

namespace gjTools.Commands
{
    [CommandStyle(Style.ScriptRunner)]
    public class CAD_CutFile : Command
    {
        public CAD_CutFile()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static CAD_CutFile Instance { get; private set; }

        public override string EnglishName => "CAD_CutFile";
        public Eto.Drawing.Point windowPosition = Eto.Drawing.Point.Empty;

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            if (windowPosition == Eto.Drawing.Point.Empty)
                windowPosition = new Eto.Drawing.Point((int)MouseCursor.Location.X - 250, 200);

            var cg = new CutFile.CutGui(doc, windowPosition);
            var res = cg.ShowForm();
            windowPosition = cg.WinPosition;

            if (res == Eto.Forms.DialogResult.Cancel)
                return Result.Cancel;

            var ct = new CutFile.CreateCutFile(doc, GetPath(doc, cg));

            // Loop the stuff
            foreach(var i in cg.GetLayers)
            {
                var nestBox = ct.CheckMultiNestBox(i);
                if (nestBox.Count > 0)
                {
                    ObjRef SelectedBox = new ObjRef(nestBox[0]);

                    if (nestBox.Count > 1)
                    {
                        ct.HighlightCurve(nestBox);

                        // Ask to choose
                        if (RhinoGet.GetOneObject("Select One Nest Box", false, ObjectType.Curve, out SelectedBox) != Result.Success)
                        {
                            ct.Show.Dispose();
                            return Result.Cancel;
                        }
                        ct.Show.Dispose();
                    }

                    // Check the Lines for non-polylines
                    var RObj = ct.GetCutObjects(i, SelectedBox);
                        RObj.Add(SelectedBox.Object());
                    var BadPart = ct.CheckPolyLines(RObj);
                    
                    // Display Bad Lines (If Any)
                    if (BadPart)
                    {
                        string Fuck = "";
                        RhinoGet.GetString("Some Bad Lines, No cut file Made...", true, ref Fuck);
                        ct.Show.Dispose();
                        doc.Views.Redraw();
                        return Result.Cancel;
                    }
                    ct.Show.Dispose();
                    doc.Views.Redraw();

                    // We are here so the part is good
                    //ct.WritePDF_CutFile(RObj, cg.GetCutName[cg.GetLayers.IndexOf(i)]);
                    ct.WriteDXF(RObj, cg.GetCutName[cg.GetLayers.IndexOf(i)]);
                    cg.IncrementTempCut();
                    ct.CreateCutText(SelectedBox.Object(), i, cg.GetCutName[cg.GetLayers.IndexOf(i)]);
                }
                else
                    continue;
            }

            return Result.Success;
        }

        private string GetPath(RhinoDoc doc, CutFile.CutGui FData)
        {
            var Path = "";
            var locations = FileLocations.Paths(doc);

            var type = FData.GetCutType;
            if (type == "Working Location")
            {
                Path = locations[0];
            }
            else if (type == "Router")
            {
                Path = locations[6];
            }
            else if (type == "E&P")
            {
                Path = locations[4] + doc.Name.Replace(".3dm", @"\");
            }
            else
            {
                Path = locations[5];
            }

            return Path;
        }
    }
}



namespace CutFile
{
    using Eto.Forms;
    using Eto.Drawing;
    using System.Text.RegularExpressions;

    public class CutGui
    {
        private Dialog<DialogResult> Window;
        private Point WindowPosition;

        private RadioButtonList R_CutType;

        private Button But_Ok;
        private Button But_Cancel;

        private TextBox FileName;
        private GridView CutItems;
        private List<List<string>> ds_CutItems;
        private List<int> ds_EPCutItems;
        private List<Layer> layers;
        private int CurrentLayer;
        private string NextDefaultCutName;
        private string JobNumber;
        private int NextDefaultCutNum;

        private gjTools.SQLTools sql;
        private RhinoDoc doc;





        //  --------------------------------------------------  Load Form Data
        public CutGui(RhinoDoc Document, Point WinLocation)
        {
            doc = Document;
            WindowPosition = WinLocation;

            // Launch on new thread
            Thread thr = new Thread(LoadLayers);
            thr.Start();
            thr.Join();

            LoadControls();
            LoadEvents();
        }

        private void LoadLayers()
        {
            var lays = doc.Layers;
            ds_CutItems = new List<List<string>>();
            ds_EPCutItems = new List<int>();
            layers = new List<Layer>();
            CurrentLayer = 0;
            sql = new gjTools.SQLTools();

            // Create the job regex
            var regX = new Regex(@"J\d{9}-\d");
            if (doc.Path != null)
                JobNumber = regX.Match(doc.Path).Value;
            else
                JobNumber = "";

            for (int i = 0; i < lays.Count; i++)
            {
                var l = lays[i];

                // Check if its a parent layer
                if (l.ParentLayerId != Guid.Empty)
                    continue;

                // Only Visible Layers
                if (l.IsDeleted || l.Name == "" || !l.IsVisible)
                    continue;

                // add items
                ds_CutItems.Add(new List<string> { l.Name });
                layers.Add(l);

                if (l.Name.StartsWith("CUT"))
                    ds_EPCutItems.Add(ds_CutItems.Count - 1);

                // Get current layer index
                if (l == doc.Layers.CurrentLayer)
                    CurrentLayer = ds_CutItems.Count - 1;
            }

            // Find the next cut name
            var cut = sql.queryVariableData();
            NextDefaultCutName = $"{cut.userInitials}{cut.cutNumber}";
            NextDefaultCutNum = cut.cutNumber;
        }

        private void LoadControls()
        {
            Window = new Dialog<DialogResult>
            {
                Title = "CutFile Export",
                Result = DialogResult.Cancel,
                AutoSize = true,
                Minimizable = false,
                Maximizable = false,
                Padding = new Padding(5),
                Resizable = false,
                Topmost = true,
                Location = WindowPosition
            };

            R_CutType = new RadioButtonList
            {
                Items = { "Working Location", "Router", "Default", "Default Custom", "E&P" },
                Orientation = Orientation.Vertical,
                SelectedIndex = 2,
                Spacing = new Size(5, 5),
                TabIndex = 0,
                ID = "Location"
            };

            But_Ok = new Button { Text = "OK", TabIndex = 3 };
            But_Cancel = new Button { Text = "Cancel" };

            FileName = new TextBox
            {
                PlaceholderText = "CutName",
                Text = NextDefaultCutName,
                ToolTip = "CTRL+J Adds the Job Number (If Any)\nCTRL+L Adds the Layer Name",
                ID = "FileName",
                TabIndex = 2,
                Enabled = false
            };

            CutItems = new GridView
            {
                Size = new Size(250, 300),
                ShowHeader = true,
                Border = BorderType.Line,
                GridLines = GridLines.None,
                AllowEmptySelection = false,
                AllowMultipleSelection = false,
                TabIndex = 1,
                Columns =
                {
                    new GridColumn
                    {
                        HeaderText = "Layer",
                        Editable = false,
                        Expand = true,
                        DataCell = new TextBoxCell(0)
                    }
                },
                DataStore = ds_CutItems,
                SelectedRow = CurrentLayer
            };

            //  ---------------------------------------  Make Layout
            var LocGroup = new GroupBox
            {
                Text = "Location",
                Padding = new Padding(5),
                Content = new TableLayout{ Rows = { new TableRow(R_CutType) } }
            };

            var space = new Size(5, 5);
            Window.Content = new TableLayout
            {
                Spacing = space,
                Rows =
                {
                    new TableLayout
                    {
                        Spacing = space,
                        Rows =
                        {
                            new TableRow(LocGroup, CutItems)
                        }
                    },
                    new TableLayout
                    {
                        Spacing = space,
                        Rows =
                        {
                            new TableRow(new Label{ Text = "Cut Name:" }, FileName)
                        }
                    },
                    new TableLayout
                    {
                        Spacing = space,
                        Rows =
                        {
                            new TableRow(null, But_Ok, But_Cancel)
                        }
                    }
                }
            };
        }

        private void LoadEvents()
        {
            R_CutType.SelectedIndexChanged += R_CutType_SelectedIndexChanged;
            CutItems.SelectedItemsChanged += CutItems_SelectedItemsChanged;
            But_Cancel.Click += (s, e) => Window.Close(DialogResult.Cancel);
            But_Ok.Click += (s, e) => Window.Close(DialogResult.Ok);
            FileName.KeyUp += FileName_KeyUp;
            FileName.KeyDown += FileName_KeyDown;
            Window.KeyUp += Window_KeyUp;
        }



        //  --------------------------------------------------  Show the Form
        /// <summary>
        /// Show the Dialog and freeze Mainwindow
        /// </summary>
        /// <returns></returns>
        public DialogResult ShowForm()
        {
            Window.ShowModal(RhinoEtoApp.MainWindow);
            WindowPosition = Window.Location;
            return Window.Result;
        }





        //  --------------------------------------------------  Event Functions
        private void R_CutType_SelectedIndexChanged(object sender, EventArgs e)
        {
            int sel = R_CutType.SelectedIndex;
            var path = doc.Path;
            var name = doc.Name;

            // enable the filename field
            if (sel == 0 || sel == 1 || sel == 3)
                FileName.Enabled = true;
            else
                FileName.Enabled = false;

            // See if file is saved
            if (sel == 0 && path == null)
                R_CutType.SelectedIndex = 2;

            if (CutItems.AllowMultipleSelection)
            {
                CutItems.AllowMultipleSelection = false;
                CutItems.Enabled = true;
                CutItems.SelectedRow = CurrentLayer;
            }

            // Preset the filename field per cuttype
            if (sel == 0)
            {
                // Working Location
                FileName.Text = layers[CutItems.SelectedRow].Name;
            }
            else if (sel == 1)
            {
                // Router
                FileName.Text = layers[CutItems.SelectedRow].Name + "-ROUTE";
                return;
            }
            else if (sel == 2)
            {
                // Default Next up cut number
                FileName.Text = NextDefaultCutName;
                return;
            }
            else if (sel == 3)
            {
                // Custom Default
                FileName.Text = NextDefaultCutName.Substring(0, 2);
                return;
            }
            else if (sel == 4 && ds_EPCutItems.Count != 0)
            {
                // E&P Output
                CutItems.AllowMultipleSelection = true;
                CutItems.SelectedRows = ds_EPCutItems;
                CutItems.Enabled = false;
                FileName.Text = "";
            }
            else
            {
                R_CutType.SelectedIndex = 2;
            }
        }

        private void FileName_KeyUp(object sender, KeyEventArgs e)
        {
            if (FileName.Text == "")
                But_Ok.Enabled = false;
            else
                But_Ok.Enabled = true;
        }

        private void FileName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Keys.J && e.Modifiers == Keys.Control)
            {
                int ind = FileName.CaretIndex;

                FileName.Text = FileName.Text.Insert(FileName.CaretIndex, JobNumber);
                FileName.CaretIndex = JobNumber.Length + ind;
                return;
            }
            if (e.Key == Keys.L && e.Modifiers == Keys.Control)
            {
                int ind = FileName.CaretIndex;
                string lName = layers[CutItems.SelectedRow].Name;

                FileName.Text = FileName.Text.Insert(FileName.CaretIndex, lName);
                FileName.CaretIndex = lName.Length + ind;
            }
        }

        private void CutItems_SelectedItemsChanged(object sender, EventArgs e)
        {
            var locSel = R_CutType.SelectedIndex;
            int sel = CutItems.SelectedRow;

            if (locSel == 0 || locSel == 1)
            {
                // router or working
                FileName.Text = layers[CutItems.SelectedRow].Name;
                if (locSel == 1)
                    FileName.Text += "-ROUTE";
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Keys.Escape)
                Window.Close(DialogResult.Cancel);
        }




        //  --------------------------------------------------  Getters
        /// <summary>
        /// Return the new position of the window
        /// </summary>
        public Point WinPosition { get { return WindowPosition; } }

        /// <summary>
        /// Chosen Layer or Layers if E&P and more than one cut
        /// </summary>
        public List<Layer> GetLayers
        {
            get
            {
                var l = new List<Layer>();

                if (R_CutType.SelectedIndex == 4)
                {
                    foreach (var i in ds_EPCutItems)
                        l.Add(layers[i]);
                }
                else
                {
                    l.Add(layers[CutItems.SelectedRow]);
                }

                return l;
            }
        }

        /// <summary>
        /// Chosen Name or names if E&P and more than one cut
        /// </summary>
        public List<string> GetCutName
        {
            get
            {
                var cNames = new List<string>();

                if (R_CutType.SelectedIndex == 4)
                {
                    foreach (var i in ds_EPCutItems)
                        cNames.Add(doc.Name.Replace("A.3dm", $"_{layers[i].Name}"));
                }
                else
                {
                    cNames.Add(FileName.Text);
                }

                return cNames;
            }
        }

        /// <summary>
        /// Return a string saying what cut type this is
        /// <para>"Working Location", "Router", "Default", "Default Custom", "E&P"</para>
        /// </summary>
        public string GetCutType
        {
            get
            {
                var list = new List<string> { "Working Location", "Router", "Default", "Default Custom", "E&P" };
                return list[R_CutType.SelectedIndex];
            }
        }
        
        /// <summary>
        /// Get the SQL ref obj
        /// </summary>
        public gjTools.SQLTools GetSql { get { return sql; } }

        /// <summary>
        /// Uptick the cut number for next time
        /// </summary>
        public void IncrementTempCut()
        {
            if (R_CutType.SelectedIndex != 2)
                return;

            var vd = sql.queryVariableData();
                vd.cutNumber++;

            sql.updateVariableData(vd);
        }
    }

    public class CreateCutFile
    {
        private RhinoDoc doc;
        private string Path;
        public Rhino.Display.CustomDisplay Show { get; set; }

        public CreateCutFile(RhinoDoc Document, string path)
        {
            doc = Document;
            Path = path;
        }


        /// <summary>
        /// Return objects on nest layer
        /// </summary>
        /// <param name="l"></param>
        /// <returns></returns>
        public List<RhinoObject> CheckMultiNestBox(Layer l)
        {
            var NestBox = new List<RhinoObject>();
            var sl = new List<Layer>( l.GetChildren() );

            if (sl.Count == 0)
                return NestBox;

            Layer nestLay = null;
            for (int i = 0; i < sl.Count; i++)
            {
                if (sl[i].Name == "NestBox")
                {
                    nestLay = sl[i];
                    break;
                }
            }

            if (nestLay == null)
                return NestBox;

            var items = new List<RhinoObject>( doc.Objects.FindByLayer(nestLay) );

            return items;
        }

        /// <summary>
        /// Highlight Objects Blue
        /// </summary>
        /// <param name="Obj"></param>
        /// <returns></returns>
        public void HighlightCurve(List<RhinoObject> Obj)
        {
            Show = new Rhino.Display.CustomDisplay(true);

            foreach(var crv in Obj)
            {
                var c = crv as CurveObject;
                if (c != null)
                    Show.AddCurve(c.CurveGeometry, System.Drawing.Color.Aquamarine, 5);
            }

            doc.Views.Redraw();
        }

        /// <summary>
        /// Return the Cut Objects
        /// </summary>
        /// <param name="l"></param>
        /// <returns></returns>
        public List<RhinoObject> GetCutObjects(Layer l, ObjRef NestBox)
        {
            var cutLays = l.GetChildren();
            var RObj = new List<RhinoObject>();
            var nBB = NestBox.Geometry().GetBoundingBox(true);

            for (int i = 0; i < cutLays.Length; i++)
            {
                var sl = cutLays[i];

                if (sl.Name.Substring(0,2) == "C_")
                {
                    var layerObjects = doc.Objects.FindByLayer(sl);

                    // Check if the objects are within the box
                    foreach(var ro in layerObjects)
                    {
                        if (nBB.Contains(ro.Geometry.GetBoundingBox(true), false))
                            RObj.Add(ro);
                    }
                }
            }

            return RObj;
        }

        /// <summary>
        /// Highlight Bad Lines (if any) (All inputs are Assumed Curves or text)
        /// </summary>
        /// <param name="RObj"></param>
        /// <returns></returns>
        public bool CheckPolyLines(List<RhinoObject> RObj)
        {
            Show = new Rhino.Display.CustomDisplay(true);
            var c_red = System.Drawing.Color.OrangeRed;
            var BadPart = false;

            for (int i = 0; i < RObj.Count; i++)
            {
                var typ = RObj[i].Geometry.ObjectType;

                if (typ == ObjectType.Curve)
                {
                    var crv = RObj[i].Geometry as Curve;
                    var segs = crv.DuplicateSegments();

                    // Replace Circle
                    if (crv.IsCircle())
                    {
                        // Convert to circle
                        crv.TryGetCircle(out Circle Cir, 0.05);

                        if (Cir.IsValid)
                            doc.Objects.Replace(RObj[i].Id, Cir);
                        continue;
                    }

                    for (int ii = 0; ii < segs.Length; ii++)
                    {
                        Curve s = segs[ii];

                        if (!s.IsLinear() && !s.IsArc())
                        {
                            Show.AddCurve(s, c_red, 5);
                            BadPart = true;
                        }
                    }

                    // Testing to see if the Curve is planar
                    if (!crv.IsPlanar(0.001))
                    {
                        RhinoApp.WriteLine("Curve is not Planar, and that's real bad cowboy...");
                    }
                }
                else if (typ == ObjectType.Annotation)
                {
                    var te = RObj[i] as TextObject;

                    if (te == null)
                        BadPart = true;
                }
            }

            doc.Views.Redraw();
            return BadPart;
        }

        /// <summary>
        /// Does what it say
        /// </summary>
        /// <param name="RObj"></param>
        public bool WriteDXF(List<RhinoObject> RObj, string FileName)
        {
            var FullPath = Path + FileName;
            doc.Objects.UnselectAll();

            for (int i = 0; i < RObj.Count; i++)
                doc.Objects.Select(RObj[i].Id, true);

            return RhinoApp.RunScript($"_-Export \"{FullPath}.dxf\" Scheme \"Vomela\" _Enter", false);
        }

        /// <summary>
        /// Testing writeing a PDF CutFile
        /// </summary>
        /// <param name="RObl"></param>
        /// <param name="FileName"></param>
        /// <returns></returns>
        public bool WritePDF_CutFile(List<RhinoObject> RObj, string FileName)
        {
            if (RObj.Count == 0)
                return false;

            // Get the pdf Class
            var pdf = new PDF.PDFJob(doc, Rhino.Display.ViewCaptureSettings.ColorMode.DisplayColor, 72);
            pdf.MakeSinglePagePDF(Path, FileName, RObj);

            return true;
        }

        /// <summary>
        /// Create the Cut text
        /// </summary>
        /// <param name="RObj"></param>
        /// <param name="CutName"></param>
        public void CreateCutText(RhinoObject NestBox, Layer parent, string CutName)
        {
            var nBB = NestBox.Geometry.GetBoundingBox(false);
            var dt = new DrawTools(doc);
            var ds = dt.StandardDimstyle();
            var offsetPt = new Point3d(-0.5, 0.5, 0);
            var scaleFact = (CutName.Length > 9) ? 0.4 : 0.25;

            var txt = dt.AddText("Cut Name:", nBB.Corner(false, false, true) + new Point3d(0, 0.75, 0), ds, 0.65, 0, 0, 6);
            var cName = dt.AddText(CutName, nBB.Corner(false, false, true), ds, 1.5, 1, 0, 0);

            var rect = new Rectangle3d(Plane.WorldXY,
                txt.GetBoundingBox(true).Corner(true, false, true) + offsetPt,
                cName.GetBoundingBox(true).Corner(false, true, true) + -offsetPt);

            var xForm = Transform.Translation(nBB.Corner(false, false, true) - rect.Corner(1));
            var scale = Transform.Scale(nBB.Corner(false, false, true), (nBB.GetEdges()[0].Length * scaleFact) / rect.Width);

            txt.Transform(xForm); cName.Transform(xForm); rect.Transform(xForm);
            txt.Transform(scale, doc.DimStyles[ds]); cName.Transform(scale, doc.DimStyles[ds]); rect.Transform(scale);

            var attr = new ObjectAttributes { LayerIndex = parent.Index };
                attr.AddToGroup(doc.Groups.Add());

            doc.Objects.AddText(txt, attr);
            doc.Objects.AddText(cName, attr);
            doc.Objects.AddRectangle(rect, attr);
        }
    }
}