using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.Geometry;
using Rhino.DocObjects;

namespace gjTools.Commands
{
    public class MirrorCenterQuads : Command
    {
        public MirrorCenterQuads()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static MirrorCenterQuads Instance { get; private set; }

        public override string EnglishName => "MirrorCenterQuads";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            if (RhinoGet.GetMultipleObjects("Select Objects to Mirror", false, ObjectType.AnyObject, out ObjRef[] objs) != Result.Success)
                return Result.Cancel;

            if (RhinoGet.GetPoint("Mirror Axis Point", false, out Point3d pt) != Result.Success)
                return Result.Cancel;

            var v_mirror = Transform.Mirror(new Plane(pt, Vector3d.XAxis));
            var h_mirror = Transform.Mirror(new Plane(pt, Vector3d.YAxis));

            foreach (var o in objs)
            {
                Guid last = doc.Objects.Transform(o.ObjectId, v_mirror, false);
                doc.Objects.Transform(last, h_mirror, false);
                doc.Objects.Transform(o.ObjectId, h_mirror, false);
            }

            return Result.Success;
        }
    }
}