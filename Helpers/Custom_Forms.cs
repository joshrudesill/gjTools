using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Rhino.UI;

namespace gjTools.Helpers
{
    /// <summary>
    /// Intended for the PDF command
    /// </summary>
    class DualListDialog
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

        public void ShowForm() {
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


}
