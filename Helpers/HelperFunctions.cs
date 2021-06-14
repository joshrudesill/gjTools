using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino;
using Rhino.Commands;

namespace gjTools
{
    interface IHelperFunctions
    {
        void showColorPallete();
    }

    class HelperFunctions : IHelperFunctions
    {
        /// <summary>
        /// Shows list box with colors listed.
        /// </summary>
        public void showColorPallete()
        {
            SQLHelper sql = new SQLHelper();
            var rL = new List<string>();
            var rL2 = new List<string>();
            foreach (OEMColor item in sql.queryOEMColors())
            {
                rL.Add(item.colorName);
                rL2.Add(item.colorNum);
            }
            
            Rhino.UI.Dialogs.ShowPropertyListBox("OEM Colors", "List of Colors", rL, rL2);
            
        }

        public void addColor()
        {
            SQLHelper sql = new SQLHelper();
            int c = sql.queryOEMColors().Count();
            List<string> bsL = new List<string>{"", ""};
            List<string> sL = new List<string> { "Color name", "Color number" };

            string[] rL = Rhino.UI.Dialogs.ShowPropertyListBox("Add Color", "Add a color", sL, bsL);

            sql.executeCommand(string.Format("INSERT INTO oemColors (colorNum, colorName, id) VALUES ('{0}', '{1}', '{2}');", rL[1], rL[0], c + 1));
        }
    }
}
