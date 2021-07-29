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
        private Button okButt = new Button { Text = "OK" };
        private Button cancelButt = new Button { Text = "Cancel" };
        private GridView protoLabels = new GridView
        {
            Height = 300,
            Width = 55,
            ShowHeader = false,
            Border = BorderType.Line,
            GridLines = GridLines.Horizontal,
            AllowMultipleSelection = false,
            Columns = {
                new GridColumn { 
                    Editable = false,
                    DataCell = new TextBoxCell(0) { TextAlignment = TextAlignment.Right },
                    Width = 50
                },
            }
        };
        private GridView protoUserVals = new GridView
        {
            Height = 300,
            Width = 85,
            ShowHeader = false,
            Border = BorderType.Line,
            GridLines = GridLines.Horizontal,
            AllowMultipleSelection = false,
            Columns = {
                new GridColumn {
                    Editable = true,
                    DataCell = new TextBoxCell(0) { TextAlignment = TextAlignment.Center },
                    Width = 80
                },
            }
        };
        private GridView protoResults = new GridView
        {
            Height = 300,
            Width = 205,
            ShowHeader = false,
            Border = BorderType.Line,
            GridLines = GridLines.Horizontal,
            AllowMultipleSelection = false,
            Columns = {
                new GridColumn {
                    Editable = false,
                    DataCell = new TextBoxCell(0) { TextAlignment = TextAlignment.Left },
                    Width = 200
                },
            }
        };

        private List<string> labels = new List<string>
        {
            "Job",
            "Due Date",
            "Description",
            "Film",
            "Cut Qty",
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
        public List<string> userInfo = new List<string>();
        public Point windowPosition;

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
                    new TableRow(null, okButt, cancelButt)
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
        }

        private void ProtoInfo_CellEdited(object sender, GridViewCellEventArgs e)
        {
            var user_ds = protoUserVals.DataStore as List<List<string>>;
            var res_ds = protoResults.DataStore as List<List<string>>;
            var partInfo = new OEM_Label(user_ds[e.Row][0]);
            userInfo[e.Row] = user_ds[e.Row][0];

            if (partInfo.IsValid)
                res_ds[e.Row][0] = partInfo.partName;

            protoResults.DataStore = res_ds;
        }
    }
}