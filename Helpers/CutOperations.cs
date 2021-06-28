using Rhino;
using System.Collections.Generic;
public class CutOperations
{
    public List<Rhino.DocObjects.ObjRef> CrvObjects;
    public RhinoDoc doc;
    public Rhino.DocObjects.Layer parentLayer;
    public List<int> groupInd;
    private bool _IsValid;

    public CutOperations(List<Rhino.DocObjects.ObjRef> Crvs, RhinoDoc document)
    {
        CrvObjects = Crvs;
        doc = document;
        _IsValid = true;

        OnlyCurves();
        var singleSubLayer = doc.Layers[CrvObjects[0].Object().Attributes.LayerIndex];
        if (singleSubLayer.ParentLayerId == System.Guid.Empty)
            parentLayer = singleSubLayer;
        else
            parentLayer = doc.Layers.FindId(singleSubLayer.ParentLayerId);
    }


    public bool IsValid { get { return _IsValid; } }

    private void OnlyCurves()
    {
        var tmp = new List<Rhino.DocObjects.ObjRef>();
        groupInd = new List<int>();

        foreach (var i in CrvObjects)
            if (i.Curve() != null)
            {
                tmp.Add(i);

                // count groups (if any)
                var singleObj = i.Object();
                if (singleObj.GroupCount > 0)
                {
                    // we have a grouped object
                    int indi = singleObj.GetGroupList()[0];
                    if (!groupInd.Contains(indi))
                        groupInd.Add(indi);
                }
            }

        // No curves, object invalid
        if (CrvObjects.Count == 0)
            _IsValid = false;

        CrvObjects = tmp;
    }

    public List<string> CutLayers()
    {
        var cutLayers = new List<string>();

        foreach (var i in CrvObjects)
        {
            string layerName = doc.Layers[i.Object().Attributes.LayerIndex].Name;
            if (layerName.Contains("C_") && !cutLayers.Contains(layerName.Substring(2)))
                cutLayers.Add(layerName.Substring(2));
        }

        // No cut layers, object is invalid
        if (cutLayers.Count == 0)
            _IsValid = false;

        return cutLayers;
    }

    public double CutLengthByLayer(string layerName)
    {
        if (!_IsValid)
            return 0;

        double Tlength = 0.0;

        foreach (var i in CrvObjects)
            if (doc.Layers[i.Object().Attributes.LayerIndex].Name == "C_" + layerName)
                Tlength += i.Curve().GetLength();

        return (int)Tlength;
    }
}