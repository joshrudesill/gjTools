﻿using System;
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
    /// Makes a Top-Level Layer with Color
    /// </summary>
    /// <param name="layerName"></param>
    /// <param name="color"></param>
    /// <returns></returns>
    public Layer CreateLayer(string layerName, System.Drawing.Color color)
    {
        if (doc.Layers.FindByFullPath(layerName, -1) == -1)
        {
            var lay = new Layer();
                lay.Name = layerName;
                lay.Color = color;
            int layInd = doc.Layers.Add(lay);
            return doc.Layers[layInd];
        }
        return doc.Layers[doc.Layers.FindByFullPath(layerName, 0)];
    }

    /// <summary>
    /// Makes a Top-Level Layer
    /// </summary>
    /// <param name="layerName"></param>
    public Layer CreateLayer(string layerName)
    {
        return CreateLayer(layerName, System.Drawing.Color.Black);
    }

    /// <summary>
    /// Create a Second-Level Layer
    /// </summary>
    /// <param name="layerName"></param>
    /// <param name="parent"></param>
    /// <returns></returns>
    public Layer CreateLayer(string layerName, string parent, System.Drawing.Color color)
    {
        Layer paren = CreateLayer(parent);

        if (doc.Layers.FindByFullPath(parent + "::" + layerName, -1) == -1)
        {
            Layer lay = new Layer
            {
                ParentLayerId = paren.Id,
                Name = layerName,
                Color = color
            };
            int layInd = doc.Layers.Add(lay);
            return doc.Layers[layInd];
        }

        return doc.Layers[doc.Layers.FindByFullPath(parent + "::" + layerName, 0)];
    }


    /// <summary>
    /// Create a Second-Level Layer with Color
    /// </summary>
    /// <param name="layerName"></param>
    /// <param name="parent"></param>
    /// <param name="color"></param>
    /// <returns></returns>
    public Layer CreateLayer(string layerName, string parent)
    {
        return CreateLayer(layerName, parent, System.Drawing.Color.Black);
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
        if (childLAyers != null) 
        {
            foreach (var l in childLAyers)
            {
                if (l.Name.Substring(0, 2) == "C_")
                    cutLayers.Add(l);
                else if (l.Name == "NestBox" && includeNestBox)
                    cutLayers.Add(l);
            }
            return cutLayers;
        }
        return null;
    }

    public List<Layer> getAllSubLayers(Layer parentLayer)
    {
        var childLAyers = parentLayer.GetChildren();
        if (childLAyers != null)
        {
            return new List<Layer>(childLAyers);
        }
        return null;
    }


    /// <summary>
    /// Just returns all parent layer objects
    /// </summary>
    /// <returns></returns>
    public List<Layer> getAllParentLayers()
    {
        var parents = new List<Layer>();
        foreach (Layer l in doc.Layers)
            if (l.ParentLayerId == Guid.Empty && !l.IsDeleted)
                parents.Add(l);

        return parents;
    }

    public List<gjTools.Helpers.LayerData> getAllLayerData()
    {
        var parents = getAllParentLayers();
        var ltr = new List<gjTools.Helpers.LayerData>();
        foreach (var layer in parents)
        {
            ltr.Add(new gjTools.Helpers.LayerData(layer, doc));
        }
        return ltr;
    }

    /// <summary>
    /// Just returns all parent layer names
    /// </summary>
    /// <returns></returns>
    public List<string> getAllParentLayersStrings()
    {
        var parents = new List<string>();
        foreach (Layer l in doc.Layers)
            if (l.ParentLayerId == Guid.Empty && !l.IsDeleted)
                parents.Add(l.Name);

        return parents;
    }

    /// <summary>
    /// Checks if object is on a cut layer
    /// </summary>
    /// <param name="o"></param>
    /// <returns></returns>
    public bool isObjectOnCutLayer(RhinoObject o)
    {
        if(doc.Layers[o.Attributes.LayerIndex].Name.Substring(0,2) == "C_")
            return true;
        return false;
    }


    /// <summary>
    /// Checks if object is on a cut layer
    /// </summary>
    /// <param name="o"></param>
    /// <returns></returns>
    public List<Layer> isObjectOnCutLayer(RhinoObject o, bool returnParentChild)
    {
        var res = new List<Layer>();
        if (isObjectOnCutLayer(o))
        {
            // object on cut layer, safe to assume it has a parent
            var child = ObjLayer(o);
            if (child.ParentLayerId != Guid.Empty)
                res.Add(doc.Layers.FindId(child.ParentLayerId));
            res.Add(child);
        }
        return res;
    }
}
