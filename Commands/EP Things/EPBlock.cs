using System;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace gjTools.Commands.EP_Things
{
    public class EPBlock : Command
    {
        public EPBlock()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static EPBlock Instance { get; private set; }

        public override string EnglishName => "EPBlock";

        // remember the window location
        GUI.EPBlockInfo EPDat = new GUI.EPBlockInfo();

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var window = new GUI.EPBlockGUI(EPDat);

            if (window.CommandResult != Eto.Forms.DialogResult.Ok)
                return Result.Cancel;

            // check if the proper layer exists
            Layer TitleLayer = null;
            foreach(Layer l in doc.Layers)
            {
                if (l.FullPath == "TitleBlock")
                {
                    TitleLayer = l;
                    break;
                }
            }

            // do we need to create the layer?
            if (TitleLayer == null)
                TitleLayer = doc.Layers[doc.Layers.Add("TitleBlock", System.Drawing.Color.Black)];

            // if all is well, make the title block
            CreateTitleBlock(EPDat, doc, TitleLayer);

            return Result.Success;
        }


        /// <summary>
        /// Create the objects
        /// </summary>
        /// <param name="dat"></param>
        /// <param name="doc"></param>
        private void CreateTitleBlock(GUI.EPBlockInfo dat, RhinoDoc doc, Layer lay)
        {
            // TODO
        }
    }
}

namespace GUI
{
    using Eto.Forms;
    using Eto.Drawing;
    using Rhino.UI;

    public class EPBlockInfo
    {
        public Point WindowLocation = new Point(400, 400);
        public string PartNumbers = "";
        public string Version = "A";
        public string PrintType = "Film";
        public string[] Colors = new string[8];
        public string[] ColorDescription = new string[8];
    }

    public class EPBlockGUI
    {
        // main window
        private Dialog<DialogResult> window;

        // Version and print type
        private TextBox m_version = new TextBox { Text = "A" };
        private string[] m_printTypeList = new string[3] { "Film", "Digital Print", "Screen Print" };
        private string[] m_coatingTypeList = new string[3] { "None", "Scren Clear", "Laminate" };
        private DropDown m_printType = new DropDown();
        private DropDown m_coating = new DropDown();

        // Part number slot
        private TextArea m_parts = new TextArea { AllowDrop = true, Height = 50, AcceptsTab = false };

        // color textbox
        private TextBox[] m_colors = new TextBox[8];
        private Label[] m_colorLabels = new Label[8];

        public EPBlockGUI(EPBlockInfo dat)
        {
            window = new Dialog<DialogResult>
            {
                Padding = 10,
                Title = "Prototype Utility",
                AutoSize = true,
                Topmost = true,
                Result = DialogResult.Cancel,
                WindowStyle = WindowStyle.Default,
                Location = dat.WindowLocation
            };

            // fill the dropdown
            m_printType.DataStore = m_printTypeList;
            m_coating.DataStore = m_coatingTypeList;
            m_printType.SelectedIndex = m_coating.SelectedIndex = 0;
            // events of the change
            m_printType.SelectedIndexChanged += Ev_printChanged;
            m_coating.SelectedIndexChanged += (s, e) => ColorLabelUpdate();

            // fill the textbox array and user object
            for (int i = 0; i < m_colors.Length; i++)
            {
                m_colors[i] = new TextBox { ID = i.ToString() };
                m_colorLabels[i] = new Label { Text = (i == 0) ? "Film" : "Unused" };

                // event to check if the color is in the database
                m_colors[i].LostFocus += Ev_OnChange_Color;

                dat.Colors[i] = "";
                dat.ColorDescription[i] = (i == 0) ? "Film" : "";
            }

            // start to make the form
            var VersionLayout = new GroupBox
            {
                Text = "Drawing Info",
                Padding = new Padding(5),
                Width = 400,
                Content = new TableLayout
                {
                    Spacing = new Size(5, 1),
                    Rows =
                    {
                        new TableRow(new Label{Text = "Version"}, m_version),
                        new TableRow(new Label{Text = "Print Type"}, m_printType),
                        new TableRow(new Label{Text = "Coating"}, m_coating)
                    }
                }
            };

            var PartLayout = new GroupBox
            {
                Text = "Part Information",
                Padding = new Padding(5),
                Width = 400,
                Content = new TableLayout
                {
                    Spacing = new Size(5, 1),
                    Rows =
                    {
                        new TableRow(m_parts)
                    }
                }
            };

            var ColorLayout = new GroupBox
            {
                Text = "Color Information",
                Padding = new Padding(5),
                Width = 400,
                Content = new TableLayout
                {
                    Spacing = new Size(5, 1),
                    Rows =
                    {
                        new TableRow(m_colorLabels[0], m_colors[0]),
                        new TableRow(m_colorLabels[1], m_colors[1]),
                        new TableRow(m_colorLabels[2], m_colors[2]),
                        new TableRow(m_colorLabels[3], m_colors[3]),
                        new TableRow(m_colorLabels[4], m_colors[4]),
                        new TableRow(m_colorLabels[5], m_colors[5]),
                        new TableRow(m_colorLabels[6], m_colors[6]),
                        new TableRow(m_colorLabels[7], m_colors[7])
                    }
                }
            };

            // contruct the buttons to continue
            Button Butt_OK = new Button { Text = "OK" };
            Button Butt_Cancel = new Button { Text = "Cancel" };
            Butt_Cancel.Click += (s, e) => window.Close(DialogResult.Cancel);
            Butt_OK.Click += (s, e) => window.Close(DialogResult.Ok);
            var buttonLayout = new TableLayout
            {
                Padding = new Padding(1,1,1,1),
                Spacing = new Size(5, 5),
                Rows = {
                    new TableRow(null, Butt_OK, Butt_Cancel)
                }
            };

            // add everything to the window
            window.Content = new TableLayout
            {
                Padding = new Padding(4),
                Spacing = new Size(5, 5),
                Rows = {
                    new TableRow(VersionLayout),
                    new TableRow(PartLayout),
                    new TableRow(ColorLayout),
                    new TableRow(buttonLayout)
                }
            };

            window.ShowSemiModal(RhinoDoc.ActiveDoc, RhinoEtoApp.MainWindow);
            UpdateInfoObject(dat);
        }

        private void UpdateInfoObject(EPBlockInfo dat)
        {
            dat.WindowLocation = window.Location;
            dat.Version = "Version " + m_version.Text;
            dat.PartNumbers = m_parts.Text;
            dat.PrintType = m_printTypeList[m_printType.SelectedIndex];

            for (int i = 0; i < m_colors.Length; i++)
            {
                dat.Colors[i] = m_colors[i].Text;
                dat.ColorDescription[i] = m_colorLabels[i].Text;
            }
        }

        private void ColorLabelUpdate()
        {
            int ColorCount = 1;

            for (int i = 1; i < m_colors.Length; i++)
            {
                if (m_colors[i].Text.Length == 0)
                {
                    m_colorLabels[i].Text = "Unused";
                    continue;
                }

                m_colorLabels[i].Text = $"Color {ColorCount}";
                ColorCount++;
            }
        }

        private void Ev_printChanged(object sender, EventArgs e)
        {
            if (m_printType.SelectedIndex == 0)
            {
                m_colorLabels[0].Text = "Film";
                return;
            }
            
            // all other ones are the same
            m_colorLabels[0].Text = "Base Film";
        }

        private void Ev_OnChange_Color(object sender, EventArgs e)
        {
            TextBox tb = (TextBox)sender;

            // update the labels
            ColorLabelUpdate();

            if (tb.Text.Length == 0)
                return;

            var matl = SQL.SQLTool.queryOEMColors(tb.Text);
            if (matl.Count == 0)
                return;

            tb.Text = matl[0].colorNum + " - " + matl[0].colorName;
        }

        // result of the form
        public DialogResult CommandResult { get { return window.Result; } }
    }
}