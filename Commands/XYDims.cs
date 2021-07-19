using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.DocObjects;
using Rhino.Input;
using Rhino.Input.Custom;

namespace gjTools.Commands
{
    public class XYDims : Command
    {
        public XYDims()
        {
            Instance = this;
        }

        public static XYDims Instance { get; private set; }

        public override string EnglishName => "XYDims";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            int dimlevel = 1;

            var obj = CollectObjects(ref dimlevel);
            if (obj.Count == 0)
                return Result.Cancel;

            //  Get the needed layer
            var lay = GetObjectData(obj, out BoundingBox bb);

            // create the dimensions
            Point3d[] pts = bb.GetCorners();

            // add the dims to the drawing and swap the layer
            var dims = new List<Guid> { 
                doc.Objects.AddLinearDimension(MakeDim(doc.DimStyles.Current, pts[3], pts[2], dimlevel)), 
                doc.Objects.AddLinearDimension(MakeDim(doc.DimStyles.Current, pts[0], pts[3], dimlevel, true))
            };
            foreach(var d in dims)
            {
                var o = doc.Objects.FindId(d);
                    o.Attributes.LayerIndex = lay.Index;
                    o.CommitChanges();
            }
                                                    
            doc.Views.Redraw();                     
            return Result.Success;                  
        }



        /// <summary>
        /// Makes a linear dimension 
        /// <para>Vertical Dim extends to the left</para>
        /// <para>Horizontal Dim extends above</para>
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="dimlevel"></param>
        /// <param name="dimVertical"></param>
        /// <returns></returns>
        public LinearDimension MakeDim(DimensionStyle ds, Point3d start, Point3d end, int dimlevel = 1, bool dimVertical = false)
        {
            Plane p = Plane.WorldXY;
            Vector3d v = Vector3d.XAxis;
            Point3d offset = start;
                    offset.X += start.DistanceTo(end) / 2;
                    offset.Y += (ds.TextHeight * ds.DimensionScale * 2.25) * dimlevel;

            if (dimVertical)
            {
                p = new Plane(start, Vector3d.YAxis, Vector3d.XAxis);
                v = Vector3d.YAxis;
                offset = start;
                offset.Y += start.DistanceTo(end) / 2;
                offset.X -= (ds.TextHeight * ds.DimensionScale * 2) * dimlevel;
            }

            return LinearDimension.Create( AnnotationType.Aligned, ds, p, v, start, end, offset, 0 );
        }

        /// <summary>
        /// Ask for user input either a number and/or the objects
        /// <para>TODO: make dimlevel part of the datastore in the database</para>
        /// </summary>
        /// <param name="dimLevel"></param>
        /// <returns></returns>
        public List<ObjRef> CollectObjects(ref int dimLevel)
        {
            var obj = new List<ObjRef>();

            // TODO: make dimlevel part of the datastore in the database

            var go = new GetObject();
                go.SetCommandPrompt($"Select Objects <Dim Level = {dimLevel}>");
                go.AcceptNumber(true, false);
                go.GroupSelect = true;

            while (true)
            {
                var res = go.GetMultiple(1, 0);
                if (res == GetResult.Number)
                {
                    dimLevel = (int)go.Number();
                    go.SetCommandPrompt($"Select Objects <Dim Level = {dimLevel}>");
                }
                else if (res == GetResult.Object)
                {
                    obj.AddRange(go.Objects());
                    break;
                }
                else
                {
                    break;
                }
            }

            return obj;
        }

        /// <summary>
        /// Get the parent layer and produce a bounding box from input geometry
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="bb"></param>
        /// <returns></returns>
        public Layer GetObjectData(List<ObjRef> obj, out BoundingBox bb)
        {
            var doc = obj[0].Object().Document;

            var lay = doc.Layers[obj[0].Object().Attributes.LayerIndex];
            if (lay.ParentLayerId != Guid.Empty)
                lay = doc.Layers.FindId(lay.ParentLayerId);

            // collect the objects
            bb = obj[0].Geometry().GetBoundingBox(true);
            foreach (var o in obj)
                bb.Union(o.Geometry().GetBoundingBox(true));

            return lay;
        }
    }
}