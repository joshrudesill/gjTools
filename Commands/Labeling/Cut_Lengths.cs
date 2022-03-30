using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Geometry;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Input;
using Rhino.UI;

namespace gjTools.Commands
{
    public class Cut_Lengths : Command
    {
        /// <summary>
        /// Load this up with objects and you get Cut Lengths
        /// </summary>
        public struct CutLengths
        {
            private List<ObjRef> _obj;

            /// <summary>
            /// This isnt available until you load 
            /// <para>objects in or assign it yourself</para>
            /// </summary>
            public RhinoDoc doc;

            /// <summary>
            /// Add objects to this first
            /// </summary>
            public List<ObjRef> obj
            {
                get { return _obj; }
                set { 
                    _obj = value; 
                    if (_obj.Count > 0)
                        doc = _obj[0].Object().Document;
                }
            }

            /// <summary>
            /// Support for rhinoObjects instead of the default ObjRef list
            /// </summary>
            public List<RhinoObject> GetRhinoObjects
            {
                get
                {
                    var rObj = new List<RhinoObject>();
                    if (obj.Count == 0)
                        return rObj;
                    else
                        foreach (var o in obj)
                            rObj.Add(o.Object());
                    return rObj;
                }
                set
                {
                    List<RhinoObject> rObj = value;
                    if (rObj.Count > 0)
                        foreach (var o in rObj)
                            obj.Add(new ObjRef(o));
                }
            }

            /// <summary>
            /// Gets cut layers from selected objects
            /// </summary>
            public List<string> GetCutLayers
            {
                get
                {
                    var laysName = new List<string>();
                    foreach(var o in _obj)
                    {
                        var obLay = doc.Layers[o.Object().Attributes.LayerIndex];
                        if (!laysName.Contains(obLay.Name) && obLay.Name.Substring(0, 2) == "C_")
                            laysName.Add(obLay.Name);
                    }
                    return laysName;
                }
            }

            /// <summary>
            /// Does what it says
            /// </summary>
            /// <param name="cutLayer"></param>
            /// <returns></returns>
            public int GetCutLength(string cutLayer, out Layer layer)
            {
                double cutLength = 0;
                layer = null;
                foreach(var o in _obj)
                    if (o.Curve() != null && cutLayer == doc.Layers[o.Object().Attributes.LayerIndex].Name)
                    {
                        cutLength += o.Curve().GetLength();
                        if (layer == null)
                            layer = doc.Layers[o.Object().Attributes.LayerIndex];
                    }

                return (int)cutLength;
            }
        }



        public Cut_Lengths()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static Cut_Lengths Instance { get; private set; }

        public override string EnglishName => "KerfSelected";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // get some objects
            if (RhinoGet.GetMultipleObjects("Select Objects", false, ObjectType.Curve, out ObjRef[] obj) != Result.Success)
                return Result.Cancel;

            var objCt = new CutLengths();
                objCt.obj = new List<ObjRef>(obj);

            // get the kerf from the selected
            MakeTextColored(objCt, Point3d.Origin);

            return Result.Success;
        }


        /// <summary>
        /// Makes the cut length block by Color
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="pt"></param>
        public void MakeTextColored(CutLengths obj, Point3d pt)
        {
            var dt = new DrawTools(obj.doc);
            var ds = dt.StandardDimstyle();

            if (pt == Point3d.Origin)
                RhinoGet.GetPoint("Place CutLengths", false, out pt);

            foreach (var l in obj.GetCutLayers)
            {
                var objId = obj.doc.Objects.AddText(dt.AddText(l.Substring(2) + ": " + obj.GetCutLength(l, out Layer lay), pt, ds, 1, 0, 2, 6));
                var rObj = obj.doc.Objects.FindId(objId);
                    rObj.Attributes.PlotColorSource = ObjectPlotColorSource.PlotColorFromObject;
                    rObj.Attributes.PlotColor = lay.Color;
                    rObj.Attributes.ObjectColor = lay.Color;
                    rObj.Attributes.ColorSource = ObjectColorSource.ColorFromObject;
                    rObj.CommitChanges();
                pt.Y -= 1.75;
            }
        }
    }
}