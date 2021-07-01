using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.UI;

namespace gjTools.Commands
{
    public struct OEM_Label
    {
        public List<string> rawLines;
        public string drawingNumber;
        public bool IsValid;
        
        public OEM_Label(string OEMPartNumber)
        {
            rawLines = new List<string>();
            drawingNumber = OEMPartNumber.ToUpper();
            IsValid = false;
            IsValid = GetData();
        }

        private bool GetData()
        {
            string folderPath = "\\\\spi\\art\\PROTOTYPE\\AutoCAD_XML\\";
            if (System.IO.File.Exists(folderPath + drawingNumber + ".xml"))
            {
                var XMLfile = System.IO.File.OpenText(folderPath + drawingNumber + ".xml");
                while (true)
                {
                    string Line = XMLfile.ReadLine();
                    if (Line == "<AUTOCAD>")
                        continue;
                    if (Line == "</AUTOCAD>" || Line == null)
                        break;

                    rawLines.Add(Line);
                }
                return true;
            }
            else
                return false;
        }

        public string partName { get { return rawLines[1]; } }
        public string year { get { return rawLines[2]; } }
        public string customer { get { return rawLines[3]; } }
        public string process { get { return rawLines[4]; } }
        public string partsPerUnit { get { return rawLines[5]; } }
        public string DOC { get { return rawLines[6]; } }
        public string path { get { return rawLines[7]; } }
        public string customerPartNumber { get { return rawLines[8]; } }
    }

    public class PrototypeTool : Command
    {
        public PrototypeTool()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static PrototypeTool Instance { get; private set; }

        public override string EnglishName => "gjProtoUtility";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // SQL Lines used for this Command
            var SQL_Rows = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };
            var sql = new SQLTools();

            var partInfo = sql.queryDataStore(SQL_Rows);
            var slot = partInfo[0];
            var jobInfo = sql.queryJobSlots()[slot.intValue - 1];
            var parts = new List<OEM_Label>();

            // prep data for the Property Box
            var userValues = GetDataFromUser(jobInfo, partInfo);
            if (userValues.Count == 0)
                return Result.Cancel;

            // Split data apart
            var newJobVals = userValues.GetRange(0, 5);
            var newParts = userValues.GetRange(5, 10);

            // write values back to the database
            sql.updateJobSlot(new JobSlot(slot.intValue, newJobVals[0], newJobVals[1], newJobVals[2], int.Parse(newJobVals[3]), newJobVals[4]));
            for (var i = 0; i < newParts.Count; i++)
                sql.updateDataStore(new DataStore(SQL_Rows[i + 1], newParts[i].ToUpper(), 0, 0.0));
            foreach ( var p in newParts)
            {
                var label = new OEM_Label(p);
                if (label.IsValid)
                    parts.Add(label);
            }

            // Get the first OEM Color found
            var colorList = sql.queryOEMColors(newJobVals[4]);
            if (colorList.Count == 0)
                colorList.Add(new OEMColor(newJobVals[4], "Not Found", 0));

            // Present the new data
            var res = CheckDataMessage(newJobVals, parts, colorList[0]);
            if (res.Count == 0)
                return Result.Cancel;


            return Result.Success;
        }



        public bool PlaceTitleBlock(RhinoDoc doc, List<string> textBlocks)
        {
            var dt = new DrawTools(doc);
            return true;
        }

        /// <summary>
        /// Asks the user for information
        /// </summary>
        /// <param name="job"></param>
        /// <param name="parts"></param>
        /// <returns>empty list on Cancel or job data on OK</returns>
        public List<string> GetDataFromUser(JobSlot job, List<DataStore> parts)
        {
            // prep data for the Property Box
            var propertyLabels = new List<string> { "Job", "Due Date", "Job Description", "Cut QTY", "Film:", "PN 1", "PN 2", "PN 3", "PN 4", "PN 5", "PN 6", "PN 7", "PN 8", "PN 9", "PN 10" };
            var propertyValues = new List<string> { job.job, job.due, job.description, job.quantity.ToString(), job.material };
            for (int i = 1; i < parts.Count; i++)
                propertyValues.Add(parts[i].stringValue);

            var res = Dialogs.ShowPropertyListBox("Prototype Utility", "Job Information", propertyLabels, propertyValues);
            var newValues = new List<string>();
            if (res != null)
                newValues.AddRange(res);

            return newValues;
        }

        /// <summary>
        /// presents the data before placing
        /// </summary>
        /// <param name="job"></param>
        /// <param name="parts"></param>
        /// <param name="color"></param>
        /// <returns>true if ok to place</returns>
        public List<string> CheckDataMessage(List<string> job, List<OEM_Label> parts, OEMColor color)
        {
            // Present the new data
            string textMatl = string.Format("{0} - {1}\nCut - {2}x", color.colorNum, color.colorName, job[3]);
            string textBlob = string.Format("Job: {0}   Due: {1}\n{2}\n", job[0], job[1], job[2]);
            foreach (var t in parts)
                textBlob += string.Format("\n{0} - {1}", t.drawingNumber, t.partName);

            var ok = Dialogs.ShowMessage(string.Format("{0}\n\n{1}", textMatl, textBlob), "Info Preview", ShowMessageButton.OKCancel, ShowMessageIcon.Information);
            var str = new List<string>();
            if (ok == ShowMessageResult.OK)
            {
                str.Add(textBlob);
                str.Add(textMatl);
            }
            return str;
        }
    }
}