using System;
using System.Collections.Generic;
using Eto.Forms;
using Rhino;

namespace gjTools.Helpers
{
    class AssignCutTypeForm
    {
        public void Makeform()
        {
            var window = new Form()
            {
                ClientSize = new Eto.Drawing.Size(515, 515),
                Padding = 15
            };

            var layout = new DynamicLayout();

            var optionList = new ListBox()
            {
                Height = 350,
                Width = 240
            };
            var optionList2 = new ListBox()
            {
                Height = 350,
                Width = 240
            };

            layout.BeginVertical();
            layout.AddRow(optionList, optionList2);
            layout.EndVertical();

            window.Content = layout;
            window.Show();
        }
    }
}
