using Rhino;
using Rhino.Geometry;
using Rhino.DocObjects;
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


/// <summary>
/// Used as a handy version of the boxes out there
/// <para>More Directed for the tools we need</para>
/// </summary>
public struct SimpleBox
{
    private List<Point3d> _pt;

    public SimpleBox(BoundingBox BBox)
    {
        var pts = BBox.GetCorners();
        _pt = new List<Point3d> { pts[0], pts[1], pts[2], pts[3] };
    }
    public SimpleBox(Point3d Min, Point3d Max)
    {
        _pt = new List<Point3d> { Min, new Point3d(Max.X, Min.Y, 0), Max, new Point3d(Min.X, Max.Y, 0) };
    }
    public SimpleBox(Point3d BottomLeftPoint, double width, double height)
    {
        var pt = BottomLeftPoint;
        _pt = new List<Point3d> { 
            BottomLeftPoint, 
            new Point3d(pt.X + width, pt.Y, 0),
            new Point3d(pt.X + width, pt.Y + height, 0),
            new Point3d(pt.X, pt.Y + height, 0)
        };
    }
    public SimpleBox(SimpleBox oldBox)
    {
        _pt = new List<Point3d> { oldBox.pt(0), oldBox.pt(1), oldBox.pt(2), oldBox.pt(3) };
    }

    public double Width { get { return _pt[0].DistanceTo(_pt[1]); } }
    public double Height { get { return _pt[0].DistanceTo(_pt[3]); } }
    public Point3d Center
    {
        get
        {
            return new Point3d(_pt[0].X + (_pt[0].DistanceTo(_pt[1]) / 2), _pt[1].Y + (_pt[1].DistanceTo(_pt[2]) / 2), 0);
        }
        set
        {
            var center = new Point3d(_pt[0].X + (_pt[0].DistanceTo(_pt[1]) / 2), _pt[1].Y + (_pt[1].DistanceTo(_pt[2]) / 2), 0);
            double xDist = center.X - value.X;
            double yDist = center.Y - value.Y;
            for (var i = 0; i < _pt.Count; i++)
            {
                var pt = _pt[i];
                pt.X += xDist;
                pt.Y += yDist;
                _pt[i] = pt;
            }
        }
    }
    public Point3d pt(int index) 
    {
        return _pt[index];
    }

    /// <summary>
    /// Get the Bounding box equivilent of this
    /// </summary>
    public BoundingBox GetBB { get { return new BoundingBox(_pt); } }
    /// <summary>
    /// Get the rectangle equivilent of this
    /// </summary>
    public Rectangle3d GetRect {
        get { return new Rectangle3d(new Plane (_pt[0], Vector3d.ZAxis), Width, Height); }
    }
    /// <summary>
    /// Same as the Rhino Box methods
    /// </summary>
    /// <param name="BBox"></param>
    public void Union(BoundingBox BBox)
    {
        var myBB = GetBB;
        myBB.Union(BBox);
        var edge = BBox.GetEdges();
        var pts = BBox.GetCorners();

        _pt = new List<Point3d> { pts[0], pts[1], pts[2], pts[3] };
    }
    /// <summary>
    /// Inflates or deflates based on +- value
    /// <para>does the uniform first, then the side adjustments</para>
    /// </summary>
    /// <param name="uniform"></param>
    /// <param name="lh"></param>
    /// <param name="rh"></param>
    /// <param name="top"></param>
    /// <param name="bott"></param>
    public void Inflate(double uniform = 0, double lh = 0, double rh = 0, double top = 0, double bott = 0)
    {
        var ptMin = _pt[0];
        var ptMax = _pt[2];

        ptMin.X -= uniform + lh;
        ptMin.Y -= uniform + bott;
        ptMax.X += uniform + top;
        ptMax.Y += uniform + rh;

        var pt = new BoundingBox(new List<Point3d> { ptMin, ptMax }).GetCorners();
        _pt = new List<Point3d> { pt[0], pt[1], pt[2], pt[3] };

    }

    /// <summary>
    /// returns a modified copy of one of the 4 bounding corners
    /// <para>Does NOT modify this object</para>
    /// </summary>
    /// <param name="index"></param>
    /// <param name="xTransform"></param>
    /// <param name="yTransform"></param>
    /// <returns></returns>
    public Point3d GetModPt(int index = 0, double xTransform = 0, double yTransform = 0)
    {
        var pt = _pt[index];
        pt.X += xTransform;
        pt.Y += yTransform;
        return pt;
    }
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
    public bool CheckPolylines(List<Rhino.DocObjects.RhinoObject> obj, bool showPreview = true)
    {
        var Curves = new List<Curve>();
        for (var i = 0; i <= obj.Count - 1; i++)
            if (obj[i].Geometry.ObjectType == Rhino.DocObjects.ObjectType.Curve)
                Curves.Add(obj[i].Geometry as Curve);

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
    /// creates the default label dimstyle used all over
    /// </summary>
    /// <returns>The STD Dimstyle index</returns>
    public int StandardDimstyle()
    {
        DimensionStyle ds = doc.DimStyles.FindName("LabelMaker");

        if (ds == null)
        {
            // create the dimstyle
            ds = doc.DimStyles[doc.DimStyles.Add("LabelMaker")];

            ds.Font = Font.FromQuartetProperties("Consolas", false, false);
            ds.DimensionScale = 1;
            ds.TextHeight = 0.14;
            ds.DrawForward = false;

            return ds.Index;
        }

        if (ds.DrawForward)
        {
            ds.DrawForward = false;
            doc.DimStyles.Modify(ds, ds.Index, true);
        }
        return ds.Index;
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

        bool bold = (fontStyle == 1 || fontStyle == 3) ? true : false;
        bool italic = (fontStyle == 2) ? true : false;

        var H = TextHorizontalAlignment.Auto;
        switch (justHoriz)
        {
            case 0: H = TextHorizontalAlignment.Left; break;
            case 1: H = TextHorizontalAlignment.Center; break;
            case 2: H = TextHorizontalAlignment.Right; break;
            default: break;
        }

        var V = TextVerticalAlignment.Top;
        switch (justVert)
        {
            case 0: V = TextVerticalAlignment.Top; break;
            case 3: V = TextVerticalAlignment.Middle; break;
            case 6: V = TextVerticalAlignment.Bottom; break;
            default: break;
        }

        var txtEntity = TextEntity.Create(text, plane, dimstyle, false, 0, 0);
            txtEntity.TextHorizontalAlignment = H;
            txtEntity.TextVerticalAlignment = V;
            txtEntity.TextHeight = height;
            txtEntity.Font = Font.FromQuartetProperties("Consolas", bold, italic);

        return txtEntity;
    }

    public TextEntity AddText(string text, Point3d pt, double height = 1, bool bold = false, bool italic = false, TextHorizontalAlignment horiz = TextHorizontalAlignment.Left, TextVerticalAlignment vert = TextVerticalAlignment.Top)
    {
        TextEntity txtEnt = TextEntity.Create(text, new Plane(pt, Vector3d.ZAxis), doc.DimStyles.FindIndex(StandardDimstyle()), false, 0, 0);

        txtEnt.TextHorizontalAlignment = horiz;
        txtEnt.TextVerticalAlignment = vert;
        txtEnt.TextHeight = height;
        txtEnt.Font = Font.FromQuartetProperties("Consolas", bold, italic);

        return txtEnt;
    }
}