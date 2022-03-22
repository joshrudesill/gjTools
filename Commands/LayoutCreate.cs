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
            var lay = lt.CreateLayer("Detail", System.Drawing.Color.Black);

            // objects size
            BoundingBox bb = BoundingBox.Empty;
            foreach (var o in oRef)
                bb.Union(o.Geometry().GetBoundingBox(true));

            // Size of the objects
            double s_Width = bb.GetEdges()[0].Length * scale;
            double s_Height = bb.GetEdges()[1].Length * scale;

            // make page slightly larger so the border goes with it
            var layout = doc.Views.AddPageView(layoutName, s_Width + 0.02, s_Height + 0.02);
            var detail = layout.AddDetailView(layoutName, 
                new Point2d(0.01,0.01), 
                new Point2d(s_Width + 0.01, s_Height + 0.01), 
                Rhino.Display.DefinedViewportProjection.Top);

            // set the page as active
            doc.Views.ActiveView = layout;

            // set the page as active
            layout.SetActiveDetail(detail.Id);
            doc.Views.ActiveView = detail.Viewport.ParentView;
            detail.Viewport.ZoomBoundingBox(bb);
            layout.SetPageAsActive();
            doc.Views.ActiveView = layout;

            // set the datail to have color for border reasons
            detail.Attributes.LayerIndex = lay.Index;
            detail.Attributes.ColorSource = ObjectColorSource.ColorFromObject;
            detail.Attributes.PlotColorSource = ObjectPlotColorSource.PlotColorFromObject;
            detail.Attributes.ObjectColor = System.Drawing.Color.Black;
            detail.Attributes.PlotColor = System.Drawing.Color.Black;
            detail.Attributes.PlotWeightSource = ObjectPlotWeightSource.PlotWeightFromObject;
            detail.Attributes.PlotWeight = 0.5;

            // set the layer and scale info
            detail.DetailGeometry.SetScale(1, doc.ModelUnitSystem, scale, doc.PageUnitSystem);
            detail.CommitChanges();

            // recommit the locked prjection 
            detail.DetailGeometry.IsProjectionLocked = true;
            detail.CommitChanges();
            
            doc.Views.Redraw();
            return Result.Success;
        }
    }
}