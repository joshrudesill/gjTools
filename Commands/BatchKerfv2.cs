﻿using System;
using Rhino;
using Rhino.Commands;
using gjTools.Helpers;
using Rhino.DocObjects;
using System.Collections.Generic;
using Rhino.Geometry;
namespace gjTools.Commands
{
    public class BatchKerfv2 : Command
    {
        public BatchKerfv2()
        {
            Instance = this;
        }

        public static BatchKerfv2 Instance { get; private set; }

        public override string EnglishName => "BatchKerfv2";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var lt = new LayerTools(doc);
            var ld = lt.getAllLayerData();
            var ldp = new List<string>();
            var ldsorted = new List<LayerData>();
            foreach (var lad in ld)
            {
                ldp.Add(lad.layerdata.Item1.Name);
            }
            var la = Rhino.UI.Dialogs.ShowMultiListBox("Layers", "Select a layer..", ldp);
            if (la == null)
            {
                RhinoApp.WriteLine("Cancelled.");
                return Result.Cancel;
            }
            foreach (var j in ld)
            {
                if (new List<string>(la).Contains(j.layerdata.Item1.Name))
                {
                    ldsorted.Add(j);
                }
            }
            foreach(var lds in ldsorted)
            {
                string sta = "";
                foreach (var sl in lds.layerdata.Item2)
                {
                    sta += sl.Item1.Name + ": ";
                    int kerf = 0;
                    foreach (var ob in sl.Item2)
                    {
                        if (ob.obRef.Curve() != null)
                        {
                            kerf += (int)ob.obRef.Curve().GetLength();
                        }
                    }
                    sta += kerf.ToString() + "\n";
                }
                var bb = lds.getBoundingBoxofParent();
                var crns = bb.GetCorners();
                Plane plane = doc.Views.ActiveView.ActiveViewport.ConstructionPlane();
                plane.Origin = crns[2];
                doc.Layers.SetCurrentLayerIndex(lds.layerdata.Item1.Index, true);
                doc.Objects.AddText(sta, plane, bb.GetEdges()[2].Length / 500, "Arial", false, false, TextJustification.BottomRight);
            }
            doc.Views.Redraw();
            return Result.Success;
        }
    }
}