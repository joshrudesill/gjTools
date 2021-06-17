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
/// 
interface IDrawTools
{
    void hideDynamicDraw();
    bool CheckPolylines(GetObject obj, bool showPreview = true);
    List<string> SelParentLayers(bool multiSel = true);
}

interface ICutOperations
{
    List<string> CutLayers();
    double CutLengthByLayer(string layerName);
}

class DrawTools : IDrawTools 
{

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

public class CutOperations : ICutOperations
{
    public List<Rhino.DocObjects.ObjRef> CrvObjects;
    public RhinoDoc doc;
    public Rhino.DocObjects.Layer parentLayer;
    public List<int> groupInd;

    public CutOperations(List<Rhino.DocObjects.ObjRef> Crvs, RhinoDoc document)
    {
        CrvObjects = Crvs;
        doc = document;

        OnlyCurves();
        var singleSubLayer = doc.Layers[CrvObjects[0].Object().Attributes.LayerIndex];
        parentLayer = doc.Layers.FindId(singleSubLayer.ParentLayerId);
    }

    private void OnlyCurves()
    {
        var tmp = new List<Rhino.DocObjects.ObjRef>();

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

        return cutLayers;
    }

    public double CutLengthByLayer(string layerName)
    {
        double Tlength = 0.0;

        foreach (var i in CrvObjects)
            if (doc.Layers[i.Object().Attributes.LayerIndex].Name == "C_" + layerName)
                Tlength += i.Curve().GetLength();
        
        return Math.Round(Tlength, 2);
    }
}