using Rhino;
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

    public bool ContainsGroup
    {
        get
        {
            if (countObjGroups > 0)
                return true;
            else
                return false;
        }
    }

    public int Count
    {
        get
        {
            if (countObjGroups > 0)
                return countObjGroups;
            else
                return countObjIndv;
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
        
        if (childLayers == null)
            return null;

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
    /// Input objects are used instead of the entire layer
    /// </summary>
    /// <param name="objs"></param>
    /// <returns></returns>
    public List<CutOp> FindCutLayers(List<ObjRef> objs)
    {
        var cutLayers = new List<CutOp>();

        // Find out how many cut layers there are
        var CutList = new List<Layer>();
        foreach (var o in objs)
        {
            int ind = o.Object().Attributes.LayerIndex;
            if (doc.Layers[ind].Name.Substring(0, 2) == "C_")
                if (!CutList.Contains(doc.Layers[ind]))
                    CutList.Add(doc.Layers[ind]);
        }

        // cycle through cut layers and make CutOps
        foreach (var cl in CutList)
        {
            var cut = new CutOp(doc);
            cut.parentLayer = doc.Layers.FindId(cl.ParentLayerId);
            cut.cutLayer = cl;
            cut.cutLayerName = cl.Name.Substring(2);

            var groups = new List<int>();
            foreach (var o in objs)
                if (doc.Layers[o.Object().Attributes.LayerIndex] == cl)
                {
                    if (o.Curve() != null)
                    {
                        cut.obj.Add(o);
                        cut.cutLength += o.Curve().GetLength();
                        cut.countObjIndv++;

                        if (o.Object().GroupCount > 0)
                        {
                            if (!groups.Contains(o.Object().GetGroupList()[0]))
                            {
                                cut.countObjGroups++;
                                groups.Add(o.Object().Attributes.LayerIndex);
                            }
                        }
                    }
                }
            cutLayers.Add(cut);
        }

        return cutLayers;
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
        var cutLayer = new CutOp(doc)
        {
            cutLayerName = childLayer.Name.Substring(2),
            parentLayer = doc.Layers.FindId(childLayer.ParentLayerId),
            cutLayer = childLayer
        };

        // create a custome selection set
        var ss = new ObjectEnumeratorSettings
        {
            LayerIndexFilter = childLayer.Index,
            ObjectTypeFilter = ObjectType.Curve
        };

        var obj = doc.Objects.GetObjectList(ss);
        var groups = new List<int>();
        foreach (var o in obj)
        {
            cutLayer.obj.Add(new ObjRef(o));
            cutLayer.cutLength += cutLayer.obj[cutLayer.obj.Count - 1].Curve().GetLength();

            if (o.GroupCount > 0)
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


    /// <summary>
    /// Totals groups up from list of CutOps
    /// </summary>
    /// <param name="cuts"></param>
    /// <returns></returns>
    public int CountGroups(List<CutOp> cuts)
    {
        int cnt = 0;
        foreach (var c in cuts)
            cnt += c.countObjGroups;
        return cnt;
    }
    /// <summary>
    /// Totals objects up from list of CutOps
    /// </summary>
    /// <param name="cuts"></param>
    /// <returns></returns>
    public int CountObjects(List<CutOp> cuts)
    {
        int cnt = 0;
        foreach (var c in cuts)
            cnt += c.countObjIndv;
        return cnt;
    }
    /// <summary>
    /// Checks if any have a group count
    /// </summary>
    /// <param name="cuts"></param>
    /// <returns></returns>
    public bool GroupsPresent(List<CutOp> cuts)
    {
        foreach(var c in cuts)
            if (c.ContainsGroup)
                return true;
        return false;
    }
}