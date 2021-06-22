using System;
using Rhino;
using Rhino.Commands;
using System.Collections.Generic;
using Rhino.Geometry;
namespace gjTools.Commands
{
    public class AddPNText : Command
    {
        public AddPNText()
        {
            Instance = this;
        }

        public static AddPNText Instance { get; private set; }

        public override string EnglishName => "AddPNText";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            DialogTools d = new DialogTools(doc);
            LayerTools lt = new LayerTools(doc);
            List<Rhino.DocObjects.Layer> ll = new List<Rhino.DocObjects.Layer>();
            List<Rhino.DocObjects.RhinoObject> ro = new List<Rhino.DocObjects.RhinoObject>();
            var go  = d.selectObjects("Select object(s) to add PN tag to");
            if (go == null)
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
            BoundingBox bb;
            Rhino.DocObjects.RhinoObject.GetTightBoundingBox(ro, out bb);
            var edges = bb.GetEdges();
            var corners = bb.GetCorners();
            Point3d pt = new Point3d(corners[3].X, corners[3].Y + edges[2].Length / 40, 0);
            Plane plane = doc.Views.ActiveView.ActiveViewport.ConstructionPlane();
            plane.Origin = pt;
            doc.Objects.AddText("PN: " + la.ToString(), plane, edges[2].Length / 500, "Arial", false, false);
            return Result.Success;
        }
    }
}