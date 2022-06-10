using System.Collections.Generic;
using System;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.DocObjects;
using Rhino.Input;
using Rhino.Input.Custom;

namespace gjTools.Commands.Labeling
{
    public class EPMarkerMake : Command
    {
        public EPMarkerMake()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static EPMarkerMake Instance { get; private set; }

        public override string EnglishName => "EPMarkerMake";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // get a map object
            if (RhinoGet.GetOneObject("Select Map Object", false, ObjectType.Curve, out ObjRef mapObj) != Result.Success)
                return Result.Cancel;

            // get map points from the map object
            Curve crv = mapObj.Curve();

            // make a list of lines that provide the mapping
            var mapLines = new List<Line>();

            // get all other objects that are the same from the layer
            var mapTargets = new List<RhinoObject>( doc.Objects.FindByLayer(doc.Layers[mapObj.Object().Attributes.LayerIndex]) );
            if (mapTargets.Count > 0)
            {
                for (int i = 0; i < mapTargets.Count; i++)
                {
                    if (mapTargets[i].ObjectType != ObjectType.Curve)
                        continue;

                    Curve crvT = mapTargets[i].Geometry as Curve;
                    mapLines.Add(new Line(crvT.PointAtStart, crvT.PointAt(crvT.GetLength() * 0.25)));
                }
            }

            // display a dummy label and get a point
            var gp = new MarkerGp();
            if (gp.CommandResult() != Result.Success)
                return Result.Cancel;

            var pt = gp.Point();

            // get a markers layer
            int markerLayer = doc.Layers.FindByFullPath("MARKERS", -1);
            if (markerLayer == -1)
                markerLayer = doc.Layers.Add("MARKERS", System.Drawing.Color.Aquamarine);
            var attr = new ObjectAttributes { LayerIndex = markerLayer };

            // create the dot object
            TextDot dot = new TextDot(gp.Name, pt) 
            { 
                FontHeight = 8, 
                SecondaryText = gp.Rotation.ToString() 
            };

            // get rotation
            Line mapLine = new Line(crv.PointAtStart, crv.PointAt(crv.GetLength() * 0.25));
            Line mapLinePt = new Line(crv.PointAtStart, pt);
            double rot = Vector3d.VectorAngle(mapLine.UnitTangent, mapLinePt.UnitTangent);
            double len = mapLinePt.Length;

            // is the rotation inverted?
            Line testInvert = new Line(mapLine.From, mapLine.To);
            testInvert.Transform(Transform.Rotation(rot, mapLine.From));
            testInvert.Length = mapLinePt.Length;
            rot *= (testInvert.EpsilonEquals(mapLinePt, 0.01)) ? 1 : -1;

            // roll through the other maplines
            for (int i = 0; i < mapLines.Count; i++)
            {
                Line l = mapLines[i];
                l.Length = len;
                l.Transform(Transform.Rotation(rot, mapLines[i].From));

                

                dot.Point = l.To;
                doc.Objects.AddTextDot(dot, attr);
            }

            doc.Views.Redraw();
            return Result.Success;
        }
    }


    public class MarkerGp : GetPoint
    {
        private readonly System.Drawing.Color m_tCol = System.Drawing.Color.Black;
        private double m_radians = 0;

        // Rotation chosen for the dot
        public int Rotation { get; private set; }

        // Label name
        public string Name { get; private set; }

        public MarkerGp()
        {
            SetCommandPrompt("Place Label <Rotation = 0> <Label = PT>");
            AcceptNumber(true, true);
            AcceptString(true);
            Rotation = 0;
            Name = "PT";

            while (true)
            {
                var res = Get();

                if (res == GetResult.Cancel || res == GetResult.Point)
                    return;

                // handle number for rotation
                if (res == GetResult.Number)
                {
                    int rot = (int)Number();

                    if (rot == 360)
                        rot = 0;

                    if (rot != 0 && rot != 90 && rot != 180 && rot != 270)
                        continue;

                    Rotation = rot;
                    m_radians = RhinoMath.ToRadians(rot);
                    SetCommandPrompt($"Place Label <Rotation = {Rotation}> <Label = {Name}>");
                    continue;
                }

                // label name
                if (res == GetResult.String)
                {
                    Name = StringResult();
                    SetCommandPrompt($"Place Label <Rotation = {Rotation}> <Label = {Name}>");
                }
            }    
        }

        protected override void OnDynamicDraw(GetPointDrawEventArgs e)
        {
            // make planes
            Plane l1_plane = new Plane(e.CurrentPoint, Vector3d.ZAxis);
            Plane l2_plane = new Plane(new Point3d(e.CurrentPoint.X, e.CurrentPoint.Y - 0.25, 0), Vector3d.ZAxis);

            // does the thing need rotation
            if (Rotation != 0)
            {

                l2_plane.Rotate(m_radians, Vector3d.ZAxis, l1_plane.Origin);
                l1_plane.Rotate(m_radians, Vector3d.ZAxis, l1_plane.Origin);
            }

            // draw dummy text label
            e.Display.Draw3dText("PART_NUMBER        <datamatrix,PART_NUMBER>", 
                m_tCol, l1_plane, 0.16, "Consolas", false, false, TextHorizontalAlignment.Left, TextVerticalAlignment.Bottom);
            e.Display.Draw3dText("CUSTOMER_PTNO   PART_DESCRIPTION CUT DATE: <date,MM/dd/yyyy> <orderid>",
                m_tCol, l2_plane, 0.14, "Consolas", false, false, TextHorizontalAlignment.Left, TextVerticalAlignment.Top);

            base.OnDynamicDraw(e);
        }
    }
}