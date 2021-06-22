using System;
using System.Collections.Generic;
using Rhino;
using Rhino.DocObjects;


/// <summary>
/// Does all things Layer Related
/// </summary>
public class LayerTools
{
    /// <summary>
    /// The Active Document
    /// </summary>
    public RhinoDoc doc;
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="document"></param>
    public LayerTools(RhinoDoc document)
    {
        doc = document;
    }

    /// <summary>
    /// Makes a Top-Level Layer
    /// </summary>
    /// <param name="layerName"></param>
    public Layer CreateLayer(string layerName)
    {
        if (doc.Layers.FindByFullPath(layerName, -1) == -1)
        {
            var i = doc.Layers.Add();
            var newLayer = doc.Layers[i];
                newLayer.Name = layerName;
        }
        return doc.Layers[doc.Layers.FindByFullPath(layerName, 0)];
    }

    /// <summary>
    /// Makes a Top-Level Layer with Color
    /// </summary>
    /// <param name="layerName"></param>
    /// <param name="color"></param>
    /// <returns></returns>
    public Layer CreateLayer(string layerName, System.Drawing.Color color)
    {
        Layer l = CreateLayer(layerName);
              l.Color = color;
        return l;
    }

    /// <summary>
    /// Create a Second-Level Layer
    /// </summary>
    /// <param name="layerName"></param>
    /// <param name="parent"></param>
    /// <returns></returns>
    public Layer CreateLayer(string layerName, string parent)
    {
        if (doc.Layers.FindByFullPath(parent, -1) == -1)
            CreateLayer(parent);
        
        if (doc.Layers.FindByFullPath(parent + "::" + layerName, -1) == -1)
        {
            Guid pl = doc.Layers[doc.Layers.FindByFullPath(parent, 0)].Id;

            // Child Doesnt Exist
            Layer newLayer = CreateLayer(layerName);
                  newLayer.ParentLayerId = pl;
        }

        return doc.Layers[doc.Layers.FindByFullPath(parent + "::" + layerName, 0)];
    }


    /// <summary>
    /// Create a Second-Level Layer with Color
    /// 
    /// </summary>
    /// <param name="layerName"></param>
    /// <param name="parent"></param>
    /// <param name="color"></param>
    /// <returns></returns>
    public Layer CreateLayer(string layerName, string parent, System.Drawing.Color color)
    {
        Layer l = CreateLayer(layerName, parent);
              l.Color = color;
        return l;
    }

    /// <summary>
    /// Assign 1 Object to a Child of a parent
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="parent"></param>
    /// <param name="layerName"></param>
    public void AddObjectsToLayer(RhinoObject obj, string layerName, string parent)
    {
        var layer = CreateLayer(layerName, parent);
        AddObjectsToLayer(obj, layer);
    }

    /// <summary>
    /// Assign 1 Object to a Child of a parent
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="parent"></param>
    /// <param name="layerName"></param>
    public void AddObjectsToLayer(Guid obj, string layerName, string parent)
    {
        var layer = CreateLayer(layerName, parent);
        AddObjectsToLayer(obj, layer);
    }

    /// <summary>
    /// Assign many Objects to a Child of a parent
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="parent"></param>
    /// <param name="layerName"></param>
    public void AddObjectsToLayer(List<RhinoObject> obj, string layerName, string parent)
    {
        var layer = CreateLayer(layerName, parent);
        AddObjectsToLayer(obj, layer);
    }

    /// <summary>
    /// Assign many objects to layer
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="layer"></param>
    public void AddObjectsToLayer(List<RhinoObject> obj, Layer layer)
    {
        foreach (var o in obj)
            AddObjectsToLayer(o, layer);
    }

    /// <summary>
    /// Assign object to layer
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="layer"></param>
    public void AddObjectsToLayer(RhinoObject obj, Layer layer)
    {
        obj.Attributes.LayerIndex = layer.Index;
        obj.CommitChanges();
    }

    /// <summary>
    /// Assign object to layer
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="layer"></param>
    public void AddObjectsToLayer(Guid obj, Layer layer)
    {
        AddObjectsToLayer(doc.Objects.FindId(obj), layer);
    }

    /// <summary>
    /// Get object Layer by GUID
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public Layer ObjLayer(Guid obj)
    {
        return doc.Layers[doc.Objects.FindId(obj).Attributes.LayerIndex];
    }

    /// <summary>
    /// Get object Layer
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public Layer ObjLayer(RhinoObject obj)
    {
        return doc.Layers[obj.Attributes.LayerIndex];
    }

    /// <summary>
    /// gives a list of layers that are cut layers
    /// </summary>
    /// <param name="parentLayer"></param>
    /// <param name="includeNestBox"></param>
    /// <returns></returns>
    public List<Layer> getAllCutLayers(Layer parentLayer, bool includeNestBox = true)
    {
        var cutLayers = new List<Layer>();
        var childLAyers = parentLayer.GetChildren();

        foreach (var l in childLAyers)
            if (l.Name.Substring(0, 2) == "C_" || l.Name == "NestBox")
                cutLayers.Add(l);

        return cutLayers;
    }

    /// <summary>
    /// Just returns all parent layer objects
    /// </summary>
    /// <returns></returns>
    public List<Layer> getAllParentLayers()
    {
        var parents = new List<Layer>();
        foreach (Layer l in doc.Layers)
            if (l.ParentLayerId == Guid.Empty)
                parents.Add(l);

        return parents;
    }

    /// <summary>
    /// Just returns all parent layer names
    /// </summary>
    /// <returns></returns>
    public List<string> getAllParentLayersStrings()
    {
        var parents = new List<string>();
        foreach (Layer l in doc.Layers)
            if (l.ParentLayerId == Guid.Empty)
                parents.Add(l.Name);

        return parents;
    }
    public bool isObjectOnCutLayer(Rhino.DocObjects.RhinoObject o)
    {
        if(doc.Layers[o.Attributes.LayerIndex].Name.Substring(0,2) == "C_")
        {
            return true;
        }
        return false;
    }
}
