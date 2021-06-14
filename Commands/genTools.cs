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
    public bool CheckPolylines(Curve obj, RhinoDoc doc,bool supressMessage=true)
    {
        bool isPoly = true;
        Curve[] pieces = obj.DuplicateSegments();

        foreach (Curve seg in pieces)
        {
            // see if it can be a polycurve
            if (!seg.IsArc() && !seg.IsCircle() && !seg.IsLinear())
            {
                if (!supressMessage)
                {
                    var show = new Rhino.Display.CustomDisplay(true);
                    show.AddCurve(seg, System.Drawing.Color.DarkMagenta, 5);
                    doc.Views.Redraw();

                    Rhino.UI.Dialogs.ShowMessage("Not a Polyline Object\nBad Lines are Highlighted", "Bad Lines");

                    show.Dispose();
                }
                
                isPoly = false;
                break;
            }
        }

        return isPoly;
    }



    /// <summary>
    /// asks user to select layer or layers depending on multiSel val
    /// returns selected layers
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="multiSel"></param>
    /// <returns></returns>
    public List<string> SelParentLayers(RhinoDoc doc, bool multiSel=true)
    {
        var lays = new List<string>(doc.Layers.Count);
        var plays = new List<string>();
        string clay = doc.Layers.CurrentLayer.Name;

        foreach (var l in doc.Layers)
        {
            if (l.ParentLayerId == Guid.Empty)
                lays.Add(l.Name);
        }
        
        if (multiSel)
        {
            // Select multiple layers
            var pts = Rhino.UI.Dialogs.ShowMultiListBox("Layers", "Choose Layer/s", lays);
            if (pts == null)
            {
                plays.Add("");
            } else
            {
                foreach (string op in pts)
                    plays.Add(op);
            } 
        } 
        else
        {
            // Select Single Layer
            var pts = (string)Rhino.UI.Dialogs.ShowListBox("Layers", "Choose a Layer", lays, clay);
            if (pts == null)
            {
                plays.Add("");
            } else
            {
                plays.Add(pts);
            }

        }

        return plays;
    }
}