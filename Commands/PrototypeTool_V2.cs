using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace gjTools.Commands
{
    public class ProtoValues
    {
        public string Job;
        public DateTime DueDate;
        public string Description;
        public int CutQTY;
        public string Film;
        public string FilmValue;

        public List<string> PartNumbers = new List<string>(10);
        public List<string> PartDescriptions = new List<string>(10);

        public bool CreateLabels;

        public Eto.Drawing.Point windowPosition = new Eto.Drawing.Point(400, 400);

        public ProtoValues()
        {
            var SQL_Rows = new List<int> { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };
            var dStore = SQL.SQLTool.queryDataStore(SQL_Rows);
            var JobInfo = SQL.SQLTool.queryJobSlots()[0];

            Job = JobInfo.job;
            DueDate = DateTime.Parse(JobInfo.due);
            Description = JobInfo.description;
            CutQTY = JobInfo.quantity;
            Film = JobInfo.material;
            FilmValue = "";
            CreateLabels = true;

            for (int i = 0; i < 10; i++)
            {
                PartNumbers.Add(dStore[i].stringValue);
                PartDescriptions.Add("");
            }
        }
    }



    public class PrototypeTool_V2 : Command
    {
        public PrototypeTool_V2()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static PrototypeTool_V2 Instance { get; private set; }

        public override string EnglishName => "PrototypeTool_V2";

        // make the initial proto datablock
        public ProtoValues PData = new ProtoValues();

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var pGui = new GUI.ProtoGui(PData);


            return Result.Success;
        }
    }
}

namespace GUI
{
    using Eto.Forms;
    using Eto.Drawing;
    using Rhino.UI;

    public class ProtoGui
    {
        // main window
        private Dialog<DialogResult> window;
        public gjTools.Commands.ProtoValues PData;

        // user boxes
        private TextBox m_tbox_jobNumber = new TextBox();
        private DateTimePicker m_date = new DateTimePicker { Value = DateTime.Now, Mode = DateTimePickerMode.Date };
        private TextBox m_tbox_description = new TextBox();
        private TextBox m_tbox_cutQty = new TextBox();
        private TextBox m_tbox_Film = new TextBox();

        // part info
        private List<TextBox> m_tbox_partList = new List<TextBox>(10);
        private List<Label> m_label_Partlist = new List<Label>(10);

        // buttons
        private CheckBox m_AddLabels = new CheckBox { Text = "Add Proto Labels too?", Checked = true };
        private Button m_butt_okButt = new Button { Text = "Place Block" };
        private Button m_butt_cancelButt = new Button { Text = "Cancel" };

        public ProtoGui(gjTools.Commands.ProtoValues PartData)
        {
            PData = PartData;

            window = new Dialog<DialogResult>
            {
                Padding = 10,
                Title = "Prototype Utility",
                AutoSize = true,
                Topmost = true,
                Result = DialogResult.Cancel,
                WindowStyle = WindowStyle.Default,
                Location = PData.windowPosition
            };

            m_tbox_jobNumber.Text = PData.Job;
            m_date.Value = PData.DueDate;
            m_tbox_description.Text = PData.Description;
            m_tbox_cutQty.Text = PData.CutQTY.ToString();
            m_tbox_Film.Text = PData.Film;
            FindFilm();

            for (int i = 0; i < 10; i++)
            {
                m_tbox_partList.Add(new TextBox { 
                    ID = i.ToString(), 
                    ShowBorder = false,
                    Width = 120,
                    ToolTip = "Ctrl+D to Copy the Above Part number" 
                });
                m_label_Partlist.Add(new Label {  ID = i.ToString() });

                if (PData.PartNumbers[i].Length > 0)
                {
                    var PartInfo = new gjTools.Commands.OEM_Label(PData.PartNumbers[i]);
                    m_tbox_partList[i].Text = PData.PartNumbers[i];

                    if (PartInfo.IsValid)
                    {
                        m_label_Partlist[i].Text = PartInfo.partName;
                        PData.PartDescriptions[i] = PartInfo.partName;
                    }
                }
            }

            // button events
            m_butt_cancelButt.Click += (s, e) => window.Close(DialogResult.Cancel);
            m_butt_okButt.Click += (s, e) => window.Close(DialogResult.Ok);

            // film event
            m_tbox_Film.LostFocus += (s, e) => FindFilm();

            // Job info update event
            m_tbox_jobNumber.LostFocus += LostFocus_JobInfoUpdate;
            m_date.ValueChanged += LostFocus_JobInfoUpdate;
            m_tbox_description.LostFocus += LostFocus_JobInfoUpdate;
            m_tbox_cutQty.LostFocus += LostFocus_JobInfoUpdate;

            // part number events
            foreach(var p in m_tbox_partList)
            {
                p.LostFocus += PartCheck;
                p.GotFocus += PartFocus;
                p.KeyUp += PartHotKeys;
            }

            // time to setup the form
            var JobLayout = new GroupBox
            {
                Text = "Proto Info",
                Padding = new Padding(5),
                Width = 400,
                Content = new TableLayout
                {
                    Spacing = new Size(5, 2),
                    Rows =
                    {
                        new TableRow(new Label { Text = "Job", TextAlignment = TextAlignment.Right }, m_tbox_jobNumber),
                        new TableRow(new Label { Text = "Due Date", TextAlignment = TextAlignment.Right }, m_date),
                        new TableRow(new Label { Text = "Descrption", TextAlignment = TextAlignment.Right }, m_tbox_description),
                        new TableRow(new Label { Text = "Film", TextAlignment = TextAlignment.Right }, m_tbox_Film),
                        new TableRow(new Label { Text = "Cut Qty", TextAlignment = TextAlignment.Right }, m_tbox_cutQty),
                    }
                }
            };

            var PartLayout = new GroupBox
            {
                Text = "Part Info Slots",
                Padding = new Padding(5),
                Width = 400,
                Content = new TableLayout
                {
                    Spacing = new Size(5, 1),
                    Rows =
                    {
                        new TableRow(m_tbox_partList[0], m_label_Partlist[0]),
                        new TableRow(m_tbox_partList[1], m_label_Partlist[1]),
                        new TableRow(m_tbox_partList[2], m_label_Partlist[2]),
                        new TableRow(m_tbox_partList[3], m_label_Partlist[3]),
                        new TableRow(m_tbox_partList[4], m_label_Partlist[4]),
                        new TableRow(m_tbox_partList[5], m_label_Partlist[5]),
                        new TableRow(m_tbox_partList[6], m_label_Partlist[6]),
                        new TableRow(m_tbox_partList[7], m_label_Partlist[7]),
                        new TableRow(m_tbox_partList[8], m_label_Partlist[8]),
                        new TableRow(m_tbox_partList[9], m_label_Partlist[9])
                    }
                }
            };

            var buttonLayout = new TableLayout
            {
                Padding = new Padding(5, 5, 5, 5),
                Spacing = new Size(5, 5),
                Rows = {
                    new TableRow(m_AddLabels, null, m_butt_okButt, m_butt_cancelButt)
                }
            };

            window.Content = new TableLayout
            {
                Padding = new Padding(4),
                Spacing = new Size(5, 5),
                Rows = {
                    new TableRow(JobLayout),
                    new TableRow(PartLayout),
                    new TableRow(buttonLayout)
                }
            };

            window.ShowModal(RhinoEtoApp.MainWindow);
            PData.windowPosition = window.Location;
        }

        private void LostFocus_JobInfoUpdate(object sender, EventArgs e)
        {
            PData.Job = m_tbox_jobNumber.Text;
            PData.DueDate = (DateTime)m_date.Value;
            PData.Description = m_tbox_description.Text;

            if (!int.TryParse(m_tbox_cutQty.Text, out int num))
            {
                PData.CutQTY = 1;
                m_tbox_cutQty.Text = "1";
            }
            else
            {
                PData.CutQTY = num;
            }
        }

        private void PartHotKeys(object sender, KeyEventArgs e)
        {
            var p = sender as TextBox;
            var indx = int.Parse(p.ID);

            if (indx == 0)
                return;

            if (e.Key == Keys.D && e.Modifiers == Keys.Control)
                p.Text = PData.PartNumbers[indx - 1];
        }

        private void PartFocus(object sender, EventArgs e)
        {
            var p = sender as TextBox;
            p.Selection = new Range<int>(0);

            if (p.Text.Length > 5)
                p.CaretIndex = p.Text.Length - 2;
        }

        private void PartCheck(object sender, EventArgs e)
        {
            var p = sender as TextBox;
            var pn = p.Text.Trim();
            int indx = int.Parse(p.ID);
            PData.PartNumbers[indx] = pn;

            if (p.Text.Length == 0)
            {
                m_label_Partlist[indx].Text = "";
                PData.PartDescriptions[indx] = "";
                return;
            }

            var partInfo = new gjTools.Commands.OEM_Label(pn);
            if (partInfo.IsValid)
            {
                m_label_Partlist[indx].Text = partInfo.partName;
                PData.PartDescriptions[indx] = partInfo.partName;
            }
            else
            {
                m_label_Partlist[indx].Text = "";
                PData.PartDescriptions[indx] = "";
            }
        }

        private void FindFilm()
        {
            var matl = SQL.SQLTool.queryOEMColors(m_tbox_Film.Text);
            if (matl.Count == 0)
                return;

            m_tbox_Film.Text = $"{matl[0].colorNum} - {matl[0].colorName}";
            PData.FilmValue = m_tbox_Film.Text;
        }
    }
}