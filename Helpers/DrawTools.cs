using Rhino;
using Rhino.Geometry;
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

public class DrawTools : IDrawTools 
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
    /// Highlights the lines green=Good, Red=Bad
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="showPreview"></param>
    /// <returns>return true or false if the line can be used as cut line</returns>
    public bool CheckPolylines(GetObject obj, bool showPreview = true)
    {
        var Curves = new List<Curve>();
        for (var i = 0; i <= obj.ObjectCount - 1; i++)
            if (obj.Object(i).Curve() != null)
                Curves.Add(obj.Object(i).Curve());

        return CheckPolylines(Curves, showPreview);
    }

    /// <summary>
    /// Highlights the lines green=Good, Red=Bad
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="showPreview"></param>
    /// <returns>return true or false if the line can be used as cut line</returns>
    public bool CheckPolylines(List<Rhino.DocObjects.ObjRef> obj, bool showPreview = true)
    {
        var Curves = new List<Curve>();
        for (var i = 0; i <= obj.Count - 1; i++)
            if (obj[i].Curve() != null)
                Curves.Add(obj[i].Curve());

        return CheckPolylines(Curves, showPreview);
    }

    /// <summary>
    /// Highlights the lines green=Good, Red=Bad
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="showPreview"></param>
    /// <returns>return true or false if the line can be used as cut line</returns>
    public bool CheckPolylines(List<Curve> obj, bool showPreview = true)
    {
        bool isPoly = true;
        show.Enabled = showPreview;

        for (var i = 0; i <= obj.Count - 1; i++)
        {
            Curve[] pieces = obj[i].DuplicateSegments();
            foreach (Curve seg in pieces)
            {
                // see if it can be a polycurve
                if (!seg.IsArc() && !seg.IsCircle() && !seg.IsLinear())
                {
                    isPoly = false;

                    if (show.Enabled)
                        show.AddCurve(seg, System.Drawing.Color.DarkRed, 5);
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


    /// <summary>
    /// creates the default label dimstyle used all over
    /// </summary>
    /// <returns></returns>
    public int StandardDimstyle()
    {
        if (doc.DimStyles.FindName("LabelMaker") == null)
        {
            // craete the dimstyle
            int dimStyleIntex = doc.DimStyles.Add("LabelMaker");
            var dimstyle = doc.DimStyles.FindIndex(dimStyleIntex);

            dimstyle.DimensionScale = 1;
            dimstyle.TextHeight = 0.14;
            dimstyle.Font = Rhino.DocObjects.Font.FromQuartetProperties("Consolas", false, false);

            return dimstyle.Index;
        } else
        {
            return doc.DimStyles.FindName("LabelMaker").Index;
        }
    }


    /// <summary>
    /// Create a Text entity and return for addition to document later
    /// <para> fontStyle: 0=normal, 1=bold, 2=italic, 3=bold and italic </para>
    /// <para> justHoriz: 0=left, 1=center, 2=right, 3=auto </para>
    /// <para> justVert: 0=Top, 3=middle, 6=bottom </para>
    /// </summary>
    /// <param name="text"></param>
    /// <param name="point"></param>
    /// <param name="dimsyleIndex"></param>
    /// <param name="height"></param>
    /// <param name="fontStyle"></param>
    /// <param name="justHoriz"></param>
    /// <param name="justVert"></param>
    /// <returns>Rhino Text Object</returns>
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
            txtEntity.TextHorizontalAlignment = H;
            txtEntity.TextVerticalAlignment = V;
            txtEntity.TextHeight = height;
            txtEntity.Font = Rhino.DocObjects.Font.FromQuartetProperties("Consolas", bold, italic);

        return txtEntity;
    }
}