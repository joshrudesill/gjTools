using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.DocObjects;
using Rhino.Input;
using Rhino.UI;

namespace gjTools.Commands
{
    public class EP_CutSide2 : Command
    {
        public EP_CutSide2()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static EP_CutSide2 Instance { get; private set; }

        public override string EnglishName => "EPCutSideTwo";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // get a list of the parent layers to choose from
            var parentLayers = new List<Layer>();
            var cutIndex = 0;
            foreach (Layer l in doc.Layers)
                if (l.ParentLayerId == Guid.Empty && !l.IsDeleted)
                {
                    parentLayers.Add(l);
                    if (l.Name == "CUT")
                        cutIndex = parentLayers.Count - 1;
                }
            
            // ask for layer selection
            Layer cutLayer = (Layer)Dialogs.ShowListBox("Make a CUT2", "Select Cut Layer to Flip", parentLayers, parentLayers[cutIndex]);
            if (cutLayer == null)
                return Result.Cancel;

            // select objects
            var cutObj = new List<RhinoObject>();
            var childLayers = cutLayer.GetChildren();
            
            // grab the markers
            int markLayerIndex = doc.Layers.FindByFullPath("MARKERS", -1);
            if (markLayerIndex != -1)
            {
                var sett = new ObjectEnumeratorSettings { LayerIndexFilter = markLayerIndex, ObjectTypeFilter = ObjectType.TextDot };
                cutObj.AddRange(doc.Objects.FindByFilter(sett));
            }

            // Get only the cut lines
            foreach (Layer l in childLayers)
                if (l.Name.StartsWith("C_") && l.Name != "C_TEXT")
                    cutObj.AddRange( doc.Objects.FindByLayer(l) );

            // new Cut layer
            var lt = new LayerTools(doc);
            Layer newParentLayer = lt.CreateLayer("CUT2");

            // mind the groups if there are any
            var GroupManager = new StraitBoxGroupManager(doc);

            // Cycle the layers and duplicate the Objects
            foreach (RhinoObject o in cutObj)
            {
                Guid id = doc.Objects.Duplicate(o);
                var newObj = doc.Objects.FindId(id);
                Layer clay = doc.Layers[newObj.Attributes.LayerIndex];
                if (clay.Index != markLayerIndex)
                    clay = lt.CreateLayer(clay.Name, newParentLayer.Name, clay.Color);

                // swap the color if on the thru layer
                Swap_RHLH.FlipColorRHLH(clay, newObj);

                // see if groups are present
                if (newObj.Attributes.GroupCount > 0)
                {
                    int newGroup = GroupManager.TranslateGroup(newObj.Attributes.GetGroupList()[0]);
                    newObj.Attributes.RemoveFromAllGroups();
                    newObj.Attributes.AddToGroup(newGroup);
                }

                newObj.Geometry.Transform(Transform.Mirror(new Point3d(0, 150, 0), Vector3d.YAxis));
                newObj.Attributes.LayerIndex = clay.Index;
                newObj.CommitChanges();
            }

            doc.Views.Redraw();
            return Result.Success;
        }
    }
}