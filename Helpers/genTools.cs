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