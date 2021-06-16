using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;

/// <summary>
/// This class is for object creation
/// </summary>
class DrawTools {

    public RhinoDoc doc;
    public Rhino.Display.CustomDisplay show = new Rhino.Display.CustomDisplay(false);

    public DrawTools(RhinoDoc doc)
    {
        this.doc = doc;
    }

    public void hideDynamicDraw () {
        show.Dispose();
        doc.Views.Redraw();
    }


    /// <summary>
    /// return true or false if the line can be used as cut line
    /// If supressMessage=false, show the bad line and display a message
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="doc"></param>
    /// <param name="supressMessage"></param>
    /// <returns></returns>
    public bool CheckPolylines(GetObject obj, bool showPreview=true)
    {
        bool isPoly = true;
        show.Enabled = showPreview;

        for (var i=0; i <= obj.ObjectCount - 1; i++)
        {

            Curve[] pieces = obj.Object(i).Curve().DuplicateSegments();
            foreach (Curve seg in pieces)
            {
                // see if it can be a polycurve
                if (!seg.IsArc() && !seg.IsCircle() && !seg.IsLinear())
                {
                    isPoly = false;

                    if (show.Enabled)
                        show.AddCurve(seg, System.Drawing.Color.DarkMagenta, 5);
                }
                else
                {
                    if (show.Enabled)
                        show.AddCurve(seg, System.Drawing.Color.ForestGreen, 5);
                }
            }
        }

        doc.Views.Redraw();
        return isPoly;
    }



    /// <summary>
    /// asks user to select layer or layers depending on multiSel val
    /// returns selected layers
    /// </summary>
    /// <param name="multiSel"></param>
    /// <returns></returns>
    public List<string> SelParentLayers(bool multiSel=true)
    {
        var lays = new List<string>(doc.Layers.Count);
        string clay = doc.Layers.CurrentLayer.Name;
        var pts = new List<string>();

        foreach (var l in doc.Layers)
            if (l.ParentLayerId == Guid.Empty)
                lays.Add(l.Name);

        if (multiSel)
            pts.AddRange(Rhino.UI.Dialogs.ShowMultiListBox("Layers", "Choose Layer/s", lays));
        else
            pts.Add((string)Rhino.UI.Dialogs.ShowListBox("Layers", "Choose a Layer", lays, clay));

        return pts;
    }
}

class CutOperations
{
    public List<Rhino.DocObjects.ObjRef> CrvObjects;
    public RhinoDoc doc;
    public string objectLayer;

    public CutOperations(List<Rhino.DocObjects.ObjRef> Crvs, RhinoDoc document)
    {
        CrvObjects = Crvs;
        doc = document;

        OnlyCurves();
    }

    private void OnlyCurves()
    {
        var tmp = new List<Rhino.DocObjects.ObjRef>();

        foreach (var i in CrvObjects)
            if (i.Curve() != null)
                tmp.Add(i);

        CrvObjects = tmp;
    }

    public List<string> CutLayers()
    {
        var cutLayers = new List<string>();
        
        foreach (var i in CrvObjects)
        {
            string layerName = doc.Layers[i.Object().Attributes.LayerIndex].Name;
            if (layerName.Contains("C_"))
                cutLayers.Add(layerName);
        }

        return cutLayers;
    }

    public double CutLengthByLayer(string layerName)
    {
        double Tlength = 0.0;

        foreach (var i in CrvObjects)
            if (doc.Layers[i.Object().Attributes.LayerIndex].Name == layerName)
                Tlength += i.Curve().GetLength();
        
        return Tlength;
    }
}