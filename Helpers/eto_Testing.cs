using System;
using System.Collections.Generic;
using Eto.Forms;
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

            var window = new Dialog()
            {
                ClientSize = new Eto.Drawing.Size(515, 515),
                Padding = 15
            };

            var layout = new DynamicLayout();
            layout.Spacing = new Eto.Drawing.Size(5, 5);

            var optionList = new ListBox()
            {
                Height = 350,
                Width = 240
            };
            var lt = new LayerTools(doc).getAllParentLayersStrings();
            optionList.DataStore = leftList;

            layout.AddRow(optionList, MultiList(rightList));

            var okButt = new Button()
            {
                Text = "Ok",
                Width = 40,
                Height = 25
            };
            //okButt.Click += OkButtonPressed();

            window.Content = layout;
            window.ShowModal();
        }

        private void OkButtonPressed()
        {
            var vals = new List<string>();

            
        }

        private GridView MultiList(List<string> input)
        {
            var newMultiList = new List<List<string>>();
            foreach(var s in input)
                newMultiList.Add(new List<string>{ s });

            var multiList = new GridView()
            {
                Height = 350,
                Width = 240,
                ShowHeader = false,
                AllowMultipleSelection = true,
                DataStore = newMultiList
            };
            multiList.Columns.Add(new GridColumn()
            {
                Editable = false,
                DataCell = new TextBoxCell(0),
                Width = 220
            });

            return multiList;
        }
    }
}
