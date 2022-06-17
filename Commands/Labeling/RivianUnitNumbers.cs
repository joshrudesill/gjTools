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
            StatusBar.ShowProgressMeter(0, total, "Progress", false, true);

            // start making the new layouts
            for (int i = 0, ii = 0; i < total; i += 4, ii++)
            {
                string u1 = unitNumbers[i];
                string u2 = (i + 1 < total) ? unitNumbers[i + 1] : u1;
                string u3 = (i + 2 < total) ? unitNumbers[i + 2] : u2;
                string u4 = (i + 3 < total) ? unitNumbers[i + 3] : u3;

                var newParent = lt.CreateLayer($"{parent.Name}_L{ii}");
                StatusBar.UpdateProgressMeter(i, true);

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
            StatusBar.HideProgressMeter();

            doc.Views.Redraw();
            return Result.Success;
        }
    }
}