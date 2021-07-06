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
        public Tuple<Layer, List<Tuple<Layer, List<ObjectData>>>> layerdata;

        private readonly Layer parent;
        private readonly LayerTools lt;
        public LayerData(Layer layer, RhinoDoc doc)
        {
            parent = layer;
            lt = new LayerTools(doc);
            layerdata = Tuple.Create(new Layer(), new List<Tuple<Layer, List<ObjectData>>>());
            var sublayers = lt.getAllSubLayers(parent);
            if (sublayers == null)
            {
                throw new Exception("Sublayer Exception: Parent layer contains no sublayers!");
            }
            var tlta = new List<Tuple<Layer, List<ObjectData>>>();
            foreach (var sl in sublayers)
            {
                var obs = doc.Objects.FindByLayer(sl);
                var lta = new List<ObjectData>();
                
                if (obs != null)
                {
                    foreach (var o in obs)
                    {
                        var od = new ObjectData(new ObjRef(o), parent, sl);
                        lta.Add(od);
                    }
                    var tta = Tuple.Create(sl, lta);
                    tlta.Add(tta);
                }
                else
                {
                    var tta = Tuple.Create(sl, new List<ObjectData>());
                    tlta.Add(tta);
                }
            }
            layerdata = Tuple.Create(parent, tlta);
        }
        public List<Layer> getSubLayers()
        {
            var ltr = new List<Layer>();
            foreach (var i in layerdata.Item2)
            {
                ltr.Add(i.Item1);
            }
            return ltr;
        }
        public List<ObjectData> getAllObjectsOnSubLayer(Layer sl)
        {
            List<ObjectData> lta = new List<ObjectData>();
            foreach(var lay in layerdata.Item2)
            {
                if (lay.Item1 == sl)
                {
                    foreach(var ita in lay.Item2)
                    {
                        lta.Add(ita);
                    }
                    break;
                }
            }
            return lta;
        }
        public IEnumerable<RhinoObject> getObjectOfType(Layer layer, ObjectType ot)
        {
            var oes = new ObjectEnumeratorSettings();
            oes.LayerIndexFilter = layer.Index;
            oes.ObjectTypeFilter = ot;
            var doc = RhinoDoc.ActiveDoc;
            return doc.Objects.GetObjectList(oes);
        }

        public List<RhinoObject> getRhinoObjectsOnSubLayer(Layer sl)
        {
            List<RhinoObject> lta = new List<RhinoObject>();
            foreach (var lay in layerdata.Item2)
            {
                if (lay.Item1 == sl)
                {
                    foreach (var ita in lay.Item2)
                    {
                        lta.Add(ita.rhObj);
                    }
                    break;
                }
            }
            return lta;
        }
        public BoundingBox getBoundingBoxofParent()
        {
            BoundingBox bbtr;
            List<RhinoObject> ltbb = new List<RhinoObject>();
            foreach (var tl in layerdata.Item2)
            {
                foreach (var i in tl.Item2)
                {
                    ltbb.Add(i.rhObj);
                }
            }
            ltbb.AddRange(RhinoDoc.ActiveDoc.Objects.FindByLayer(layerdata.Item1));
            RhinoObject.GetTightBoundingBox(ltbb, out bbtr);
            return bbtr;
        }
    }
}
