using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Rhino;

namespace gjTools.Helpers
{
    class DualListDialog
    {
        public RhinoDoc doc;
        
        public DualListDialog(RhinoDoc docum)
        {
            doc = docum;
        }
        public void Makeform()
        {
            var leftList = new LayerTools(doc).getAllParentLayersStrings();
            var rightList = leftList;
            var myTitle = "Testing Window";
            var myPromptLeft = "Left Label";
            var myPromptRight = "Right Label";
            var lt = new LayerTools(doc).getAllParentLayersStrings();

            var window = new Dialog
            {
                ClientSize = new Size(515, 440),
                Padding = 15,
                Title = myTitle,
                Topmost = true
            };

            var layout = new DynamicLayout();
            layout.Spacing = new Size(5, 5);

            var optionList = new ListBox
            {
                Height = 350,
                Width = 240
            };

            optionList.DataStore = leftList;
            var multiList = MultiList(rightList);

            layout.AddRow(new Label { Text = myPromptLeft }, new Label { Text = myPromptRight });
            layout.AddRow(optionList, multiList);

            var okButt = new Button
            {
                Text = "Ok",
            };
            okButt.Click += OkButtonPressed;

            var cancelButt = new Button
            {
                Text = "Cancel",
            };
            cancelButt.Click += OkButtonPressed;

            layout.AddRow(okButt, cancelButt);

            window.Content = layout;
            window.ShowModal();
        }

        private void OkButtonPressed(object s, EventArgs e)
        {
            var vals = new List<string>();
            
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
    }
}
