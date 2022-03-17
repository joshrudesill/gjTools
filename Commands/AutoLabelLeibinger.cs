using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;


namespace gjTools.Commands
{
    public struct DotData
    {
        public List<string> UniqueDotNames;
        public List<TextDot> AllDots;
    }

    public class UserStrings
    {
        public string PartName;
        public string DotName;
        public string LayerName;
        public string ParentLayer;
        public Eto.Drawing.Point windowPosition;
        public List<Layer> pLays;

        // for outputs
        public OEM_Label label;
        public int dotIndex;
        public int layerIndex;
        public string labelLayer;

        // custom text
        public string textLine1;
        public string textLine2;

        public List<string> parentLayerNames()
        {
            var lnames = new List<string>(pLays.Count);

            foreach(Layer layer in pLays)
                lnames.Add(layer.Name);

            return lnames;
        }
    }

    public class AutoLabelLeibinger : Command
    {
        public AutoLabelLeibinger()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static AutoLabelLeibinger Instance { get; private set; }

        public override string EnglishName => "AutoLabelLeibinger";
        private string m_PartName = "DUT-21-78365A";
        private string m_LayerName = "C_TEXT";
        private string m_TextBox1 = "";
        private string m_TextBox2 = "";
        private Eto.Drawing.Point m_windowPosition;

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // collect all of the dots in the document
            var allDots = new List<RhinoObject>(
                doc.Objects.FindByFilter(
                    new ObjectEnumeratorSettings()
                    {
                        ObjectTypeFilter = ObjectType.TextDot
                    }
                ));
            if (allDots.Count == 0)
            {
                RhinoApp.WriteLine("No TextDot objects Found in this document..");
                return Result.Cancel;
            }
            var DData = GetDotList(allDots);

            // get a list of the parent layers
            var pLays = new List<Layer>();
            foreach (Layer l in doc.Layers)
                if (l.ParentLayerId == Guid.Empty && l.Name.Length > 1)
                    pLays.Add(l);

            // define user object
            var UData = new UserStrings()
            {
                PartName = m_PartName,
                DotName = DData.UniqueDotNames[0],
                LayerName = "",
                labelLayer = m_LayerName,
                ParentLayer = pLays[0].Name,
                windowPosition = m_windowPosition,
                pLays = pLays,
                textLine1 = m_TextBox1,
                textLine2 = m_TextBox2
            };

            // create the gui
            var GetInfo = new GUI.AutoLabelLeibingerGUI(UData, DData);
            m_windowPosition = UData.windowPosition;
            m_PartName = UData.PartName;
            m_LayerName = UData.labelLayer;
            m_TextBox1 = UData.textLine1;
            m_TextBox2 = UData.textLine2;
            if (GetInfo.CommandResult == Eto.Forms.DialogResult.Cancel)
                return Result.Cancel;

            // place the labels
            var pTool = new PrototypeTool();
            var lTool = new LayerTools(doc);
            var lay = lTool.CreateLayer(UData.labelLayer, pLays[UData.layerIndex].Name );
            foreach(var d in DData.AllDots)
                if (d.Text == DData.UniqueDotNames[UData.dotIndex])
                    if (!pTool.PlaceProductionLabel(doc, UData.label, lay, d.Point))
                        AddCustomText(UData, doc, d.Point, lay);

            doc.Views.Redraw();
            return Result.Success;
        }


        /// <summary>
        /// use this for when the oem label is not found
        /// </summary>
        /// <param name="UData"></param>
        /// <param name="doc"></param>
        /// <param name="pt"></param>
        /// <param name="lay"></param>
        private void AddCustomText(UserStrings UData, RhinoDoc doc, Point3d pt, Layer lay)
        {
            var attr = new ObjectAttributes { LayerIndex = lay.Index };
            var dt = new DrawTools(doc);
            var ds = dt.StandardDimstyle();

            if (UData.textLine1.Length > 0)
            {
                var tEntity = dt.AddText(UData.textLine1, pt, ds, 0.16, 0, 3, 6);
                doc.Objects.Add(tEntity, attr);
            }
            if (UData.textLine2.Length > 0)
            {
                var tEntity = dt.AddText(UData.textLine2, new Point3d(pt.X, pt.Y - 0.25, 0), ds, 0.14, 0, 3, 0);
                doc.Objects.Add(tEntity, attr);
            }
        }

        /// <summary>
        /// returns a list of custom dot object structs
        /// </summary>
        /// <param name="rObj"></param>
        /// <returns></returns>
        private DotData GetDotList(List<RhinoObject> rObj)
        {
            var DData = new DotData();
            DData.UniqueDotNames = new List<string>();
            DData.AllDots = new List<TextDot>();

            foreach(var r in rObj)
            {
                var td = r.Geometry as TextDot;
                DData.AllDots.Add(td);

                if (!DData.UniqueDotNames.Contains(td.Text))
                    DData.UniqueDotNames.Add(td.Text);
            }

            return DData;
        }
    }
}




namespace GUI
{
    using Eto.Forms;
    using Eto.Drawing;
    using Rhino.UI;

    public class AutoLabelLeibingerGUI
    {
        private Dialog<DialogResult> m_Dialog;
        public Point windowPosition = new Point(400, 400);
        public gjTools.Commands.OEM_Label LabelInfo;

        private Button m_button_search = new Button { Text = "Search", Width = 80 };
        private Button m_button_ok = new Button { Text = "OK" };
        private Button m_button_cancel = new Button { Text = "Cancel" };

        private DropDown m_drop_dots = new DropDown { SelectedIndex = 0, Width = 180 };
        private DropDown m_drop_layers = new DropDown { SelectedIndex = 0, Width = 180 };

        private TextBox m_tbox_partNumber = new TextBox { ID = "PN", Width = 250 };
        private TextBox m_tbox_layerName = new TextBox { ID = "LAYNAME" };

        private TextBox m_tbox_partResult1 = new TextBox { Text = "", ToolTip = "ctrl+P: Add selected text to standard datamatrix" };
        private TextBox m_tbox_partResult2 = new TextBox { Text = "", ToolTip = "ctrl+D: Add selected text to standard part description" };


        /// <summary>
        /// construct the dialog and display it
        /// </summary>
        /// <param name="UData"></param>
        /// <param name="DData"></param>
        public AutoLabelLeibingerGUI(gjTools.Commands.UserStrings UData, gjTools.Commands.DotData DData)
        {
            windowPosition = UData.windowPosition;

            m_tbox_partNumber.Text = UData.PartName;
            m_tbox_layerName.Text = UData.LayerName;

            m_drop_dots.DataStore = DData.UniqueDotNames;
            m_drop_layers.DataStore = UData.parentLayerNames();

            m_tbox_partResult1.Text = UData.textLine1;
            m_tbox_partResult2.Text = UData.textLine2;

            m_tbox_layerName.Text = UData.labelLayer;

            m_Dialog = new Dialog<DialogResult>
            {
                Padding = 10,
                Title = "Text Label Maker",
                AutoSize = true,
                Topmost = true,
                Result = DialogResult.Cancel,
                WindowStyle = WindowStyle.Default,
                Location = windowPosition
            };

            var dropLayout = new TableLayout
            {
                Padding = new Padding(5, 5, 5, 5),
                Spacing = new Size(5, 5),
                Rows =
                {
                    new TableRow(new Label{Text = "Dot Mark to use:" }, m_drop_dots),
                    new TableRow(new Label{Text = "Add to Parent Layer:" }, m_drop_layers),
                    new TableRow(new Label{Text = "Label Layer Name:" }, m_tbox_layerName)
                }
            };

            var searchLayout = new TableLayout
            {
                Padding = new Padding(5, 5, 5, 5),
                Spacing = new Size(5, 5),
                Rows =
                {
                    new TableRow(new Label{Text = "Part Number:"}, m_tbox_partNumber, m_button_search)
                }
            };

            var ResultLayout = new TableLayout
            {
                Padding = new Padding(5, 5, 5, 5),
                Spacing = new Size(5, 5),
                Rows =
                {
                    new TableRow(new Label{Text = "Label Line 1:"}),
                    new TableRow(m_tbox_partResult1),
                    new TableRow(new Label{Text = "Ex. PART_NUMBER        <datamatrix,PART_NUMBER>", TextColor = Color.FromGrayscale(0.5f)}),
                    new TableRow(new Label{Text = "Label Line 2:"}),
                    new TableRow(m_tbox_partResult2),
                    new TableRow(new Label{Text = "Ex. CUSTOMER_PTNO   PART_DESCRIPTION CUT DATE: <date,MM/dd/yyyy> <orderid>", TextColor = Color.FromGrayscale(0.5f)})
                }
            };

            var buttonLayout = new TableLayout
            {
                Padding = new Padding(5, 5, 5, 5),
                Spacing = new Size(5, 5),
                Rows =
                {
                    new TableRow(null, m_button_ok, m_button_cancel)
                }
            };

            m_Dialog.Content = new TableLayout
            {
                Spacing = new Size(5, 5),
                Rows = {
                    new TableRow(dropLayout),
                    new TableRow(searchLayout),
                    new TableRow(ResultLayout),
                    new TableRow(buttonLayout)
                }
            };

            // events
            m_button_ok.Click += (s,e) => m_Dialog.Close(DialogResult.Ok);
            m_button_cancel.Click += (s,e) => m_Dialog.Close(DialogResult.Cancel);
            m_button_search.Click += M_button_search_Click;
            m_tbox_partNumber.KeyDown += M_tbox_partNumber_KeyDown;
            m_tbox_partResult1.KeyUp += M_tbox_partResult_KeyUp;
            m_tbox_partResult2.KeyUp += M_tbox_partResult_KeyUp;

            // show the window
            m_Dialog.ShowModal(RhinoEtoApp.MainWindow);

            // set the values to the current
            windowPosition = m_Dialog.Location;
            UData.PartName = m_tbox_partNumber.Text;
            UData.windowPosition = windowPosition;
            UData.textLine1 = m_tbox_partResult1.Text;
            UData.textLine2 = m_tbox_partResult2.Text;
            UData.labelLayer = m_tbox_layerName.Text;
            UData.label = LabelInfo;
            UData.dotIndex = m_drop_dots.SelectedIndex;
            UData.layerIndex = m_drop_layers.SelectedIndex;
        }

        private void M_tbox_partResult_KeyUp(object sender, KeyEventArgs e)
        {
            var tbox = sender as TextBox;
            var sel = tbox.SelectedText;

            if (e.Key == Keys.P && e.Modifiers == Keys.Control)
                tbox.Text = $"{sel}        <datamatrix,{sel}>";
            else if (e.Key == Keys.D && e.Modifiers == Keys.Control)
                tbox.Text = $"   {sel} CUT DATE: <date,MM/dd/yyyy> <orderid>";
        }

        private void M_tbox_partNumber_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Keys.Enter)
                M_button_search_Click(sender, e);
        }

        private void M_button_search_Click(object sender, EventArgs e)
        {
            // clear the text fields no matter what
            m_tbox_partResult1.Text = "";
            m_tbox_partResult2.Text = "";

            LabelInfo = new gjTools.Commands.OEM_Label(m_tbox_partNumber.Text);
            if (LabelInfo.IsValid)
            {
                m_tbox_partResult1.Text = $"{LabelInfo.drawingNumber}        <datamatrix,{LabelInfo.drawingNumber}>";
                m_tbox_partResult2.Text = $"{LabelInfo.customerPartNumber}   {LabelInfo.partName} CUT DATE: <date,MM/dd/yyyy> <orderid>";
                m_button_search.Enabled = false;
                m_button_ok.Enabled = true;
            }
        }

        /// <summary>
        /// command result getter
        /// </summary>
        public DialogResult CommandResult { get { return m_Dialog.Result; } }
    }
}