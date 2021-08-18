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
        public int dimlevel = 1;

        public XYDims()
        {
            Instance = this;
        }

        public static XYDims Instance { get; private set; }

        public override string EnglishName => "XYDims";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var obj = CollectObjects();
            if (obj.Count == 0)
                return Result.Cancel;

            //  Get the needed layer
            var lay = GetObjectData(obj, out BoundingBox bb);

            // create the dimensions
            Point3d[] pts = bb.GetCorners();

            // add the dims to the drawing and swap the layer
            var dims = new List<Guid> { 
                doc.Objects.AddLinearDimension(MakeDim(doc.DimStyles.Current, pts[3], pts[2], false, dimlevel)), 
                doc.Objects.AddLinearDimension(MakeDim(doc.DimStyles.Current, pts[0], pts[3], true, dimlevel))
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
        /// <param name="verticalDim"></param>
        /// <param name="dimOffset"></param>
        /// <returns></returns>
        public static LinearDimension MakeDim(DimensionStyle ds, Point3d start, Point3d end, bool verticalDim = false, int dimlevel = 1, double dimOffset = 0)
        {
            var dm_Center = new Line(start, end).PointAtLength(start.DistanceTo(end) / 2);
            
            // Set the correct offset
            dimOffset = (dimOffset > 0) ? dimOffset : ds.TextHeight * ds.DimensionScale * dimlevel * 2.25;

            if (verticalDim)
                dm_Center.X -= dimOffset;
            else
                dm_Center.Y += dimOffset;

            Plane p = new Plane(start, end, dm_Center);
                  p.ClosestParameter(start, out double sX, out double sY);
                  p.ClosestParameter(dm_Center, out double mX, out double mY);
                  p.ClosestParameter(end, out double eX, out double eY);
            
            return new LinearDimension(p, new Point2d(sX, sY), new Point2d(eX, eY), new Point2d(mX, mY)) { Aligned = true, DimensionStyleId = ds.Id };
        }

        /// <summary>
        /// Ask for user input either a number and/or the objects
        /// <para>TODO: make dimlevel part of the datastore in the database</para>
        /// </summary>
        /// <param name="dimLevel"></param>
        /// <returns></returns>
        public List<ObjRef> CollectObjects()
        {
            var obj = new List<ObjRef>();

            var go = new GetObject();
                go.SetCommandPrompt($"Select Objects <Dim Level = {dimlevel}>");
                go.AcceptNumber(true, false);
                go.GroupSelect = true;

            while (true)
            {
                var res = go.GetMultiple(1, 0);
                if (res == GetResult.Number)
                {
                    dimlevel = (int)go.Number();
                    go.SetCommandPrompt($"Select Objects <Dim Level = {dimlevel}>");
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