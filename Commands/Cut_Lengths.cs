using System;
using System.Collections.Generic;
using Rhino;
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
            private int _CutIterator;
            /// <summary>
            /// This isnt available until you load objects in
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
                    _CutIterator = 0;
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
            /// Each time this is read, it progresses to the next cut type or null if none
            /// </summary>
            public double NextCutType
            {
                get
                {
                    return 0.25;
                }
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

            return Result.Success;
        }
    }
}