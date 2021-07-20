using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Input;
using Rhino.UI;

namespace gjTools.Commands
{
    public class PN_Label_OEM : Command
    {
        public PN_Label_OEM()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static PN_Label_OEM Instance { get; private set; }

        public override string EnglishName => "OEMLabel";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // get from the datastore
            var sql = new SQLTools();
            var oldPartNumber = sql.queryDataStore(new List<int> { 12 })[0];

            if (Dialogs.ShowEditBox("OEM Label Maker", "Enter OEM Part Number", oldPartNumber.stringValue, false, out string partNumber) == false)
                return Result.Cancel;

            // update the 
            sql.updateDataStore(new DataStore(oldPartNumber.DBindex, partNumber.ToUpper(), 0, 0.0, EnglishName));
            var label = new OEM_Label(partNumber);
            if (!label.IsValid)
            {
                RhinoApp.WriteLine("That Part Number was not Found...");
                return Result.Cancel;
            }

            // make label
            CreateOEMLabel(doc, label);

            return Result.Success;
        }


        /// <summary>
        /// Adds the OEM Quote style Label from Chosen NestBox
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="label"></param>
        public void CreateOEMLabel(RhinoDoc doc, OEM_Label label)
        {
            var dt = new DrawTools(doc);
            var ds = dt.StandardDimstyle();

            if (RhinoGet.GetOneObject("Select a Nesting Box", false, ObjectType.Curve, out ObjRef nestBox) == Result.Success)
            {
                var pt = nestBox.Geometry().GetBoundingBox(true).GetCorners()[3];
                    pt.Y += 0.5;
                var txt = dt.AddText($"PN: {label.drawingNumber}\n{label.partName}\n{label.year} {label.customer}\n{label.process}\nU/M: {label.partsPerUnit}\nDOC#: {label.DOC}",
                    pt, ds, 1.0, 0, 3, 6);

                // add the label
                var txtObj = doc.Objects.FindId(doc.Objects.AddText(txt));

                // detect the label
                var play = doc.Layers[nestBox.Object().Attributes.LayerIndex];
                if (play.ParentLayerId != Guid.Empty)
                    play = doc.Layers.FindId(play.ParentLayerId);

                // change the label layer
                txtObj.Attributes.LayerIndex = play.Index;
                txtObj.CommitChanges();
            }
        }
    }
}