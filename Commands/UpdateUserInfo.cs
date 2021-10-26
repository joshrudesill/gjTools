using Rhino;
using Rhino.Commands;
using Rhino.UI;
using System.Collections.Generic;

namespace gjTools.Commands
{
    public class UpdateUserInfo : Command
    {
        public UpdateUserInfo()
        {
            Instance = this;
        }

        public static UpdateUserInfo Instance { get; private set; }

        public override string EnglishName => "UpdateUserInfo";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            if (!updateUserInfo())
                return Result.Cancel;

            return Result.Success;
        }


        /// <summary>
        /// Asks user for new user data and sets it
        /// </summary>
        /// <returns></returns>
        public bool updateUserInfo()
        {
            var sql = new SQLTools();
            /// VariableData creds2 = sql.queryVariableData()[0];
            VariableData creds = SQLTools2.queryVariableData();
            List<string> credVals = new List<string> { creds.userLastName, creds.userFirstName, creds.userInitials, creds.cutNumber.ToString() };
            List<string> credLabels = new List<string> { "Last Name", "First Name", "Initials", "Cut Number" };

            string[] newValues = Dialogs.ShowPropertyListBox("Update User Data", "Change user data..", credLabels, credVals);
            
            if (newValues != null)
            {
                creds.userLastName = newValues[0];
                creds.userFirstName = newValues[1];
                creds.userInitials = newValues[2];
                int newcut = creds.cutNumber;
                int.TryParse(newValues[3], out newcut);
                creds.cutNumber = newcut;

                sql.updateVariableData(creds);

                return true;
            }
            return false;
        }
    }
}