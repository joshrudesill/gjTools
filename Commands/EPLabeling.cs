using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Geometry;
using Rhino.Input.Custom;
using Rhino.DocObjects;
using Rhino.Commands;

namespace gjTools.Commands
{
    public class EPLabeling : Command
    {
        public struct EPBlock
        {
            private RhinoDoc doc;
            private List<RhinoObject> obj;
            public EPBlock(RhinoDoc document, List<RhinoObject> TitleBlock)
            {
                doc = document;
                obj = TitleBlock;
            }
            private string ToTextEntity(RhinoObject totxt)
            {
                var txt = totxt.Geometry as TextEntity;
                return txt.PlainText;
            }
            public string GetVersion
            {
                get
                {
                    foreach (var o in obj)
                    {
                        if (o.Attributes.Name == "version")
                            return ToTextEntity(o).Replace("VERSION: ", "");
                    }
                    return "";
                }
            }
            public string GetPrinter
            {
                get
                {
                    foreach (var o in obj)
                    {
                        if (o.Attributes.Name == "printer")
                            return ToTextEntity(o);
                    }
                    return "";
                }
            }
            public List<string> GetPartNumbers
            {
                get
                {
                    var partNumbers = new List<string>();
                    foreach (var o in obj)
                    {
                        if (o.Attributes.Name == "part")
                        {
                            var txt = ToTextEntity(o).Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                            foreach(var l in txt)
                            {
                                if (l.Substring(0, 4) == "PN: ")
                                    partNumbers.Add(l.Substring(4));
                                else
                                    break;
                            }
                        }
                    }
                    return partNumbers;
                }
            }
        }

        public EPLabeling()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static EPLabeling Instance { get; private set; }

        public override string EnglishName => "gjEPLabeling";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var lt = new LayerTools(doc);
            var titleBlocks = new ObjectEnumeratorSettings 
            {
                LayerIndexFilter = lt.CreateLayer("Title Block").Index,
                ObjectTypeFilter = ObjectType.Annotation
            };
            var labelDots = new ObjectEnumeratorSettings
            {
                ObjectTypeFilter = ObjectType.TextDot,
                NameFilter = "EPLabel"
            };

            var txtObj = new List<RhinoObject>(doc.Objects.GetObjectList(titleBlocks));
            var txtDot = new List<RhinoObject>(doc.Objects.GetObjectList(labelDots));

            var blocks = SortGroups(doc, txtObj);
            
            if (txtDot.Count == 0)
            {
                var cutLayStr = (string)Rhino.UI.Dialogs.ShowListBox("Layer Selector", "Choose the Cut layer", lt.getAllParentLayersStrings());
                if (cutLayStr == null)
                    return Result.Cancel;

                var gp = new GetPoint();
                    gp.SetCommandPrompt("Place the Label Markers");
                    gp.Get();
                Point3d pt = gp.Point();
                Transform m = Transform.Translation(0, -2, 0);
                foreach (var b in blocks)
                {
                    foreach (var p in b.GetPartNumbers)
                    {
                        var dot = new TextDot(p, pt);
                        dot.FontHeight = 12;

                        var id = doc.Objects.FindId(doc.Objects.AddTextDot(dot));
                        id.Name = "EPLabel";
                        id.Attributes.LayerIndex = doc.Layers.FindName(cutLayStr).Index;
                        pt.Transform(m);
                    }
                }
                doc.Views.Redraw();
                return Result.Success;
            }

            var labelMaker = new PrototypeTool();

            foreach(var b in blocks)
            {
                var version = b.GetVersion;
                foreach (var p in b.GetPartNumbers)
                {
                    var pLabel = new OEM_Label(p);
                    foreach (var d in txtDot)
                    {
                        var td = d.Geometry as TextDot;
                        Layer llayer = lt.CreateLayer("C_TEXT-V" + version, doc.Layers[d.Attributes.LayerIndex].Name);
                        labelMaker.PlaceProductionLabel(doc, pLabel, llayer, td.Point);
                    }
                }
            }

            return Result.Success;
        }





        private List<EPBlock> SortGroups(RhinoDoc doc, List<RhinoObject> obj)
        {
            var groups = new List<int>();
            foreach(var o in obj)
            {
                var g = o.Attributes.GetGroupList();
                if (g != null)
                    if (!groups.Contains(g[0]))
                        groups.Add(g[0]);
            }

            var blocks = new List<EPBlock>();
            foreach (int i in groups)
                blocks.Add(new EPBlock(doc, new List<RhinoObject>(doc.Objects.FindByGroup(i))));

            return blocks;
        }
    }
}