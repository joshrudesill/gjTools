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

        public override string EnglishName => "EPMakeCutSideTwo";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var lt = new LayerTools(doc);

            string cutName = (string)Dialogs.ShowListBox("Make CutSid 2", "Choose a Cut Layer to Flip", lt.getAllParentLayersStrings());
            if (cutName == null)
                return Result.Cancel;

            var parentLay = lt.CreateLayer(cutName);
            var parentChildren = parentLay.GetChildren();
            var cutSide2Lay = lt.CreateLayer(cutName + "SIDE2");
            var childLays = new List<Layer>();
            var nestBoxBB = BoundingBox.Empty;
            
            // create new layer and children
            foreach(var l in parentChildren)
            {
                childLays.Add(lt.CreateLayer(l.Name, cutSide2Lay.Name, l.Color));

                if (l.Name == "NestBox")
                    nestBoxBB = doc.Objects.FindByLayer(l)[0].Geometry.GetBoundingBox(true);
            }

            // copy and mirror objects
            var mirror = Transform.Mirror(new Plane(nestBoxBB.Center, Vector3d.YAxis));
            for(var i = 0; i < childLays.Count; i++)
            {
                var obj = doc.Objects.FindByLayer(parentChildren[i]);
                foreach (var o in obj)
                {
                    var id = doc.Objects.Transform(o, mirror, false);
                    var copy = doc.Objects.FindId(id);
                        copy.Attributes.LayerIndex = childLays[i].Index;
                        copy.CommitChanges();
                }
            }

            doc.Views.Redraw();
            return Result.Success;
        }
    }
}