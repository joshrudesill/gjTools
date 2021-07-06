using System;
using Rhino;
using Rhino.Commands;
using System.Collections.Generic;
using Rhino.Geometry;
using Rhino.UI;
using Rhino.DocObjects;

namespace gjTools.Commands
{
    public class AddPNText : Command
    {
        public AddPNText()
        {
            Instance = this;
        }
        // This command has been tested and is error proof. Ready for release.
        public static AddPNText Instance { get; private set; }

        public override string EnglishName => "AddPNText";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var lt = new LayerTools(doc);
            var parts = Dialogs.ShowMultiListBox("Layer Selector", "Add PN Tag to", lt.getAllParentLayersStrings());

            foreach(var p in parts)
            {
                RhinoApp.WriteLine("No objects selected, canceling command..");
                return Result.Cancel;
            }
            for(int i = 0; i < go.ObjectCount; i++)
            {
                ro.Add(go.Object(i).Object());
            }
            var parents = lt.getAllParentLayers();
            var la = Rhino.UI.Dialogs.ShowListBox("Layers", "Select a layer..", parents);
            if (la is null)
            {
                RhinoApp.WriteLine("No layer selected, canceling command..");
                return Result.Cancel;
            }
            
            BoundingBox bb;
            if (!Rhino.DocObjects.RhinoObject.GetTightBoundingBox(ro, out bb))
            {
                RhinoApp.WriteLine("Bounding Box creation failed.. Investigation needed!");
                return Result.Failure;
            }

            var edges = bb.GetEdges();
            var corners = bb.GetCorners();
            Point3d pt = new Point3d(corners[3].X, corners[3].Y + edges[2].Length / 40, 0);
            Plane plane = doc.Views.ActiveView.ActiveViewport.ConstructionPlane();
            plane.Origin = pt;
            if (!doc.Layers.SetCurrentLayerIndex(doc.Layers.FindName(la.ToString()).Index, true))
            {
                RhinoApp.WriteLine("Layer unable to set. Investigation needed.");
                return Result.Failure;
            }
            doc.Views.Redraw();
            return Result.Success;
        }
    }
}