using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace gjTools.Commands.Drawing_Tools
{
    public class GroupObjectsWithinObject : Command
    {
        public GroupObjectsWithinObject()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static GroupObjectsWithinObject Instance { get; private set; }

        public override string EnglishName => "GroupObjectsWithinObject";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            if (RhinoGet.GetOneObject("Select the Bounding object to group the objects within", false, ObjectType.Curve, out ObjRef GroupObj) != Result.Success)
                return Result.Cancel;

            doc.Objects.UnselectAll();
            List<Point3d> pts = new List<Point3d>( GroupObj.Geometry().GetBoundingBox(true).GetCorners() );

            var objs = doc.Objects.FindByWindowRegion(doc.Views.ActiveView.ActiveViewport, pts.GetRange(0, 5), true, ObjectType.AnyObject);
            int grp = doc.Groups.Add();
            foreach(var obj in objs)
            {
                obj.Attributes.RemoveFromAllGroups();
                obj.Attributes.AddToGroup(grp);
                obj.CommitChanges();
                doc.Objects.Select(obj.Id);
            }

            RhinoApp.WriteLine($"Grouped {objs.Length} Objects");

            return Result.Success;
        }
    }
}