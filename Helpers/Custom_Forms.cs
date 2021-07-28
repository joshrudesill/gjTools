using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Rhino.UI;

namespace gjTools.Helpers
{
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
        public Point windowPosition;

        public DualListDialog(string winTitle, string LabelSingle, List<string> singleSelect, string labelMulti, List<string> multiSelect, Point windowPos, int singleDefaultIndex = -1, int multiDefaultIndex = -1)
        {
            _winTitle = winTitle;
            _LabelSingle = LabelSingle;
            _singleSelect = singleSelect;
            _labelMulti = labelMulti;
            _multiSelect = multiSelect;

            window = new Dialog<DialogResult>
            {
                Padding = 10,
                Title = _winTitle,
                Topmost = true,
                Result = DialogResult.Cancel,
                WindowStyle = WindowStyle.Default,
                Location = windowPos
            };
            window.KeyDown += Window_KeyDown;

            optionList = new ListBox
            {
                Height = 350,
                Width = 240,
                DataStore = _singleSelect,
                TabIndex = 0
            };
            optionList.KeyDown += Window_KeyDown;

            multiList = MultiList(_multiSelect);
            multiList.TabIndex = 1;
            multiList.KeyDown += Window_KeyDown;

            multiList.SelectedRow = multiDefaultIndex;
            if (singleDefaultIndex != -1)
                optionList.SelectedIndex = singleDefaultIndex;

            okButt = new Button { Text = "ok" };
            okButt.Click += (s, e) => window.Close(DialogResult.Ok);

            cancelButt = new Button { Text = "Cancel" };
            cancelButt.Click += (s, e) => window.Close(DialogResult.Cancel);

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
                    new TableRow(null, null, okButt, cancelButt)
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

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Keys.Enter)
                window.Close(DialogResult.Ok);
            if (e.Key == Keys.Escape)
                window.Close(DialogResult.Cancel);
        }

        private GridView MultiList(List<string> input)
        {
            var newMultiList = new List<List<string>>();
            foreach(var s in input)
                newMultiList.Add(new List<string>{ s });

            var multiList = new GridView
            {
                Height = 350,
                Width = 240,
                ShowHeader = false,
                AllowMultipleSelection = true,
                DataStore = newMultiList
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

            return multiList;
        }


        public DialogResult CommandResult() { return window.Result; }
        public string GetSingleValue() { return optionList.SelectedKey; }
        public List<string> GetMultiSelectValue()
        {
            var ds = multiList.SelectedRows;
            var outlist = new List<String>();

            foreach (var s in ds)
                outlist.Add(_multiSelect[s]);

            return outlist;
        }
    }
}
