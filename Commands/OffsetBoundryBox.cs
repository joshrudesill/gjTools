using System;
using Rhino;
using Rhino.Commands;
using System.Collections.Generic;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input.Custom;

namespace gjTools.Commands
{
    public class OffsetBoundryBox : Command
    {
        public double offset = 0.25;

        public OffsetBoundryBox()
        {
            Instance = this;
        }
        public static OffsetBoundryBox Instance { get; private set; }

        public override string EnglishName => "OffsetBoundryBox";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var lt = new LayerTools(doc);

            // Make the box
            MakeOffsetBox(doc, lt);

            doc.Views.Redraw();
            return Result.Success;
        }



        /// <summary>
        /// Asks for objects to put a box around
        /// <para>If kiss or thru, than the result is opposite</para>
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="lt"></param>
        /// <returns></returns>
        public bool MakeOffsetBox(RhinoDoc doc, LayerTools lt)
        {
            var go = new GetObject();
            go.SetCommandPrompt("Select Objects <Offset=" + offset + ">");
            go.GeometryFilter = ObjectType.Curve;
            go.AcceptNumber(true, true);

            while (true)
            {
                var res = go.GetMultiple(1, 0);
                if (res == Rhino.Input.GetResult.Cancel)
                    return false;
                if (res == Rhino.Input.GetResult.Number)
                {
                    offset = go.Number();
                    go.SetCommandPrompt("Select Objects <Offset=" + offset + ">");
                }
                else if (res == Rhino.Input.GetResult.Object)
                {
                    RhinoApp.WriteLine("Preselected Objects completes with Default 0.25\" Offset");
                    break;
                }
                else
                    return false;
            }

            var obj = new List<ObjRef>(go.Objects());
            BoundingBox bb = obj[0].Curve().GetBoundingBox(true);

            // Test one object for layer and cuttype
            var layers = lt.isObjectOnCutLayer(obj[0].Object(), true);
            var boxLayer = lt.ObjLayer(obj[0].Object());
            if (layers.Count > 1)
            {
                if (layers[1].Name == "C_KISS")
                    boxLayer = lt.CreateLayer("C_THRU", layers[0].Name, System.Drawing.Color.Red);
                else if (layers[1].Name == "C_THRU")
                    boxLayer = lt.CreateLayer("C_KISS", layers[0].Name, System.Drawing.Color.FromArgb(255, 200, 0, 200));
            }

            // update boundingbox and offset
            foreach (var o in obj)
                bb.Union(o.Curve().GetBoundingBox(true));
            bb.Inflate(offset);

            // create objects and assign layer
            Guid id = doc.Objects.AddRectangle(new Rectangle3d(Plane.WorldXY, bb.GetCorners()[0], bb.GetCorners()[2]));
            var newRect = doc.Objects.FindId(id);
                newRect.Attributes.LayerIndex = boxLayer.Index;
                newRect.CommitChanges();

            return true;
        }
    }
}