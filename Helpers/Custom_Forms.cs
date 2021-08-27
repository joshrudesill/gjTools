using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Rhino.UI;

namespace gjTools.Commands
{
    /// <summary>
    /// Intended for the PDF command
    /// </summary>
    public class DualListDialog
    {
        private string _winTitle;
        private string _LabelSingle;
        private List<string> _singleSelect;
        private string _labelMulti;
        private List<string> _multiSelect;

        private Dialog<DialogResult> window;
        private ListBox optionList;
        private GridView multiList;
        private Button okButt;
        private Button cancelButt;
        private Label userTextLabel;
        private TextBox userText;

        /// <summary>
        /// used to swap out the info in the multi select box
        /// </summary>
        public List<string> multiSelectAlternate;
        /// <summary>
        /// Store this in the command to remember where the window should be drawn
        /// </summary>
        public Point windowPosition;
        /// <summary>
        /// Set the index of the multiselect box
        /// </summary>
        public int multiDefaultIndex = -1;
        private List<int> multiSelectIndex = new List<int>();
        /// <summary>
        /// Set this if you would like to limit the multiselect
        /// </summary>
        public bool multiSelect_selectMultiple = true;
        /// <summary>
        /// Set the index of the single select box
        /// </summary>
        public int singleDefaultIndex = -1;
        /// <summary>
        /// Show a box for text entry
        /// </summary>
        public bool showTextBox = false;
        /// <summary>
        /// Label for the textbox
        /// </summary>
        public string txtBoxLabel = "";
        /// <summary>
        /// Default value for the textbox
        /// </summary>
        public string txtBoxDefault = "";


        public DualListDialog(string winTitle, string LabelSingle, List<string> singleSelect, string labelMulti, List<string> multiSelect)
        {
            _winTitle = winTitle;
            _LabelSingle = LabelSingle;
            _singleSelect = singleSelect;
            _labelMulti = labelMulti;
            _multiSelect = multiSelect;
            multiSelectAlternate = multiSelect;
        }

        public void ShowForm()
        {
            window = new Dialog<DialogResult>
            {
                Padding = 10,
                Title = _winTitle,
                Topmost = true,
                Result = DialogResult.Cancel,
                WindowStyle = WindowStyle.Default,
                Location = windowPosition
            };

            optionList = new ListBox
            {
                Height = 350,
                Width = 240,
                DataStore = _singleSelect,
                TabIndex = 0
            };

            // Make the multilist
            MultiList();
            multiList.TabIndex = 1;
            multiList.KeyDown += Window_KeyDown;

            // Make the textbox and hide it
            userText = new TextBox
            {
                Text = txtBoxDefault,
                Width = 210,
                Enabled = false,
                ShowBorder = false,
                BackgroundColor = window.BackgroundColor
            };
            userTextLabel = new Label { Text = "", TextAlignment = TextAlignment.Right };

            // unhide if needed
            if (showTextBox)
            {
                userText.Enabled = true;
                userText.ShowBorder = true;
                userText.BackgroundColor = new TextBox().BackgroundColor;
                userTextLabel.Text = txtBoxLabel;
            }

            // set the default selection
            multiList.SelectedRow = multiDefaultIndex;
            if (singleDefaultIndex >= 0)
                optionList.SelectedIndex = singleDefaultIndex;

            // buttons
            okButt = new Button { Text = "OK", Width = 90 };
            cancelButt = new Button { Text = "Cancel", Width = 90 };

            // Events go here
            optionList.KeyDown += Window_KeyDown;
            multiList.KeyDown += Window_KeyDown;
            window.KeyDown += Window_KeyDown;
            optionList.SelectedIndexChanged += OptionList_SelectedIndexChanged;
            multiList.SelectedRowsChanged += MultiList_SelectedRowsChanged;
            okButt.Click += (s, e) => window.Close(DialogResult.Ok);
            cancelButt.Click += (s, e) => window.Close(DialogResult.Cancel);

            // disable things if they are unset intially
            okButtEnabled();
            if (optionList.SelectedIndex == -1)
                multiList.Enabled = false;

            // Make the layouts
            var selectLayout = new TableLayout
            {
                Padding = new Padding(5, 5, 5, 5),
                Spacing = new Size(5, 5),
                Rows = {
                new TableRow(new Label { Text = _LabelSingle }, new Label { Text = _labelMulti }),
                new TableRow(optionList, multiList)
            }
            };

            var buttonLayout = new TableLayout
            {
                Padding = new Padding(5, 5, 5, 5),
                Spacing = new Size(5, 5),
                Rows =
            {
                new TableRow(userTextLabel, userText, null, okButt, cancelButt)
            }
            };

            window.Content = new TableLayout
            {
                Spacing = new Size(5, 5),
                Rows =
            {
                new TableRow(selectLayout),
                new TableRow(buttonLayout)
            }
            };

            window.ShowModal(RhinoEtoApp.MainWindow);
            windowPosition = window.Location;
        }

        private void MultiList_SelectedRowsChanged(object sender, EventArgs e)
        {
            okButtEnabled();
        }

        private void okButtEnabled()
        {
            var mlSelectedRowCount = new List<int>(multiList.SelectedRows).Count;
            if (mlSelectedRowCount > 0 && optionList.SelectedIndex >= 0)
                okButt.Enabled = true;
            else
                okButt.Enabled = false;
        }

        private void OptionList_SelectedIndexChanged(object sender, EventArgs e)
        {
            okButtEnabled();

            // regular event stuff
            if (optionList.SelectedIndex == -1)
            {
                multiSelectIndex = new List<int>(multiList.SelectedRows);
                multiList.Enabled = false;
            }
            else if (_singleSelect[optionList.SelectedIndex] == "Mylar Color" ||
                _singleSelect[optionList.SelectedIndex] == "Mylar NonColor")
            {
                GridListConvert(multiSelectAlternate);
                multiList.Enabled = true;
                multiList.SelectedRows = multiSelectIndex;
            }
            else
            {
                GridListConvert(_multiSelect);
                multiList.Enabled = true;
                multiList.SelectedRows = multiSelectIndex;
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Keys.Enter && okButt.Enabled)
                window.Close(DialogResult.Ok);

            if (e.Key == Keys.Escape)
                window.Close(DialogResult.Cancel);
        }

        private void MultiList()
        {
            multiList = new GridView
            {
                Height = 350,
                Width = 240,
                ShowHeader = false,
                AllowMultipleSelection = multiSelect_selectMultiple
            };
            multiList.Columns.Add(new GridColumn
            {
                Editable = false,
                DataCell = new TextBoxCell(0),
                Width = 220
            });

            var scrollBar = new Scrollable
            {
                Border = BorderType.Line,
                Content = multiList,
                ExpandContentWidth = true
            };

            GridListConvert(_multiSelect);
        }

        private void GridListConvert(List<string> input)
        {
            var newMultiList = new List<List<string>>();
            foreach (var s in input)
                newMultiList.Add(new List<string> { s });

            multiList.DataStore = newMultiList;
        }


        // User functions here
        public DialogResult CommandResult() { return window.Result; }
        public string GetSingleValue() { return optionList.SelectedKey; }
        public List<string> GetMultiSelectValue()
        {
            var ds = multiList.SelectedRows;
            var outlist = new List<string>();

            foreach (var s in ds)
                outlist.Add(_multiSelect[s]);

            return outlist;
        }
        public List<string> GetMultiSelectAlternateValue()
        {
            var ds = multiList.SelectedRows;
            var outlist = new List<string>();

            foreach (var s in ds)
                outlist.Add(multiSelectAlternate[s]);

            return outlist;
        }
    }

    public class LiebingerDialog
    {
        private Dialog<DialogResult> window;
        public Point windowPosition = new Point(400, 400);
        public string defaultPartNumber = "";
        public OEM_Label LabelInfo;

        private Button searchButt = new Button { Text = "Search", Width = 100 };
        private Button okButt = new Button { Text = "Place", Width = 90, Enabled = false };
        private Button cancelButt = new Button { Text = "Cancel", Width = 90 };

        private TextBox partNumber;

        private Label partNumLabel = new Label { Text = "PartNumber" };
        private Label searchResult = new Label { Text = "" };
        private Label partLine = new Label { Text = "" };
        private Label descLine = new Label { Text = "" };

        public void ShowForm()
        {
            window = new Dialog<DialogResult>
            {
                Padding = 10,
                Title = "Liebinger Label",
                AutoSize = true,
                Topmost = true,
                Result = DialogResult.Cancel,
                WindowStyle = WindowStyle.Default,
                Location = windowPosition
            };

            // Make the textbox
            partNumber = new TextBox
            {
                Text = defaultPartNumber,
                Width = 180
            };

            // Make events
            okButt.Click += (s, e) => window.Close(DialogResult.Ok);
            cancelButt.Click += (s, e) => window.Close(DialogResult.Cancel);
            searchButt.Click += SearchButt_Click;
            partNumber.KeyDown += PartNumber_KeyDown;
            partNumber.TextChanged += PartNumber_TextChanged;

            // Make the layouts
            var searchLayout = new TableLayout
            {
                Rows = {
                    new TableRow(partNumLabel, null, searchResult)
                }
            };

            var userLayout = new TableLayout
            {
                Padding = new Padding(5, 5, 5, 5),
                Spacing = new Size(5, 5),
                Rows = {
                    new TableRow(partNumber, searchButt, null)
                }
            };

            var descriptionLayout = new TableLayout
            {
                Padding = new Padding(5, 5, 5, 5),
                Spacing = new Size(5, 5),
                Rows = {
                    new TableRow(partLine),
                    new TableRow(descLine)
                }
            };

            var buttonLayout = new TableLayout
            {
                Padding = new Padding(5, 5, 5, 5),
                Spacing = new Size(5, 5),
                Rows = {
                    new TableRow(null, okButt, cancelButt)
                }
            };

            window.Content = new TableLayout
            {
                Spacing = new Size(5, 5),
                Rows = {
                    new TableRow(searchLayout),
                    new TableRow(userLayout),
                    new TableRow(descriptionLayout),
                    new TableRow(buttonLayout)
                }
            };

            window.ShowModal(RhinoEtoApp.MainWindow);
            windowPosition = window.Location;
        }

        private void PartNumber_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Keys.Enter)
            {
                if (searchButt.Enabled)
                    SearchButt_Click(sender, e);
                else
                    window.Close(DialogResult.Ok);
            }
            else if (e.Key == Keys.Escape)
            {
                window.Close(DialogResult.Cancel);
            }
        }

        private void PartNumber_TextChanged(object sender, EventArgs e)
        {
            searchButt.Enabled = true;
            searchResult.Text = "";
            partLine.Text = "";
            descLine.Text = "";
            okButt.Enabled = false;
        }

        private void SearchButt_Click(object sender, EventArgs e)
        {
            LabelInfo = new OEM_Label(partNumber.Text);
            if (LabelInfo.IsValid)
            {
                searchResult.Text = "Found";
                partLine.Text = $"{LabelInfo.drawingNumber}        <datamatrix,{LabelInfo.drawingNumber}>";
                descLine.Text = $"{LabelInfo.customerPartNumber}   {LabelInfo.partName} CUT DATE: <date,MM/dd/yyyy> <orderid>";
                searchButt.Enabled = false;
                okButt.Enabled = true;
            }
            else
            {
                searchResult.Text = "Not Found";
                partLine.Text = "";
                descLine.Text = "";
            }
        }

        public DialogResult CommandResult() { return window.Result; }

        public string GetCurrentPartNumber() { return partNumber.Text; }
    }

    public class PrototypeDialog
    {
        private Dialog<DialogResult> window;
        
        private Button clearValuesButt = new Button { Text = "Clear Values", ToolTip = "Double-Click Me" };
        private Button okButt = new Button { Text = "Place Block" };
        private Button cancelButt = new Button { Text = "Cancel" };

        private CheckBox AddLabels = new CheckBox { Text = "Add Proto Labels too?", Checked = true };

        private GridView protoLabels = new GridView
        {
            Width = 95,
            ShowHeader = false,
            Border = BorderType.None,
            GridLines = GridLines.Horizontal,
            AllowMultipleSelection = false,
            Enabled = false,
            BackgroundColor = Colors.LightGrey,
            Columns = {
                new GridColumn { 
                    Editable = false,
                    DataCell = new TextBoxCell(0) { TextAlignment = TextAlignment.Right },
                    Width = 90
                },
            }
        };
        private GridView protoUserVals = new GridView
        {
            Width = 115,
            ShowHeader = false,
            Border = BorderType.Line,
            GridLines = GridLines.Horizontal,
            AllowMultipleSelection = false,
            Columns = {
                new GridColumn {
                    Editable = true,
                    DataCell = new TextBoxCell(0) { TextAlignment = TextAlignment.Center },
                    Width = 110
                },
            }
        };
        private GridView protoResults = new GridView
        {
            Width = 275,
            ShowHeader = false,
            Border = BorderType.None,
            GridLines = GridLines.Horizontal,
            AllowMultipleSelection = false,
            Enabled = false,
            BackgroundColor = Colors.LightGrey,
            Columns = {
                new GridColumn {
                    Editable = false,
                    DataCell = new TextBoxCell(0) { TextAlignment = TextAlignment.Left },
                    Width = 270
                },
            }
        };

        private List<string> labels = new List<string>
        {
            "Job",
            "Due Date",
            "Description",
            "Cut Qty",
            "Film",
            "Part #1",
            "Part #2",
            "Part #3",
            "Part #4",
            "Part #5",
            "Part #6",
            "Part #7",
            "Part #8",
            "Part #9",
            "Part #10"
        };
        /// <summary>
        /// array length is 15
        /// </summary>
        public List<string> userInfo = new List<string>();
        public Point windowPosition;

        /// <summary>
        /// Form gets written to screen
        /// </summary>
        public void ShowForm()
        {
            window = new Dialog<DialogResult>
            {
                Padding = 10,
                Title = "Liebinger Label",
                AutoSize = true,
                Topmost = true,
                Result = DialogResult.Cancel,
                WindowStyle = WindowStyle.Default,
                Location = windowPosition
            };

            // events here
            protoUserVals.CellEdited += ProtoInfo_CellEdited;
            clearValuesButt.MouseDoubleClick += ClearValuesButt_MouseDoubleClick;
            okButt.Click += (s, e) => window.Close(DialogResult.Ok);
            cancelButt.Click += (s, e) => window.Close(DialogResult.Cancel);

            var protoLayout = new TableLayout
            {
                Padding = new Padding(5, 5, 5, 5),
                Spacing = new Size(0, 5),
                Rows = {
                    new TableRow(protoLabels, protoUserVals, protoResults)
                }
            };

            var buttonLayout = new TableLayout
            {
                Padding = new Padding(5, 5, 5, 5),
                Spacing = new Size(5, 5),
                Rows = {
                    new TableRow(AddLabels, null, clearValuesButt, okButt, cancelButt)
                }
            };

            GridAssembler();
            window.Content = new TableLayout
            {
                Spacing = new Size(5, 5),
                Rows = {
                    new TableRow(protoLayout),
                    new TableRow(buttonLayout)
                }
            };

            window.ShowModal(RhinoEtoApp.MainWindow);
            windowPosition = window.Location;
        }

        private void ClearValuesButt_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            userInfo.Clear();
            GridAssembler();
        }

        //  external user data extractors
        public bool PlaceLabels
        {
            get
            {
                return AddLabels.Checked ?? true;
            }
        }
        public List<string> GetUserValues
        {
            get
            {
                var user_ds = protoUserVals.DataStore as List<List<string>>;
                var user_1d = new List<string>();
                foreach (var d in user_ds)
                    user_1d.Add(d[0]);
                return user_1d;
            }
        }
        public List<string> GetResultValues
        {
            get
            {
                var res_ds = protoResults.DataStore as List<List<string>>;
                var res_1d = new List<string>();
                foreach (var d in res_ds)
                    res_1d.Add(d[0]);
                return res_1d;
            }
        }
        public DialogResult CommandResult
        {
            get
            {
                return window.Result;
            }
        }

        // Inner class tools
        private void GridAssembler()
        {
            var lab = new List<List<string>>();
            var usr = new List<List<string>>();
            var res = new List<List<string>>();
            if (userInfo.Count == 0)
                for (var i = 0; i < labels.Count; i++)
                {
                    lab.Add(new List<string> { labels[i] });
                    usr.Add(new List<string> { "" });
                    res.Add(new List<string> { "" });
                }
            else
                for (var i = 0; i < labels.Count; i++)
                {
                    lab.Add(new List<string> { labels[i] });
                    usr.Add(new List<string> { userInfo[i] });
                    res.Add(new List<string> { "" });
                }

            protoLabels.DataStore = lab;
            protoUserVals.DataStore = usr;
            protoResults.DataStore = res;

            // initialize already present values
            if (userInfo.Count != 0)
            {
                for(var i = 4; i < userInfo.Count; i++)
                {
                    if (userInfo[i].Length > 0)
                    {
                        if (i == 4)
                            res[i] = new List<string> { UpdateColor()[i][0] };
                        if (i > 4)
                            res[i] = new List<string> { UpdatePart(i)[i][0] };
                    }
                }
                protoResults.DataStore = res;
            }
        }
        private List<List<string>> UpdateColor()
        {
            var user_ds = protoUserVals.DataStore as List<List<string>>;
            var res_ds = protoResults.DataStore as List<List<string>>;

            var sql = new SQLTools();
            var color = sql.queryOEMColors(user_ds[4][0]);

            if (color[0].colorName.Length > 1)
                res_ds[4][0] = $"{color[0].colorNum} - {color[0].colorName}";
            else
                res_ds[4][0] = "";
            
            return res_ds;
        }
        private List<List<string>> UpdatePart(int row)
        {
            var user_ds = protoUserVals.DataStore as List<List<string>>;
            var res_ds = protoResults.DataStore as List<List<string>>;

            var partInfo = new OEM_Label(user_ds[row][0]);

            if (partInfo.IsValid)
                res_ds[row][0] = partInfo.partName;
            else
                res_ds[row][0] = "";

            return res_ds;
        }
        private void ProtoInfo_CellEdited(object sender, GridViewCellEventArgs e)
        {
            if (e.Row > 3)
            {
                var user_ds = protoUserVals.DataStore as List<List<string>>;
                var res_ds = protoResults.DataStore as List<List<string>>;

                // for the color
                if (e.Row == 4)
                    res_ds = UpdateColor();

                // for the parts only
                if (e.Row > 4)
                    res_ds = UpdatePart(e.Row);

                protoResults.DataStore = res_ds;
            }
        }
    }

    public class OEMColorManager
    {
        private Dialog<DialogResult> window;
        public Point windowPosition = new Point(400, 400);

        private Button placeButt = new Button { Text = "Place Label" };
        private Button addButt = new Button { Text = "ADD", ToolTip = "Double-Click", Enabled = false };
        private Button okButt = new Button { Text = "Done" };

        private TextBox newNumber = new TextBox { Width = 100 };
        private TextBox newName = new TextBox { Width = 180 };

        private SQLTools sql = new SQLTools();

        private GridView colors = new GridView
        {
            Height = 350,
            ShowHeader = true,
            Border = BorderType.Line,
            GridLines = GridLines.Both,
            AllowMultipleSelection = false,
            Enabled = true,
            Columns = {
                new GridColumn {
                    Editable = true,
                    HeaderText = "Color Number",
                    DataCell = new TextBoxCell(0) { TextAlignment = TextAlignment.Center },
                    Width = 100
                },
                new GridColumn {
                    Editable = true,
                    HeaderText = "Description",
                    DataCell = new TextBoxCell(1) { TextAlignment = TextAlignment.Center },
                    Width = 260
                }
            }
        };

        public void ShowForm()
        {
            window = new Dialog<DialogResult>
            {
                Padding = 10,
                Title = "Liebinger Label",
                AutoSize = true,
                Topmost = true,
                Result = DialogResult.Cancel,
                WindowStyle = WindowStyle.Default,
                Location = windowPosition
            };

            var gridLabel = new Label { Text = "Current Color List" };
            var removeRowLayout = new TableLayout
            {
                Padding = new Padding(5, 5, 5, 5),
                Spacing = new Size(5, 5),
                Rows = {
                    new TableRow(gridLabel, null)
                }
            };

            var colorGridLayout = new TableLayout
            {
                Padding = new Padding(5, 0, 5, 15),
                Spacing = new Size(5, 5),
                Rows = {
                    new TableRow(colors)
                }
            };

            var newNumberLabel = new Label { Text = "New Color Number" };
            var newNameLabel = new Label { Text = "Description" };
            var newValsLayout = new TableLayout
            {
                Padding = new Padding(5, 5, 5, 5),
                Spacing = new Size(5, 5),
                Rows = {
                    new TableRow(newNumberLabel, newNameLabel, null),
                    new TableRow(newNumber, newName, addButt)
                }
            };

            var buttonLayout = new TableLayout
            {
                Padding = new Padding(5, 5, 5, 5),
                Spacing = new Size(5, 5),
                Rows = {
                    new TableRow(null, placeButt, okButt)
                }
            };

            window.Content = new TableLayout
            {
                Spacing = new Size(5, 5),
                Rows = {
                    new TableRow(removeRowLayout),
                    new TableRow(colorGridLayout),
                    new TableRow(newValsLayout),
                    new TableRow(buttonLayout)
                }
            };

            // make the events
            colors.SelectedItemsChanged += Colors_SelectionChanged;
            colors.CellEdited += Colors_CellEdited;
            okButt.Click += (s, e) => window.Close(DialogResult.Ok);
            placeButt.Click += (s, e) => window.Close(DialogResult.Yes);
            addButt.MouseDoubleClick += AddButt_MouseDoubleClick;
            newName.TextChanged += NewName_TextChanged;
            newNumber.TextChanged += NewName_TextChanged;

            gridAssembler();
            window.ShowModal(RhinoEtoApp.MainWindow);
            windowPosition = window.Location;
        }

        public DialogResult CommandResult
        {
            get
            {
                return window.Result;
            }
        }

        public OEMColor SelectedColor
        {
            get
            {
                return sql.queryOEMColors()[colors.SelectedRow];
            }
        }

        private void NewName_TextChanged(object sender, EventArgs e)
        {
            if (newNumber.Text != string.Empty && newName.Text != string.Empty)
                addButt.Enabled = true;
        }

        private void Colors_CellEdited(object sender, GridViewCellEventArgs e)
        {
            if (e.Row != -1)
            {
                var c = sql.queryOEMColors()[e.Row];
                var ds = colors.DataStore as List<List<string>>;
                var newC = new OEMColor(ds[e.Row][0], ds[e.Row][1], c.id);
                sql.updateOemColor(newC);
            }
        }

        private void AddButt_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            
            if (newNumber.Text != string.Empty && newName.Text != string.Empty)
            {
                var c = sql.queryOEMColors();
                sql.insertOemColor(new OEMColor(newNumber.Text, newName.Text, c[c.Count - 1].id + 1));
                gridAssembler();
            }
        }

        private void Colors_SelectionChanged(object sender, EventArgs e)
        {
            if (colors.SelectedItem != null)
                placeButt.Enabled = true;
            else
                placeButt.Enabled = false;
        }

        private void gridAssembler()
        {
            var colorDB = sql.queryOEMColors();
            var ds = new List<List<string>>(colorDB.Count);
            foreach(var c in colorDB)
                ds.Add(new List<string> { c.colorNum, c.colorName });

            colors.DataStore = ds;
        }
    }

    public class BannerDialog
    {
        // storage struct
        public struct BannerInfo
        {
            public enum Stitch { Single, Double, Raw, Weld }
            public enum Finish { None, Hem, Pocket }
            public double gromEdgeOffset;
            public double gromDiameter;

            public double Width;
            public double Height;
            public bool Folded;

            public Stitch st_Top;
            public Stitch st_Bott;
            public Stitch st_Side;

            public Finish fn_Top;
            public Finish fn_Bott;
            public Finish fn_Side;

            public double Size_Top;
            public double Size_Bott;
            public double Size_Side;

            public int gromQty_Top;
            public int gromQty_Bott;
            public int gromQty_Side;

            public string PartNumber;
        }

        // Make structure
        public BannerInfo BData = new BannerInfo();

        // Text Lists
        private List<string> STRType = new List<string> { "Single Sided", "Double Sided", "Single Side Folded" };
        private List<string> STRStitch = new List<string> { "Single", "Double", "Raw Edge", "Weld" };
        private List<string> STRFinishing = new List<string> { "None", "Hem", "Pocket" };

        // ListBoxes
        public DropDown FoldedBanner = new DropDown { SelectedIndex = 0, Width = 180 };
        public ListBox StitchTypeTop = new ListBox { SelectedIndex = 2, Height = 85 };
        public ListBox StitchTypeBott = new ListBox { SelectedIndex = 2, Height = 85 };
        public ListBox StitchTypeSide = new ListBox { SelectedIndex = 2, Height = 85 };
        public ListBox FinishingTop = new ListBox { SelectedIndex = 0, Height = 65 };
        public ListBox FinishingBott = new ListBox { SelectedIndex = 0, Height = 65 };
        public ListBox FinishingSide = new ListBox { SelectedIndex = 0, Height = 65 };

        // Values
        public TextBox GromDiameter = new TextBox { Text = "0.75", Width = 45, ToolTip = "Diameter" };
        public TextBox GromEdgeOffset = new TextBox { Text = "0.563", Width = 45, ToolTip = "Offset" };

        // Text Fields
        public TextBox Width = new TextBox();
        public TextBox Height = new TextBox();

        public TextBox TopSize = new TextBox();
        public TextBox BottSize = new TextBox();
        public TextBox SideSize = new TextBox();

        private TextBox TopPoleSize = new TextBox { ToolTip = "Top" };
        private TextBox BottPoleSize = new TextBox { ToolTip = "Bottom" };

        public TextBox TopGromQty = new TextBox { ToolTip = "Top" };
        public TextBox BottGromQty = new TextBox { ToolTip = "Bottom" };
        public TextBox SideGromQty = new TextBox { ToolTip = "Side" };

        private TextBox TopGromSpace = new TextBox { Enabled = false };
        private TextBox BottGromSpace = new TextBox { Enabled = false };
        private TextBox SideGromSpace = new TextBox { Enabled = false };

        public TextBox PartNumber = new TextBox();
        public TextBox Code = new TextBox();

        // buttons
        private Button OkButt = new Button { Text = "Ok", Enabled = false };
        private Button CancelButt = new Button { Text = "Cancel" };


        // Window stuff
        private Dialog<DialogResult> window;
        public Point windowPosition = new Point(400, 400);

        /// <summary>
        /// Create and Show the form
        /// </summary>
        public void ShowForm()
        {
            window = new Dialog<DialogResult>
            {
                Padding = 10,
                Title = "Banner Maker",
                Topmost = true,
                Result = DialogResult.Cancel,
                WindowStyle = WindowStyle.Default,
                Location = windowPosition
            };

            var ChangeVariables = new TableLayout
            {
                Padding = new Padding(5, 5, 5, 20),
                Spacing = new Size(5, 5),
                Rows = {
                    new TableRow(new Label { Text = "Grommet Diameter" }, GromDiameter, new Label { Text = "Gromet Offset" }, GromEdgeOffset)
                }
            };

            FoldedBanner.DataStore = STRType;
            var WidthAndHeight = new TableLayout
            {
                Padding = new Padding(5, 5, 5, 5),
                Spacing = new Size(5, 5),
                Rows = {
                    new TableRow(new Label { Text = "Paste Code" }, Code),
                    new TableRow(new Label { Text = "Width" }, Width),
                    new TableRow(new Label { Text = "Height" }, Height),
                    new TableRow(new Label { Text = "Printed Sides" }, FoldedBanner)
                }
            };

            StitchTypeTop.DataStore = STRStitch;
            StitchTypeBott.DataStore = STRStitch;
            StitchTypeSide.DataStore = STRStitch;
            FinishingTop.DataStore = STRFinishing;
            FinishingBott.DataStore = STRFinishing;
            FinishingSide.DataStore = STRFinishing;
            var multiTable = new TableLayout
            {
                Padding = new Padding(5, 15, 5, 15),
                Spacing = new Size(5, 5),
                Rows = {
                    new TableRow(null, new Label { Text = "Top", TextAlignment = TextAlignment.Center }, 
                        new Label { Text = "Bottom", TextAlignment = TextAlignment.Center }, 
                        new Label { Text = "Sides", TextAlignment = TextAlignment.Center }),
                    new TableRow(new Label { Text = "Stitch\nType", TextAlignment = TextAlignment.Right }, 
                        StitchTypeTop, StitchTypeBott, StitchTypeSide),
                    new TableRow(new Label { Text = "Edge\nFinishing", TextAlignment = TextAlignment.Right }, 
                        FinishingTop, FinishingBott, FinishingSide),
                    new TableRow(new Label { Text = "Size", TextAlignment = TextAlignment.Right }, 
                        TopSize, BottSize, SideSize),
                    new TableRow(new Label { Text = "Pole Dia.", TextAlignment = TextAlignment.Right }, 
                        TopPoleSize, BottPoleSize, null),
                    new TableRow(new Label { Text = "Grommets", TextAlignment = TextAlignment.Right }, 
                        new Label { Text = "Top", TextAlignment = TextAlignment.Center },
                        new Label { Text = "Bottom", TextAlignment = TextAlignment.Center },
                        new Label { Text = "Sides", TextAlignment = TextAlignment.Center }),
                    new TableRow(new Label { Text = "Qty", TextAlignment = TextAlignment.Right },
                        TopGromQty, BottGromQty, SideGromQty),
                    new TableRow(new Label { Text = "Spacing", TextAlignment = TextAlignment.Right }, 
                        TopGromSpace, BottGromSpace, SideGromSpace)
                }
            };

            var PartNum = new TableLayout
            {
                Padding = new Padding(5, 5, 5, 5),
                Spacing = new Size(5, 5),
                Rows = {
                    new TableRow(new Label { Text = "Part Number" }, PartNumber)
                }
            };

            var ButtonRow = new TableLayout
            {
                Padding = new Padding(5, 5, 5, 5),
                Spacing = new Size(5, 5),
                Rows = {
                    new TableRow(null, OkButt, CancelButt)
                }
            };

            // Fill the window
            window.Content = new TableLayout
            {
                Spacing = new Size(5, 5),
                Rows = {
                    new TableRow(ChangeVariables),
                    new TableRow(WidthAndHeight),
                    new TableRow(multiTable),
                    new TableRow(PartNum),
                    new TableRow(ButtonRow)
                }
            };

            // Events
            OkButt.Click += (s, e) => window.Close(DialogResult.Ok);
            CancelButt.Click += (s, e) => window.Close(DialogResult.Cancel);
            Code.KeyUp += Code_KeyUp;
            PartNumber.KeyUp += PartNumber_KeyUp;
            TopGromQty.KeyUp += GromQty_KeyUp;
            BottGromQty.KeyUp += GromQty_KeyUp;
            SideGromQty.KeyUp += GromQty_KeyUp;
            TopPoleSize.KeyUp += PoleSize_KeyUp;
            BottPoleSize.KeyUp += PoleSize_KeyUp;
            GromEdgeOffset.LostFocus += GromValue_LostFocus;
            GromDiameter.LostFocus += GromValue_LostFocus;

            // Display the window
            window.ShowModal(RhinoEtoApp.MainWindow);
            windowPosition = window.Location;
        }

        private void GromValue_LostFocus(object sender, EventArgs e)
        {
            var box = sender as TextBox;
            if (!double.TryParse(box.Text, out double value))
            {
                if (box.ToolTip == "Diameter")
                    box.Text = "0.75";
                if (box.ToolTip == "Offset")
                    box.Text = "0.563";
            }
        }

        private void PoleSize_KeyUp(object sender, KeyEventArgs e)
        {
            var box = sender as TextBox;
            var tryV = double.TryParse(box.Text, out double dia);
            
            if (e.Key == Keys.Enter && tryV)
            {
                double circum = (Math.Floor(dia * Math.PI * 4) / 4) / 2 + 0.25;

                if (box.ToolTip == "Top")
                {
                    TopSize.Text = circum.ToString();
                    BData.Size_Top = circum;
                }
                if (box.ToolTip == "Bottom")
                {
                    BottSize.Text = circum.ToString();
                    BData.Size_Bott = circum;
                }

                box.Text = "";
            }
        }

        private void Code_KeyUp(object sender, KeyEventArgs e)
        {
            if (Code.Text.Length == 105)
                CalcCode_Click(sender, e);
        }

        private void GromQty_KeyUp(object sender, KeyEventArgs e)
        {
            var box = sender as TextBox;
            var tryW = double.TryParse(Width.Text, out BData.Width);
            var tryH = double.TryParse(Width.Text, out BData.Height);
            var tryV = int.TryParse(box.Text, out int num);
            var g_offset = double.Parse(GromEdgeOffset.Text);

            double dist;
            if (num <= 1)
                tryV = false;

            if (box.ToolTip == "Top" && tryW && tryV)
            {
                dist = (BData.Width - (g_offset * 2)) / (num - 1);
                BData.gromQty_Top = num;
                TopGromSpace.Text = Math.Round(dist, 4).ToString();
            }

            if (box.ToolTip == "Bottom" && tryW && tryV)
            {
                dist = (BData.Width - (g_offset * 2)) / (num - 1);
                BData.gromQty_Bott = num;
                BottGromSpace.Text = Math.Round(dist, 4).ToString();
            }

            if (box.ToolTip == "Side" && tryH && tryV)
            {
                var trySizeT = double.TryParse(TopSize.Text, out double st);
                var trySizeB = double.TryParse(BottSize.Text, out double sb);
                var h = BData.Height;

                if (FinishingTop.SelectedIndex == 2 && trySizeT)
                    h -= st;
                if (FinishingBott.SelectedIndex == 2 && trySizeB)
                    h -= sb;

                dist = (h - (g_offset * 2)) / (num - 1);
                BData.gromQty_Side = num;
                SideGromSpace.Text = Math.Round(dist, 4).ToString();
            }
        }

        private void PartNumber_KeyUp(object sender, KeyEventArgs e)
        {
            if (PartNumber.Text.Length > 0)
            {
                BData.PartNumber = PartNumber.Text;
                OkButt.Enabled = true;
            }
            else
                OkButt.Enabled = false;
        }

        private void CalcCode_Click(object sender, EventArgs e)
        {
            int byt = 7;
            var snip = new List<double>();
            var dat = Code.Text;

            for (int i = 1; i < dat.Length / byt; i++)
                snip.Add(double.Parse(dat.Substring(byt * i, byt)));

            Width.Text = snip[0].ToString();
            Height.Text = snip[1].ToString();

            FoldedBanner.SelectedIndex = (snip[13] == 1) ? 2 : 0;

            StitchTypeTop.SelectedIndex = StitchIndex(snip[4]);
            StitchTypeBott.SelectedIndex = StitchIndex(snip[5]);
            StitchTypeSide.SelectedIndex = StitchIndex(snip[6]);

            FinishingTop.SelectedIndex = FinishIndex(snip[2], snip[7]);
            FinishingBott.SelectedIndex = FinishIndex(snip[3], snip[8]);
            FinishingSide.SelectedIndex = FinishIndex(0, snip[9]);

            TopSize.Text = (snip[2] + snip[7]).ToString();
            BottSize.Text = (snip[3] + snip[8]).ToString();
            SideSize.Text = snip[9].ToString();

            TopGromSpace.Text = snip[10].ToString();
            BottGromSpace.Text = snip[11].ToString();
            SideGromSpace.Text = snip[12].ToString();

            // populate the qtys
            CalcCode_QTYS();

            // Blank out the Code
            Code.Text = "";

            int FinishIndex(double pocket, double hem)
            {
                if (pocket > 0)
                    return 2;
                if (hem > 0)
                    return 1;
                return 0;
            }

            int StitchIndex(double val)
            {
                switch (val)
                {
                    case 0: // Single
                        val = 0; break;
                    case 1: // Double
                        val = 1; break;
                    case 2: // Weld
                        val = 3; break;
                    case 3: // Raw
                        val = 2; break;
                    default:
                        break;
                }
                return (int)val;
            }
        }

        /// <summary>
        /// Sync the spacing with the QTYs
        /// </summary>
        private void CalcCode_QTYS()
        {
            var w = double.Parse(Width.Text);
            var h = double.Parse(Height.Text);
            var t_space = double.Parse(TopGromSpace.Text);
            var b_space = double.Parse(BottGromSpace.Text);
            var s_space = double.Parse(SideGromSpace.Text);
            var g_offset = double.Parse(GromEdgeOffset.Text);

            // Initialize
            TopGromQty.Text = "0";
            BottGromQty.Text = "0";
            SideGromQty.Text = "0";

            if (t_space > 0)
                TopGromQty.Text = ((int)((w - (g_offset * 2)) / t_space + 1)).ToString();
            if (b_space > 0)
                BottGromQty.Text = ((int)((w - (g_offset * 2)) / b_space + 1)).ToString();
            if (t_space > 0)
                SideGromQty.Text = ((int)((h - (g_offset * 2)) / s_space + 1)).ToString();
        }

        public BannerInfo GetAllValues()
        {
            double.TryParse(Width.Text, out BData.Width);
            double.TryParse(Height.Text, out BData.Height);
            BData.Folded = (FoldedBanner.SelectedIndex == 2) ? true : false;

            BData.st_Top = (BannerInfo.Stitch)StitchTypeTop.SelectedIndex;
            BData.st_Bott = (BannerInfo.Stitch)StitchTypeBott.SelectedIndex;
            BData.st_Side = (BannerInfo.Stitch)StitchTypeSide.SelectedIndex;

            BData.fn_Top = (BannerInfo.Finish)FinishingTop.SelectedIndex;
            BData.fn_Bott = (BannerInfo.Finish)FinishingBott.SelectedIndex;
            BData.fn_Side = (BannerInfo.Finish)FinishingSide.SelectedIndex;

            double.TryParse(TopSize.Text, out BData.Size_Top);
            double.TryParse(BottSize.Text, out BData.Size_Bott);
            double.TryParse(SideSize.Text, out BData.Size_Side);

            int.TryParse(TopGromQty.Text, out BData.gromQty_Top);
            int.TryParse(BottGromQty.Text, out BData.gromQty_Bott);
            int.TryParse(SideGromQty.Text, out BData.gromQty_Side);

            double.TryParse(GromDiameter.Text, out BData.gromDiameter);
            double.TryParse(GromEdgeOffset.Text, out BData.gromEdgeOffset);

            BData.PartNumber = PartNumber.Text;

            return BData;
        }

        public DialogResult CommandResult
        {
            get
            {
                return window.Result;
            }
        }
    }
    
}