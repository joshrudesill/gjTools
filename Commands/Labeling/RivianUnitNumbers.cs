using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.DocObjects;
using Rhino.UI;

namespace gjTools.Commands.Labeling
{
    public class RivianUnitNumbers : Command
    {
        public RivianUnitNumbers()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static RivianUnitNumbers Instance { get; private set; }

        public override string EnglishName => "RivianUnitNumbers";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // check the filename
            if (doc.Name != "21-80300A.3dm")
            {
                RhinoApp.WriteLine("Only to be used on the correct part:  21-80300A.3dm");
                return Result.Failure;
            }

            // get the original block
            if (RhinoGet.GetMultipleObjects("Select the original Unit number layout", false, ObjectType.AnyObject, out ObjRef[] unitNest) != Result.Success)
                return Result.Cancel;

            // Ask what version layout is being used
            int blockSize = 4;
            if (RhinoGet.GetInteger("8 or 4 Set Layout being used?", true, ref blockSize, 4, 8) != Result.Success)
                return Result.Cancel;
            blockSize = (blockSize != 4 && blockSize != 8) ? 4 : blockSize;

            // get the list of parts
            if (!Dialogs.ShowEditBox("List Import", "Paste Here", "Each Line will be a new part...", true, out string rawInput))
                return Result.Cancel;

            // Parse the unit numbers
            var unitNumbers = new List<string>(rawInput.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries));

            // constant
            string RivianPartNumber = "PT00447176-A";

            // sort the objects
            // get the objects that need changing
            int total = unitNumbers.Count;
            
            // find the parent layer
            Layer parent = doc.Layers[unitNest[0].Object().Attributes.LayerIndex];
            if (parent.ParentLayerId != Guid.Empty)
                parent = doc.Layers.FindId(parent.ParentLayerId);

            // layertools needed
            var lt = new LayerTools(doc);

            // start making the new layouts
            for (int i = 0, ii = 0; i < total; i += blockSize, ii++)
            {
                string u1 = unitNumbers[i];
                string u2 = (i + 1 < total) ? unitNumbers[i + 1] : u1;
                string u3 = (i + 2 < total) ? unitNumbers[i + 2] : u2;
                string u4 = (i + 3 < total) ? unitNumbers[i + 3] : u3;
                string u5 = (i + 4 < total) ? unitNumbers[i + 4] : u4;
                string u6 = (i + 5 < total) ? unitNumbers[i + 5] : u5;
                string u7 = (i + 6 < total) ? unitNumbers[i + 6] : u6;
                string u8 = (i + 7 < total) ? unitNumbers[i + 7] : u7;

                var newParent = lt.CreateLayer($"{parent.Name}_L{ii}");

                // should only be one group
                int group = doc.Groups.Add();

                foreach (var o in unitNest)
                {
                    var robj = o.Object();
                    var attr = robj.Attributes;

                    if (attr.GroupCount > 0)
                    {
                        attr.RemoveFromAllGroups();
                        attr.AddToGroup(group);
                    }
                    
                    // child layer reassign
                    if (doc.Layers[robj.Attributes.LayerIndex] != parent)
                    {
                        var cl = lt.CreateLayer(doc.Layers[robj.Attributes.LayerIndex].Name, newParent.Name, doc.Layers[robj.Attributes.LayerIndex].Color);
                        attr.LayerIndex = cl.Index;
                    }
                    else
                        attr.LayerIndex = newParent.Index;

                    TextEntity num = o.TextEntity();
                    switch (attr.Name)
                    {
                        case "UNIT1":
                            num.RichText = num.RichText.Replace("UNIT", u1);
                            doc.Objects.AddText(num, attr);
                            break;
                        case "UNIT2":
                            num.RichText = num.RichText.Replace("UNIT", u2);
                            doc.Objects.AddText(num, attr);
                            break;
                        case "UNIT3":
                            num.RichText = num.RichText.Replace("UNIT", u3);
                            doc.Objects.AddText(num, attr);
                            break;
                        case "UNIT4":
                            num.RichText = num.RichText.Replace("UNIT", u4);
                            doc.Objects.AddText(num, attr);
                            break;
                        case "UNIT5":
                            num.RichText = num.RichText.Replace("UNIT", u5);
                            doc.Objects.AddText(num, attr);
                            break;
                        case "UNIT6":
                            num.RichText = num.RichText.Replace("UNIT", u6);
                            doc.Objects.AddText(num, attr);
                            break;
                        case "UNIT7":
                            num.RichText = num.RichText.Replace("UNIT", u7);
                            doc.Objects.AddText(num, attr);
                            break;
                        case "UNIT8":
                            num.RichText = num.RichText.Replace("UNIT", u8);
                            doc.Objects.AddText(num, attr);
                            break;
                        case "LABEL1":
                            num.RichText = num.RichText.Replace("RIVPART", RivianPartNumber);
                            num.RichText = num.RichText.Replace("UNIT", u1);
                            doc.Objects.AddText(num, attr);
                            break;
                        case "LABEL2":
                            num.RichText = num.RichText.Replace("RIVPART", RivianPartNumber);
                            num.RichText = num.RichText.Replace("UNIT", u2);
                            doc.Objects.AddText(num, attr);
                            break;
                        case "LABEL3":
                            num.RichText = num.RichText.Replace("RIVPART", RivianPartNumber);
                            num.RichText = num.RichText.Replace("UNIT", u3);
                            doc.Objects.AddText(num, attr);
                            break;
                        case "LABEL4":
                            num.RichText = num.RichText.Replace("RIVPART", RivianPartNumber);
                            num.RichText = num.RichText.Replace("UNIT", u4);
                            doc.Objects.AddText(num, attr);
                            break;
                        case "LABEL5":
                            num.RichText = num.RichText.Replace("RIVPART", RivianPartNumber);
                            num.RichText = num.RichText.Replace("UNIT", u5);
                            doc.Objects.AddText(num, attr);
                            break;
                        case "LABEL6":
                            num.RichText = num.RichText.Replace("RIVPART", RivianPartNumber);
                            num.RichText = num.RichText.Replace("UNIT", u6);
                            doc.Objects.AddText(num, attr);
                            break;
                        case "LABEL7":
                            num.RichText = num.RichText.Replace("RIVPART", RivianPartNumber);
                            num.RichText = num.RichText.Replace("UNIT", u7);
                            doc.Objects.AddText(num, attr);
                            break;
                        case "LABEL8":
                            num.RichText = num.RichText.Replace("RIVPART", RivianPartNumber);
                            num.RichText = num.RichText.Replace("UNIT", u8);
                            doc.Objects.AddText(num, attr);
                            break;
                        case "LAYERNAME":
                            num.RichText = num.RichText.Replace(parent.Name, newParent.Name);
                            doc.Objects.AddText(num, attr);
                            break;

                        default:
                            doc.Objects.Add(robj.Geometry, attr);
                            break;
                    }
                }
            }

            doc.Views.Redraw();
            return Result.Success;
        }
    }
}