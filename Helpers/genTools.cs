using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class genTools {

    public RhinoDoc doc;
    public Rhino.Display.CustomDisplay show = new Rhino.Display.CustomDisplay(false);

    public genTools(RhinoDoc doc)
    {
        this.doc = doc;
    }

    ~ genTools () {
        show.Dispose();
    }


    /// <summary>
    /// Returns the Center Point of a single object
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public Point3d CurveCenter(Curve obj)
    {
        var bb = obj.GetBoundingBox(true);
        Point3d[] corners = bb.GetCorners();
        double[] center = {
            corners[0].X + (corners[0].DistanceTo(corners[1]) / 2), 
            corners[0].Y + (corners[0].DistanceTo(corners[3]) / 2)
        };
        return new Point3d(center[0], center[1], 0.0);
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
                    if (show.Enabled)
                        show.AddCurve(seg, System.Drawing.Color.DarkMagenta, 5);
                    isPoly = false;
                }
                else
                {
                    if (show.Enabled)
                        show.AddCurve(seg, System.Drawing.Color.ForestGreen, 5);
                }
            }
        }
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