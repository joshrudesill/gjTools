using System;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;

namespace gjTools.Commands
{
    public class EP_CreateXML : Command
    {
        public EP_CreateXML()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static EP_CreateXML Instance { get; private set; }

        public override string EnglishName => "EP_CreateXML";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            RhinoApp.WriteLine("Create the E&P XML File (Made for Automation ReportMaker)");
            RhinoApp.WriteLine("This will create the folder if it's not already");

            var CutLayer = doc.Layers.FindByFullPath("CUT::NestBox", -1);
            if (CutLayer == -1)
            {
                RhinoApp.WriteLine("CUT Layer Not Found -or- NestBox is in need of an update");
                return Result.Cancel;
            }

            // get the data and provide a preview and Sets choice
            var NestBox = doc.Objects.FindByLayer(doc.Layers[CutLayer])[0];
            GUI.XML_Info xmlInfo = new GUI.XML_Info(doc.Name.Replace(".3dm", ""), NestBox.Attributes);
            GUI.XML_Gui xmlDialog = new GUI.XML_Gui(xmlInfo);

            if (xmlDialog.CommandResult != Eto.Forms.DialogResult.Ok)
                return Result.Cancel;

            // assemble the xml string
            xmlInfo = xmlDialog.GetXML_Info;
            string XMLDoc = "<JDF>\n  " +
                $"<DrawingNumber>{xmlInfo.DrawingNumber}</DrawingNumber>\n  " +
                $"<CADSheetWidth>{xmlInfo.SheetWidth}</CADSheetWidth>\n  " +
                $"<CADSheetHeight>{xmlInfo.SheetHeight}</CADSheetHeight>\n  " +
                $"<CADNumberUp>{xmlInfo.GetPartQty}</CADNumberUp>\n</JDF>";

            // create the directory if not already
            string folderPath = FileLocations.PathDict["EP"] + doc.Name.Replace(".3dm", "");
            if (!System.IO.Directory.Exists(folderPath))
            {
                System.IO.Directory.CreateDirectory(folderPath);
                RhinoApp.Write("Folder Created and ");
            }

            // write the xml file to the EP location
            var path = folderPath + "\\" + doc.Name.Replace(".3dm", ".xml");
            System.IO.File.WriteAllText(path , XMLDoc);

            RhinoApp.WriteLine($"Wrote XML: {path}");
            return Result.Success;
        }
    }
}


namespace GUI
{
    using Eto.Forms;
    using Eto.Drawing;
    using Rhino.UI;

    public struct XML_Info
    {
        public readonly string DrawingNumber;
        public readonly string SheetWidth;
        public readonly string SheetHeight;
        private string PartQty;

        public string GetPartQty
        {
            get { return (IsSets) ? (double.Parse(PartQty) / 2).ToString() : PartQty; }
        }
        
        // divide PartQty by 2
        public bool IsSets;

        public XML_Info(string drawingNumber, ObjectAttributes NestBoxAttrs)
        {
            DrawingNumber = drawingNumber;
            IsSets = false;

            var vals = NestBoxAttrs.GetUserStrings();
            SheetWidth = vals.Get("Width");
            SheetHeight = vals.Get("Height");
            PartQty = (vals.Get("QtyGrp") == "0") ? vals.Get("QtyObj") : vals.Get("QtyGrp");
        }
    }

    public class XML_Gui
    {
        private Dialog<DialogResult> m_Dialog;
        private XML_Info XMLInfo;

        public XML_Gui(XML_Info xml)
        {
            XMLInfo = xml;

            m_Dialog = new Dialog<DialogResult>
            {
                Padding = 10,
                Title = "E&P XML Maker",
                AutoSize = true,
                Topmost = true,
                Result = DialogResult.Cancel,
                WindowStyle = WindowStyle.Default,
                Location = new Point((int)(Mouse.Position.X - 50), 400)
            };

            Label qty = new Label { Text = xml.GetPartQty, TextAlignment = TextAlignment.Center };
            CheckBox sets = new CheckBox { Text = "Yes ", Checked = false };
            Button but_ok = new Button { Text = "Ok" };
            Button but_cancel = new Button { Text = "Cancel" };

            var infobox = new TableLayout
            {
                Padding = new Padding(10, 10, 10, 10),
                Spacing = new Size(15, 5),
                Rows =
                {
                    new TableRow(new Label{ Text = "Drawing Number: ", TextAlignment = TextAlignment.Right }, new Label{ Text = xml.DrawingNumber, TextAlignment = TextAlignment.Center}),
                    new TableRow(new Label{ Text = "Sheet Width: ", TextAlignment = TextAlignment.Right },    new Label{ Text = xml.SheetWidth, TextAlignment = TextAlignment.Center}),
                    new TableRow(new Label{ Text = "Sheet Height: ", TextAlignment = TextAlignment.Right },   new Label{ Text = xml.SheetHeight, TextAlignment = TextAlignment.Center}),
                    new TableRow(new Label{ Text = "Part Qty: ", TextAlignment = TextAlignment.Right },       qty),
                    new TableRow(new Label{ Text = "Sets: ", TextAlignment = TextAlignment.Right },           sets)
                }
            };

            var cont = new TableLayout
            {
                Padding = new Padding(1, 1, 1, 1),
                Spacing = new Size(1, 1),
                Rows =
                {
                    new TableRow(infobox),
                    new TableLayout
                    {
                        Spacing = new Size(5, 5),
                        Rows = { new TableRow(null, but_ok, but_cancel) }
                    }
                }
            };

            // Events
            but_ok.Click += (s, e) => m_Dialog.Close(DialogResult.Ok);
            but_cancel.Click += (s, e) => m_Dialog.Close(DialogResult.Cancel);
            sets.CheckedChanged += (s, e) => Sets_CheckedChanged(sets, qty);

            // show the window
            m_Dialog.Content = cont;
            m_Dialog.ShowModal(RhinoEtoApp.MainWindow);
        }

        private void Sets_CheckedChanged(CheckBox sender, Label qty)
        {
            XMLInfo.IsSets = sender.Checked != null && sender.Checked != false;
            qty.Text = XMLInfo.GetPartQty;
        }

        public DialogResult CommandResult { get { return m_Dialog.Result; } }
        public XML_Info GetXML_Info { get { return XMLInfo; } }
    }
}