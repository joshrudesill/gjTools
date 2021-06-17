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


    public void StandardDimstyle()
    {
        if (doc.DimStyles.FindName("LableMaker") == null)
        {
            // craete the dimstyle
            int dimStyleIntex = doc.DimStyles.Add("LabelMaker");
            var dimstyle = doc.DimStyles.FindIndex(dimStyleIntex);

            dimstyle.DimensionScale = 1;
            dimstyle.TextHeight = 0.14;
            dimstyle.Font = Rhino.DocObjects.Font.FromQuartetProperties("Consolas", false, false);

            RhinoApp.WriteLine("Created a Standard Dimstyle");
        } else
        {
            RhinoApp.WriteLine("Standard Dimstyle Exists");
        }
            
    }


    /// <summary>
    /// Create a Text entity and return for addition to document later
    /// </summary>
    /// <param name="text"></param>
    /// <param name="point"></param>
    /// <param name="dimsyleIndex"></param>
    /// <param name="height"></param>
    /// <param name="fontStyle">0=normal, 1=bold, 2=italic, 3=bold & italic</param>
    /// <param name="justHoriz">0=left, 1=center, 2=right, 3=auto</param>
    /// <param name="justVert">0=Top, 3=middle, 6=bottom</param>
    /// <returns></returns>
    public TextEntity AddText(string text, Point3d point, int dimsyleIndex, double height = 1, int fontStyle = 0, int justHoriz = 3, int justVert = 0)
    {
        Plane plane = doc.Views.ActiveView.ActiveViewport.ConstructionPlane();
              plane.Origin = point;
        var dimstyle = doc.DimStyles.FindIndex(dimsyleIndex);

        bool bold = true ? (fontStyle == 1 || fontStyle == 3) : false;
        bool italic = true ? (fontStyle == 2) : false;

        var H = Rhino.DocObjects.TextHorizontalAlignment.Auto;
        switch (justHoriz)
        {
            case 0: H = Rhino.DocObjects.TextHorizontalAlignment.Left; break;
            case 1: H = Rhino.DocObjects.TextHorizontalAlignment.Center; break;
            case 2: H = Rhino.DocObjects.TextHorizontalAlignment.Right; break;
            default: break;
        }

        var V = Rhino.DocObjects.TextVerticalAlignment.Top;
        switch (justVert)
        {
            case 0: V = Rhino.DocObjects.TextVerticalAlignment.Top; break;
            case 3: V = Rhino.DocObjects.TextVerticalAlignment.Middle; break;
            case 6: V = Rhino.DocObjects.TextVerticalAlignment.Bottom; break;
            default: break;
        }

        var txtEntity = TextEntity.Create(text, plane, dimstyle, false, 0, 0);
            txtEntity.SetBold(bold);
            txtEntity.SetItalic(italic);
            txtEntity.TextHorizontalAlignment = H;
            txtEntity.TextVerticalAlignment = V;

        return txtEntity;
    }
}



public class CutOperations
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
        
        return (int)Tlength;
    }
}