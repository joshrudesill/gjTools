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
        
        public OEM_Label(string OEMPartNumber)
        {
            rawLines = new List<string>();
            drawingNumber = OEMPartNumber;
            GetData();
        }

        private void GetData()
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
            }
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
            var SQL_Rows = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
            var sql = new SQLTools();

            var partInfo = sql.queryDataStore(SQL_Rows);
            var jobInfo = sql.queryJobSlots()[partInfo[0].intValue];
            var parts = new List<OEM_Label>();

            // prep data for the Property Box
            var propertyLabels = new List<string> { "Job", "Due Date", "Job Description", "Cut QTY", "Film:", "PN 1", "PN 2", "PN 3", "PN 4", "PN 5", "PN 6", "PN 7", "PN 8", "PN 10" };
            var propertyValues = new List<string> { jobInfo.job, jobInfo.due, jobInfo.description, jobInfo.quantity.ToString(), jobInfo.material };
            for (int i = 1; i < partInfo.Count; i++)
                propertyValues.Add(partInfo[i].stringValue);

            var userValues = new List<string>( Dialogs.ShowPropertyListBox("Prototype Utility", "Job Information", propertyLabels, propertyValues) );
            if (userValues == null)
                return Result.Cancel;

            // write values back to the database
            if (propertyValues != userValues.GetRange(0, 5))
            {
                sql.updateJobSlot(new JobSlot(partInfo[0].intValue, userValues[0], userValues[1], userValues[2], int.Parse(userValues[3]), userValues[4]));
                propertyValues = userValues.GetRange(0, 5);
            }
            for (int i = 1; i < partInfo.Count; i++)
            {
                if (partInfo[i].stringValue != userValues[i + 4])
                    partInfo[i] = new DataStore(partInfo[i].DBindex, userValues[i + 4], 0, 0.0);
                if (userValues[i + 4].Length > 7)
                    parts.Add(new OEM_Label(userValues[i + 4]));
            }

            // Get the OEM Color
            var colorList = sql.queryOEMColors(propertyValues[4]);

            // Present the new data
            string textMatl = string.Format("{0}\nCut - {1}x", propertyValues[4], propertyValues[3]);
            string textBlob = string.Format("Job: {0}   Due: {1}\n{2}\n", propertyValues[0], propertyValues[1], propertyValues[2]);
            foreach (var t in parts)
                textBlob += string.Format("\n{0} - {1}", t.drawingNumber, t.partName);
            

            return Result.Success;
        }
    }
}