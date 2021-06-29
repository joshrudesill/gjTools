using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino;
using Rhino.Geometry;
using Rhino.DocObjects;
namespace gjTools.Helpers
{
    public struct ObjectData
    {
        public Layer parent;
        public Layer subLayer;
        public ObjRef obRef;
        public RhinoObject rhObj;
        public BoundingBox bb
        {
            get
            {
                return obRef.Object().Geometry.GetBoundingBox(true);
            }
        }
        public double W
        {
            get
            {
                return bb.GetEdges()[0].Length;
            }
        }
        public double H
        {
            get
            {
                return bb.GetEdges()[1].Length;
            }
        }
        public ObjectData(ObjRef or, Layer parent, Layer sub)
        {
            obRef = or;
            this.parent = parent;
            subLayer = sub;
            rhObj = or.Object();
        }
    }
    public struct LayerData
    {
        public Dictionary<Layer, List<ObjectData>> layerdata;
        private Layer parent;
        private LayerTools lt;
        public LayerData(Layer layer, RhinoDoc doc)
        {
            parent = layer;
            layerdata = new Dictionary<Layer, List<ObjectData>>();
            lt = new LayerTools(doc);
            var sublayers = lt.getAllSubLayers(parent);
            if (sublayers == null)
            {
                throw new Exception("Sublayer Exception: Parent layer contains no sublayers!");
            }
            foreach (var sl in sublayers)
            {
                RhinoApp.WriteLine("SubLayer");

                var obs = doc.Objects.FindByLayer(sl);
                var lta = new List<ObjectData>();
                if (obs != null)
                {
                    foreach (var o in obs)
                    {
                        RhinoApp.WriteLine("Adding object");
                        var od = new ObjectData(new ObjRef(o), parent, sl);
                        lta.Add(od);
                    }
                    layerdata[parent] = lta;
                }
                else
                {
                    layerdata[parent] = null;
                }
            }
        }
        public List<Layer> getSublayers()
        {
            var ltr = new List<Layer>();
            foreach(var ld in layerdata)
            {
                ltr.Add(ld.Key);
            }
            return ltr;
        }
        public List<ObjectData> getAllObjectsOnSubLayer(Layer sl)
        {
            return layerdata[sl];
        }
        public IEnumerable<RhinoObject> getObjectOfType(Layer layer, ObjectType ot)
        {
            var oes = new ObjectEnumeratorSettings();
            oes.LayerIndexFilter = layer.Index;
            oes.ObjectTypeFilter = ot;
            var doc = RhinoDoc.ActiveDoc;
            return doc.Objects.GetObjectList(oes);
        }
    }
}
