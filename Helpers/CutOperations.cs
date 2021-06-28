using Rhino;
using Rhino.Geometry;
using Rhino.DocObjects;
using System.Collections.Generic;


public struct CutOp
{
    public List<ObjRef> obj;

    public Layer parentLayer;
    public Layer cutLayer;
    public string cutLayerName;

    public double cutLength;

    public int countObjIndv;
    public int countObjGroups;

    public CutOp(RhinoDoc document)
    {
        obj = new List<ObjRef>();
        parentLayer = document.Layers[0];
        cutLayer = document.Layers[0];
        cutLayerName = "";
        cutLength = 0;
        countObjGroups = 0;
        countObjIndv = 0;
    }

    /// <summary>
    /// if the CutOp is blank, then it contains invalid data
    /// </summary>
    public bool IsValid
    {
        get
        {
            if (obj.Count > 0)
                return true;
            else
                return false;
        }
    }
}

public class CutOperations
{
    

    public RhinoDoc doc;

    public CutOperations(RhinoDoc document)
    {
        doc = document;
    }


    /// <summary>
    /// Makes objects out of cut layers only
    /// </summary>
    /// <param name="parentLayer"></param>
    /// <returns> list of cut layer objects or null </returns>
    public List<CutOp> FindCutLayers(Layer parentLayer)
    {
        var childLayers = parentLayer.GetChildren();
        var cutInfo = new List<CutOp>();

        for (int i = 0;i < childLayers.Length; i++)
        {
            if (childLayers[i].Name.Substring(0,2) == "C_")
            {
                cutInfo.Add(CutLayerInfo(childLayers[i]));
            }
        }
        if (cutInfo.Count > 0)
            return cutInfo;
        else
            return null;
    }
    public List<CutOp> FindCutLayers(string parentLayer)
    {
        var pl = doc.Layers.FindByFullPath(parentLayer, -1);
        if (pl == -1)
            return null;

        return FindCutLayers(doc.Layers[pl]);
    }



    /// <summary>
    /// returns information on one cut layer
    /// <para>Make SURE you are passing it a cut layer</para>
    /// </summary>
    /// <param name="childLayer"></param>
    /// <returns></returns>
    public CutOp CutLayerInfo(Layer childLayer)
    {
        //  We have a cut layer
        var cutLayer = new CutOp(doc);
        cutLayer.cutLayerName = childLayer.Name.Substring(2);
        cutLayer.parentLayer = doc.Layers.FindId(childLayer.ParentLayerId);
        cutLayer.cutLayer = childLayer;

        // create a custome selection set
        var ss = new ObjectEnumeratorSettings();
            ss.LayerIndexFilter = childLayer.Index;
            ss.ObjectTypeFilter = ObjectType.Curve;

        var obj = doc.Objects.GetObjectList(ss);
        var groups = new List<int>();
        foreach (var o in obj)
        {
            cutLayer.obj.Add(new ObjRef(o));
            cutLayer.cutLength += cutLayer.obj[-1].Curve().GetLength();

            if (o.GroupCount > 1)
            {
                if (!groups.Contains(o.GetGroupList()[0]))
                {
                    cutLayer.countObjGroups++;
                    groups.Add(o.GetGroupList()[0]);
                }
            }
            cutLayer.countObjIndv++;
        }
        return cutLayer;
    }
}