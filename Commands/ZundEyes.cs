﻿using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using System;
using Rhino.DocObjects;
using System.Collections.Generic;
namespace gjTools.Commands
{
    public class ZundEyes : Command
    {
        public ZundEyes()
        {
            Instance = this;
        }
        int spacingDivNumber = 24;
        double spacingFromSide = (0.65 * 2);

        int l2index;
        int l1index;

        

        double topD;
        double numEyesT;
        double spacingT;

        double sideD;
        double numEyesS;
        double spacingS;

        LayerTools lt = new LayerTools(RhinoDoc.ActiveDoc);

        ObjRef nestboxref;

        List<Curve> cl = new List<Curve>();
        public static ZundEyes Instance { get; private set; }

        public override string EnglishName => "ZundEyes";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            int currentLayer = doc.Layers.CurrentLayerIndex;
            RhinoApp.WriteLine("TEST");
            const ObjectType filter = ObjectType.Curve;
            List<RhinoObject> robj = new List<RhinoObject>();
            Result rc = Rhino.Input.RhinoGet.GetMultipleObjects("Select box(es) to add eyes to..", false, filter, out ObjRef[] objref);
            if (rc != Result.Success) { return Result.Cancel; }
            BoundingBox bb = objref[0].Geometry().GetBoundingBox(true);
            bool nestbox = false;
            nestboxref = objref[0];
            foreach (var or in objref)
            {
                bb.Union(or.Geometry().GetBoundingBox(true));
                if (!nestbox && doc.Layers[or.Object().Attributes.LayerIndex].Name == "NestBox")
                {
                    nestbox = true;
                    nestboxref = or;
                }
            }
            //----Layering----//

            createLayers(doc);

            // Get corners
            Point3d[] corners = bb.GetCorners();

            //------ Math -------//
            calcPlacement(corners);

            //----------------Draw Eyes----------------//
            drawEyes(corners, doc);
            doc.Layers.SetCurrentLayerIndex(currentLayer, true);

            doc.Views.Redraw();
            return Result.Success;
        }



        /// <summary>
        /// Internal use only. Private function
        /// </summary>
        /// <param name="c"></param>
        /// <param name="layer1"></param>
        /// <param name="layer2"></param>
        /// <param name="doc"></param>
        
        void createLayers(RhinoDoc doc)
        {
            int layer = nestboxref.Object().Attributes.LayerIndex;
            Layer pli = doc.Layers[layer];
            if (pli.ParentLayerId != Guid.Empty)
            {
                var player = doc.Layers.FindId(pli.ParentLayerId);
                pli = player;
            }

            l1index = lt.CreateLayer("C_EYES", pli.Name, System.Drawing.Color.Red).Index;
            l2index = lt.CreateLayer("EYE_FILL", pli.Name, System.Drawing.Color.Black).Index;
            

        }
        void calcPlacement(Point3d[] corners)
        {
            topD = Math.Abs(corners[3].X - corners[2].X);
            numEyesT = Math.Floor((topD - spacingFromSide) / spacingDivNumber);
            if (numEyesT < 1) { numEyesT = 1; }
            spacingT = (topD - spacingFromSide) / (numEyesT);
            //----------------------
            sideD = Math.Abs(corners[3].Y - corners[0].Y);
            numEyesS = Math.Floor((sideD - spacingFromSide) / spacingDivNumber);
            if (numEyesS < 1) { numEyesS = 1; }
            if (numEyesS > 2) { numEyesS = 2; }
            spacingS = (sideD - spacingFromSide) / (numEyesS);
        }
        void drawEyes(Point3d[] corners, RhinoDoc doc)
        {
            Point3d first = new Point3d((corners[3].X + (spacingFromSide / 2)), (corners[3].Y - (spacingFromSide / 2)), 0);
            doc.Layers.SetCurrentLayerIndex(l1index, true);
            for (int i = 0; i < numEyesT + 1; i++)
            {
                Circle c1 = new Circle(first, 0.125);
                cl.Add(c1.ToNurbsCurve());
                for (int j = 0; j < numEyesS; j++)
                {
                    first.Y -= spacingS;
                    Circle c2 = new Circle(first, 0.125);
                    cl.Add(c2.ToNurbsCurve());
                }
                first.Y = corners[3].Y - 0.65;
                first.X += spacingT;
            }
            Circle fc = new Circle(new Point3d(corners[1].X - 1 - (spacingFromSide / 2), corners[1].Y + (spacingFromSide / 2), 0), 0.125);
            cl.Add(fc.ToNurbsCurve());
            Hatch[] hl = Hatch.Create(cl, doc.HatchPatterns.CurrentHatchPatternIndex, 0, 1.0, 1.0);
            
            foreach (var c in cl)
            {
                Guid newob = doc.Objects.Add(c);
                RhinoObject ro = doc.Objects.FindId(newob);
                ro.Attributes.LayerIndex = l1index;
                ro.CommitChanges();
            }
            foreach (var h in hl)
            {
                Guid newob = doc.Objects.AddHatch(h);
                RhinoObject ro = doc.Objects.FindId(newob);
                ro.Attributes.LayerIndex = l1index;
                ro.CommitChanges();
            }
        }
    }
}