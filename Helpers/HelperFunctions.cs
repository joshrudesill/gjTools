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
            foreach (List<object> item in sql.queryOEMColors())
            {
                rL.Add(item[0].ToString());
                rL2.Add(item[1].ToString());
            }
            Rhino.UI.Dialogs.ShowPropertyListBox("OEM Colors", "List of colors", rL, rL2);
        }
    }
}
