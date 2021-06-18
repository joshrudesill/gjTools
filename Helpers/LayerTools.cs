using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino;
using Rhino.DocObjects;


/// <summary>
/// Does all things Layer Related
/// </summary>
public class LayerTools
{
    public RhinoDoc doc;
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
        {
            // Parent Doesnt Exist
            CreateLayer(parent);
        }
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

    public void AddObjectsToLayer(RhinoObject obj, Layer layer)
    {

    }
}
