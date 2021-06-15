using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino;
using Rhino.Commands;


/// <summary>
/// Dialog creation and result return
/// </summary>
namespace gjTools
{
    interface IHelperFunctions
    {
        void showColorPallete();
        void addColor();
        void updateUserInfo();
    }

    class DialogTools : IHelperFunctions
    {
        RhinoDoc m_doc;
        SQLTools sql;
        public DialogTools(RhinoDoc doc)
        {
            m_doc = doc;
            sql = new SQLTools();
        }
        /// <summary>
        /// Shows list box with colors listed.
        /// </summary>
        public void showColorPallete()
        {
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
            int c = sql.queryOEMColors().Count();
            List<string> bsL = new List<string>{"", ""};
            List<string> sL = new List<string> { "Color name", "Color number" };

            string[] rL = Rhino.UI.Dialogs.ShowPropertyListBox("Add Color", "Add a color", sL, bsL);

            sql.executeCommand(string.Format("INSERT INTO oemColors (colorNum, colorName, id) VALUES ('{0}', '{1}', '{2}');", rL[1], rL[0], c + 1));
        }

        /// <summary>
        /// Updates user info in the database.
        /// </summary>
        public void updateUserInfo()
        {
            List<VariableData> vd = sql.queryVariableData();
            List<string> bsL = new List<string> { vd[0].userLastName, vd[0].userFirstName, vd[0].userInitials, vd[0].cutNumber.ToString() };
            List<string> sL = new List<string> { "Last Name", "First Name", "Initials", "Cut Number" };

            string[] rL = Rhino.UI.Dialogs.ShowPropertyListBox("Update User Data", "Change user data..", sL, bsL);

            sql.executeCommand(string.Format(
                "UPDATE variableData SET userLastName = '{0}', userFirstName = '{1}' , userInitials = '{2}', cutNumber = '{3}' WHERE userLastName = '{4}';",
                rL[0], rL[1], rL[2], rL[3], vd[0].userLastName
                ));
        }
        /// <summary>
        /// Adds a named parent layer with color. Returns index of layer. Returns -1 on failure.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        public int addLayer(string name, System.Drawing.Color color)
        {
            Rhino.DocObjects.Layer layer = new Rhino.DocObjects.Layer();
            layer.Name = name;
            layer.Color = color;
            return m_doc.Layers.Add(layer);
        }
    }
}
