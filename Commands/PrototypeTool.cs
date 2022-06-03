using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;

namespace gjTools.Commands
{
    /// <summary>
    /// Pulls and Saves the values related to prototypes
    /// </summary>
    public class ProtoValues
    {
        public RhinoDoc document;
        public Layer parentLayer;

        public string Job;
        public DateTime DueDate;
        public string Description;
        public int CutQTY;
        public string Film;

        public List<string> PartNumbers = new List<string>(10);
        public List<string> PartDescriptions = new List<string>(10);

        public bool CreateLabels;

        public Eto.Drawing.Point windowPosition = new Eto.Drawing.Point(400, 400);

        public ProtoValues()
        {
            CreateLabels = true;
            GetFreshDataBaseValues();
        }

        public void GetFreshDataBaseValues()
        {
            var SQL_Rows = new List<int> { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };
            var dStore = SQL.SQLTool.queryDataStore(SQL_Rows);
            var JobInfo = SQL.SQLTool.queryJobSlots()[0];

            Job = JobInfo.job;
            DueDate = DateTime.Parse(JobInfo.due);
            Description = JobInfo.description;
            CutQTY = JobInfo.quantity;
            Film = JobInfo.material;

            PartNumbers.Clear();
            PartDescriptions.Clear();

            for (int i = 0; i < 10; i++)
            {
                PartNumbers.Add(dStore[i].stringValue);
                PartDescriptions.Add("");
            }
        }

        public void WriteJobValueToDatabase()
        {
            var SQL_Rows = new List<int> { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };

            // add the parts to the database
            for (int i = 0; i < 10; i++)
                SQL.SQLTool.updateDataStore(new SQL.DataStore(SQL_Rows[i], PartNumbers[i].ToUpper(), 0, 0.0, "PrototypeTool"));

            // update the job slot information
            SQL.SQLTool.updateJobSlot(new SQL.JobSlot(1, Job, DueDate.ToShortDateString(), Description, CutQTY, Film));
        }
    }

    /// <summary>
    /// Retreives single oem part information
    /// </summary>
    public struct OEM_Label
    {
        public List<string> rawLines;
        public string drawingNumber;
        public bool IsValid;

        public OEM_Label(string OEMPartNumber)
        {
            rawLines = new List<string>();
            drawingNumber = OEMPartNumber.ToUpper();
            IsValid = false;
            IsValid = GetData();
        }

        private bool GetData()
        {
            string folderPath = "\\\\spi\\art\\PROTOTYPE\\AutoCAD_XML\\";
            if (System.IO.File.Exists(folderPath + drawingNumber + ".xml") && drawingNumber.Length > 4)
            {
                var XMLfile = System.IO.File.OpenText(folderPath + drawingNumber + ".xml");
                while (true)
                {
                    string Line = XMLfile.ReadLine();
                    if (Line == "<AUTOCAD>")
                        continue;
                    if (Line == "</AUTOCAD>" || Line == null)
                        break;

                    rawLines.Add(Line);
                }
                return true;
            }
            else
                return false;
        }

        public string partName { get { return rawLines[1]; } }
        public string year { get { return rawLines[2]; } }
        public string customer { get { return rawLines[3]; } }
        public string process { get { return rawLines[4]; } }
        public string partsPerUnit { get { return rawLines[5]; } }
        public string DOC { get { return rawLines[6]; } }
        public string path { get { return rawLines[7]; } }
        public string customerPartNumber
        {
            get
            {
                if (rawLines.Count > 9)
                    return rawLines[9];
                else
                    return "";
            }
        }
    }


    public class PrototypeTool : Command
    {
        public PrototypeTool()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static PrototypeTool Instance { get; private set; }

        public override string EnglishName => "ProtoUtility";

        // make the initial proto datablock
        public ProtoValues PData = new ProtoValues();

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // pull fresh values
            PData.GetFreshDataBaseValues();

            // assign the fresh document definition
            PData.document = doc;

            // show the form and collect the values
            var pGui = new GUI.ProtoGui(PData);

            // Update the database values
            PData.WriteJobValueToDatabase();

            if (pGui.CommandResult != Eto.Forms.DialogResult.Ok)
                return Result.Cancel;

            // get the nestbox entity to place titleblock
            if (RhinoGet.GetOneObject("Select NestBox", false, ObjectType.AnyObject, out ObjRef nestBox) != Result.Success)
                return Result.Cancel;

            // check if the selection is a nestbox
            if (nestBox.Object().Name != "NestBox")
            {
                RhinoApp.WriteLine("Selection Was not a NestBox object, cancelling...");
                return Result.Cancel;
            }

            CreateProtoTitleBlock(nestBox);

            // See if the user wanted labels
            if (pGui.PData.CreateLabels)
                CreateProtoLabels();

            return Result.Success;
        }


        /// <summary>
        /// The standard title block for prototypes
        /// </summary>
        /// <param name="nestBox"></param>
        private void CreateProtoTitleBlock(ObjRef nestBox)
        {
            var dt = new DrawTools(PData.document);
            int ds = dt.StandardDimstyle();
            BoundingBox nestBoxBB = nestBox.Geometry().GetBoundingBox(true);
            Point3d pt = nestBoxBB.GetCorners()[3];
                pt.Y += 0.5;

            TextEntity mainblock = dt.AddText(
                $"Job: {PData.Job}  Due: {PData.DueDate.ToShortDateString()}\n{PData.Description}\n\n",
                pt, ds, 1, 0, 0, 6);

            // include the parts in the block
            for (int i = 0; i < 10; i++)
            {
                if (PData.PartNumbers[i].Length > 5)
                {
                    mainblock.PlainText += $"\n{PData.PartNumbers[i]} - {PData.PartDescriptions[i]}";
                }
            }
            
            BoundingBox mainBlockBB = mainblock.GetBoundingBox(true);
            pt = mainBlockBB.GetCorners()[2];
            pt.X += 2;

            TextEntity cutBlock = dt.AddText(
                $"{PData.Film}\nCut: {PData.CutQTY}x",
                pt, ds);

            mainBlockBB.Union(cutBlock.GetBoundingBox(true));

            // now scale the text to the proper width
            var xForm = Transform.Scale(mainBlockBB.Corner(true, true, true),
                (nestBoxBB.GetEdges()[0].Length * 0.7) / mainBlockBB.GetEdges()[0].Length);
            
            mainblock.Transform(xForm, PData.document.DimStyles[ds]);
            cutBlock.Transform(xForm, PData.document.DimStyles[ds]);

            // find the parent layer
            PData.parentLayer = PData.document.Layers[nestBox.Object().Attributes.LayerIndex];
            PData.parentLayer = PData.document.Layers.FindId(PData.parentLayer.ParentLayerId);

            // add the items to the document
            ObjectAttributes attr = new ObjectAttributes { LayerIndex = PData.parentLayer.Index };
            PData.document.Objects.AddText(mainblock, attr);
            PData.document.Objects.AddText(cutBlock, attr);
        }

        /// <summary>
        /// Creates the printed labels
        /// </summary>
        public void CreateProtoLabels()
        {
            var doc = PData.document;
            var lt = new LayerTools(doc);
            var dt = new DrawTools(doc);

            // create the base text entities to reuse as needed
            TextEntity baseDocText = dt.AddText("", Point3d.Origin, 0.5, bold: true, horiz: TextHorizontalAlignment.Center, vert: TextVerticalAlignment.Middle);
            TextEntity baseLabelText = dt.AddText("", Point3d.Origin, 0.15);
            TextEntity baseLGLabelText = dt.AddText("", Point3d.Origin, 0.75, horiz: TextHorizontalAlignment.Center, vert: TextVerticalAlignment.Middle);

            // need the proper layer and base attributes
            Layer labelLayer = lt.CreateLayer("C_TEXT", PData.parentLayer.Name);
            ObjectAttributes attrLGLabel = new ObjectAttributes { LayerIndex = PData.parentLayer.Index };
            ObjectAttributes attrLabel = new ObjectAttributes { LayerIndex = labelLayer.Index };

            // cycle through the parts and place them in the document
            for (int i = 0; i < PData.PartNumbers.Count; i++)
            {
                OEM_Label partInfo = new OEM_Label(PData.PartNumbers[i]);

                if (!partInfo.IsValid)
                    continue;
                
                if (RhinoGet.GetPoint($"Place Label for {partInfo.drawingNumber} - {partInfo.partName}", false, out Point3d pt) != Result.Success)
                    return;

                pt.Z = 0;
                Plane ptPlain = new Plane(pt, Vector3d.ZAxis);
                bool sets = partInfo.partsPerUnit.Contains("Sets") || partInfo.partsPerUnit.Contains("Usage");

                // replace the text in the base text
                baseDocText.PlainText = partInfo.DOC;
                baseLabelText.PlainText = $"{partInfo.year} {partInfo.customer}\n{partInfo.partName}\n{partInfo.drawingNumber}";
                baseLGLabelText.PlainText = $"{partInfo.drawingNumber} - {partInfo.partName}";

                // Create the doc number as Geometry
                baseDocText.Plane = ptPlain;
                var DocCurves = new List<Curve>(baseDocText.Explode());

                // add a round box corner box around the doc number
                BoundingBox BB = BoundingBox.Empty;
                for (int o = 0; o < DocCurves.Count; o++)
                {
                    DocCurves[o] = DocCurves[o].ToPolyline(0.2, 0.28, 0, 0);
                    BB.Union(DocCurves[o].GetBoundingBox(true));
                }
                BB.Inflate(0.06);
                BB.Min = new Point3d(BB.Min.X, BB.Min.Y, 0);
                BB.Max = new Point3d(BB.Max.X, BB.Max.Y, 0);
                
                NurbsCurve box = NurbsCurve.Create(true, 1, new List<Point3d>(BB.GetCorners()).GetRange(0, 4));
                DocCurves.Add(NurbsCurve.CreateFilletCornersCurve(box, 0.06, 0.01, 0.01));

                // Move the part description
                baseLabelText.Plane = new Plane(BB.Corner(false, false, true), Vector3d.ZAxis);
                baseLabelText.Translate(0.06, 0, 0);

                // move the label to the point chosen
                baseLGLabelText.Plane = ptPlain;
                baseLGLabelText.Translate(0, 1.5, 0);

                // create groups
                attrLabel.RemoveFromAllGroups();
                attrLabel.AddToGroup(doc.Groups.Add());

                baseLabelText.PlainText += sets ? " - RH" : "";

                // add the object to the document
                foreach (var c in DocCurves)
                    doc.Objects.AddCurve(c, attrLabel);
                doc.Objects.AddText(baseLabelText, attrLabel);
                doc.Objects.AddText(baseLGLabelText, attrLGLabel);

                // if the LH side is needed
                if (!sets)
                    continue;

                baseLabelText.PlainText = baseLabelText.PlainText.Replace(" - RH", " - LH");
                baseLabelText.Translate(0, -1, 0);
                attrLabel.RemoveFromAllGroups();
                attrLabel.AddToGroup(doc.Groups.Add());

                // add the new label text
                doc.Objects.AddText(baseLabelText, attrLabel);
                foreach (var c in DocCurves)
                {
                    c.Translate(0, -1, 0);
                    doc.Objects.AddCurve(c, attrLabel);
                }
            }
        }

        /// <summary>
        /// creates a production style datamatrix label intended for E&P on target Layer
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="part"></param>
        /// <param name="parentLayer"></param>
        /// <returns>True if the label was placed, False if user cancelled</returns>
        public bool PlaceProductionLabel(RhinoDoc doc, OEM_Label part, Layer targetLayer, Point3d pt)
        {
            if (!part.IsValid)
                return false;

            var dt = new DrawTools(doc);
            var ds = dt.StandardDimstyle();

            // make text
            var Etxt = new List<TextEntity> {
                dt.AddText(string.Format("{0}        <datamatrix,{0}>", part.drawingNumber), pt, ds, 0.16, 0, 3, 6),
                dt.AddText(string.Format("{0}   {1} CUT DATE: <date,MM/dd/yyyy> <orderid>", part.customerPartNumber, part.partName),
                    new Point3d(pt.X, pt.Y - 0.25, 0), ds, 0.14, 0, 3, 0)
            };

            // add to the doc
            foreach (var t in Etxt)
            {
                Guid id = doc.Objects.AddText(t);
                var obj = doc.Objects.FindId(id);
                obj.Attributes.LayerIndex = targetLayer.Index;
                obj.CommitChanges();
            }

            return true;
        }
    }
}

namespace GUI
{
    using Eto.Forms;
    using Eto.Drawing;
    using Rhino.UI;

    /// <summary>
    /// GUI made specifically for the prototype rhino command
    /// </summary>
    public class ProtoGui
    {
        // main window
        private Dialog<DialogResult> window;
        public gjTools.Commands.ProtoValues PData;

        // user boxes
        private TextBox m_tbox_jobNumber = new TextBox();
        private DateTimePicker m_date = new DateTimePicker { Value = DateTime.Now, Mode = DateTimePickerMode.Date };
        private TextBox m_tbox_description = new TextBox();
        private Label m_label_cutQty = new Label { Text = "Cut Qty(1)", TextAlignment = TextAlignment.Right };
        private Slider m_slid_cutQty = new Slider { MinValue = 1, MaxValue = 12, Value = 1, SnapToTick = true, TickFrequency = 1, Orientation = Orientation.Horizontal };
        private TextBox m_tbox_Film = new TextBox();

        // part info
        private List<TextBox> m_tbox_partList = new List<TextBox>(10);
        private List<Label> m_label_Partlist = new List<Label>(10);

        // buttons
        private CheckBox m_AddLabels = new CheckBox { Text = "Add Proto Labels too?", Checked = true };
        private Button m_butt_okButt = new Button { Text = "Place Block" };
        private Button m_butt_clearParts = new Button { Text = "Clear Parts", ToolTip = "Double Click" };
        private Button m_butt_cancelButt = new Button { Text = "Cancel" };


        /// <summary>
        /// Show the window
        /// </summary>
        /// <param name="PartData"></param>
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
            m_label_cutQty.Text = $"Cut {PData.CutQTY}x";
            m_slid_cutQty.Value = PData.CutQTY;
            m_AddLabels.Enabled = PData.CreateLabels;
            m_tbox_Film.Text = PData.Film;
            FindFilm();

            for (int i = 0; i < 10; i++)
            {
                m_tbox_partList.Add(new TextBox { 
                    ID = i.ToString(), 
                    ShowBorder = false,
                    Width = 120,
                    AutoSelectMode = AutoSelectMode.Never,
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
            m_AddLabels.EnabledChanged += (s, e) => PData.CreateLabels = m_AddLabels.Enabled;
            m_butt_clearParts.MouseDoubleClick += clearParts_MouseDoubleClick;

            // film event
            m_tbox_Film.LostFocus += (s, e) => FindFilm();

            // Job info update event
            m_tbox_jobNumber.LostFocus += LostFocus_JobInfoUpdate;
            m_date.ValueChanged += LostFocus_JobInfoUpdate;
            m_tbox_description.LostFocus += LostFocus_JobInfoUpdate;
            m_slid_cutQty.ValueChanged += LostFocus_JobInfoUpdate;

            // part number events
            foreach(var p in m_tbox_partList)
            {
                p.KeyUp += PartHotKeys;
                p.LostFocus += PartCheck;
                p.GotFocus += PartFocus;
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
                        new TableRow(m_label_cutQty, m_slid_cutQty)
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
                    new TableRow(m_AddLabels, null, m_butt_clearParts, m_butt_okButt, m_butt_cancelButt)
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
            PData.CreateLabels = (bool)m_AddLabels.Checked;
            PData.windowPosition = window.Location;
        }

        private void clearParts_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            for (int i = 1; i < 10; i++)
            {
                m_tbox_partList[i].Text = "";
                m_label_Partlist[i].Text = "";
                PData.PartNumbers[i] = "";
                PData.PartDescriptions[i] = "";
            }
        }

        private void LostFocus_JobInfoUpdate(object sender, EventArgs e)
        {
            PData.Job = m_tbox_jobNumber.Text;
            PData.DueDate = (DateTime)m_date.Value;
            PData.Description = m_tbox_description.Text;

            PData.CutQTY = m_slid_cutQty.Value;
            m_label_cutQty.Text = $"Cut {m_slid_cutQty.Value}x";
        }

        private void PartHotKeys(object sender, KeyEventArgs e)
        {
            var p = sender as TextBox;
            var indx = int.Parse(p.ID);

            if (indx == 0)
                return;

            if (e.Key == Keys.D && e.Modifiers == Keys.Control)
            {
                p.Text = PData.PartNumbers[indx - 1];

                if (p.Text.Length > 10)
                    p.Selection = new Range<int>(p.Text.Length - 4, p.Text.Length - 2);
            }
        }

        private void PartFocus(object sender, EventArgs e)
        {
            var p = sender as TextBox;

            if (p.Text.Length > 10)
                p.Selection = new Range<int>(p.Text.Length - 4, p.Text.Length - 2);
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
            {
                PData.Film = m_tbox_Film.Text;
                return;
            }

            string hyphen = (matl[0].colorName.Length > 2) ? " - " : "";
            m_tbox_Film.Text = $"{matl[0].colorNum}{hyphen}{matl[0].colorName}";
            PData.Film = m_tbox_Film.Text;
        }

        public DialogResult CommandResult { get { return window.Result; } }
    }
}