using System;
using System.Collections.Generic;
using Eto.Forms;
using Rhino;

namespace gjTools.Helpers
{
    class AssignCutTypeForm
    {
        public RhinoDoc doc;
        public List<string> LeftList;
        public AssignCutTypeForm(RhinoDoc docum)
        {
            doc = docum;
        }
        public void Makeform()
        {
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
            optionList.DataStore = new LayerTools(doc).getAllParentLayersStrings();

            var multiList = new GridView()
            {
                Height = 350,
                Width = 240,
            };
            multiList.DataStore = new LayerTools(doc).getAllParentLayersStrings();

            layout.AddRow(optionList, multiList);


            window.Content = layout;
            window.ShowModal();
        }
    }
}
