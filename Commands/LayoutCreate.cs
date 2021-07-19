using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace gjTools.Commands
{
    public class LayoutCreate : Command
    {
        public LayoutCreate()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static LayoutCreate Instance { get; private set; }

        public override string EnglishName => "LayoutCreate";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var lt = new LayerTools(doc);
            string layoutName = "LAYOUT";
            double scale = 1;

            // get the name of the new layout
            var res = RhinoGet.GetString("Layout Name", false, ref layoutName);
            if (res != Result.Success)
                return Result.Cancel;

            layoutName = layoutName.ToUpper();

            //  see if it already exists
            var views = doc.Views.GetPageViews();
            foreach(var p in views)
                if (p.MainViewport.Name == layoutName)
                {
                    RhinoApp.WriteLine("Page by that name already exists, choose another...");
                    return Result.Cancel;
                }

            // ask for scale on layout
            res = RhinoGet.GetNumber("Page Scale", false, ref scale, 0, 1);
            if (res != Result.Success)
                return Result.Cancel;

            // get some objects
            res = RhinoGet.GetMultipleObjects("Select Objects", false, ObjectType.AnyObject, out ObjRef[] oRef);
            if (res != Result.Success)
                return Result.Cancel;

            // Make detail Layer
            var lay = lt.CreateLayer("Detail", System.Drawing.Color.White);

            // objects size
            BoundingBox bb = oRef[0].Object().Geometry.GetBoundingBox(true);
            foreach (var o in oRef)
                bb.Union(o.Object().Geometry.GetBoundingBox(true));

            // make page
            var layout = doc.Views.AddPageView(layoutName, bb.GetEdges()[0].Length * scale, bb.GetEdges()[1].Length * scale);
            var detail = layout.AddDetailView(layoutName, 
                new Point2d(0,0), 
                new Point2d(bb.GetEdges()[0].Length * scale, bb.GetEdges()[1].Length * scale), 
                Rhino.Display.DefinedViewportProjection.Top);

            layout.SetPageAsActive();               //activate the page
            doc.Views.ActiveView = layout;          //set the viewport to see the page
            layout.SetActiveDetail(detail.Id);      //set the detail to active viewport
            detail.Viewport.ZoomBoundingBox(bb);    // make the zoomyzoom
            layout.SetPageAsActive();               // make the page active again
            doc.Views.ActiveView = layout;

            detail.DetailGeometry.SetScale(1, doc.ModelUnitSystem, scale, doc.PageUnitSystem);
            detail.Attributes.LayerIndex = lay.Index;
            detail.DetailGeometry.IsProjectionLocked = true;
            detail.CommitChanges();

            doc.Views.Redraw();
            return Result.Success;
        }
    }
}