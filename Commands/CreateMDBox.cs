using System;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using System.Collections.Generic;
using Rhino.DocObjects;
using SQL;
namespace gjTools.Commands
{
    public class CreateMDBox : Command
    {
        public CreateMDBox()
        {
            Instance = this;
        }
        public static CreateMDBox Instance { get; private set; }
        public override string EnglishName => "CreateMDBox";
        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            MDBox.createBox(doc.Layers.CurrentLayer, doc);
            return Result.Success;
        }
    }


    public static class MDBox
    {
        static List<Guid> m_guids = new List<Guid>();
        public static void createBox(Layer layer, RhinoDoc doc)
        {
            var lt = new LayerTools(doc);
            var cutLayers = lt.getAllCutLayers(layer);
            string kerfString = "";
            string pn = layer.Name;
            string name = SQLTool.queryVariableData().userFirstName.ToUpper();
            string date = DateTime.Now.ToString();
            ObjectAttributes oa = new ObjectAttributes();
            oa.LayerIndex = layer.Index;
            var allObjs = new List<RhinoObject>();
            allObjs.AddRange(doc.Objects.FindByLayer(layer));
            

            foreach(Layer lay in cutLayers)
            {
                kerfString += lay.Name.Replace("C_", "[") + ": ";
                double l = 0;
                new List<RhinoObject>(doc.Objects.FindByLayer(lay)).ForEach(x => { l += new ObjRef(x).Curve().GetLength(); });
                kerfString += Math.Ceiling(l).ToString() + "] ";
                allObjs.AddRange(doc.Objects.FindByLayer(lay));
            }

            BoundingBox bb;
            RhinoObject.GetTightBoundingBox(allObjs, out bb);
            Transform t;           
            var scale = 0.0;
            if (bb.GetEdges()[0].Length > bb.GetEdges()[1].Length)
            {
                scale = bb.GetEdges()[0].Length / 10;
            } 
            else
            {
                scale = (bb.GetEdges()[1].Length * (11 / 8.5)) / 9.5;
            }
            Plane pl = new Plane(bb.GetCorners()[0], Vector3d.ZAxis);
            pl.OriginY -= .75 * scale + 1;
            pl.OriginX -= ((scale * 10.85) - (bb.GetEdges()[0].Length)) / 2;
            var DimStyle = doc.DimStyles[new DrawTools(doc).StandardDimstyle()];
            Rectangle3d r = new Rectangle3d(pl, 10.85 * scale, .75 * scale);
            Rectangle3d rh = new Rectangle3d(pl, -.04 * scale, .75 * scale);
            var pld = Plane.WorldXY;
            var f = Font.FromQuartetProperties("Consolas", false, false);
            var ff = Font.FromQuartetProperties("Arial", false, false);

            pl.Origin = new Point3d(r.Corner(3).X + 0.08 * scale, r.Corner(3).Y - 0.08 * scale, 0);

            var pnte = TextEntity.Create("PN: " + pn, pl, DimStyle, false, 0, 0);
            pnte.TextHeight = 0.145 * scale;
            pnte.Justification = TextJustification.TopLeft;
            pnte.SetBold(true);
            pnte.Font = f;


            
            { pl.Origin = new Point3d(r.Corner(3).X + 0.09 * scale, r.Corner(3).Y - 0.38 * scale, 0); }
            var kete = TextEntity.Create("Kerf: " + kerfString, pl, DimStyle, false, 0, 0);
            kete.TextHeight = 0.113 * scale;
            kete.Justification = TextJustification.TopLeft;
            kete.Font = f;


            { pl.Origin = new Point3d(r.Corner(3).X + 0.09 * scale, r.Corner(3).Y - 0.595 * scale, 0); }
            var path = TextEntity.Create("Path: " + doc.Path, pl, DimStyle, false, 0, 0);
            path.TextHeight = 0.072 * scale;
            path.Justification = TextJustification.TopLeft;
            path.Font = f;

            { pl.Origin = new Point3d(r.Corner(1).X - .14 * scale, r.Corner(1).Y + .17 * scale, 0); }
            var md = TextEntity.Create("Measure Drawing", pl, DimStyle, false, 0, 0);
            md.TextHeight = 0.13 * scale;
            md.Justification = TextJustification.BottomRight;
            md.Font = ff;

            { pl.Origin = new Point3d(r.Corner(2).X - .07 * scale, r.Corner(2).Y - .06 * scale, 0); }
            var ndt = TextEntity.Create(name + "\n" + date, pl, DimStyle, false, 0, 0);
            ndt.TextHeight = 0.063 * scale;
            ndt.Justification = TextJustification.TopRight;
            ndt.Font = f;


            var line = new Line(new Point3d(r.Corner(3).X, r.Corner(3).Y - .3 * scale, 0), new Point3d(r.Corner(2).X, r.Corner(2).Y - .3 * scale, 0));
            var linediv = new Line(new Point3d(r.Corner(3).X + 9.12 * scale, r.Corner(3).Y, 0), new Point3d(r.Corner(3).X + 9.12 * scale, r.Corner(1).Y, 0));
            var cl = new List<Curve>();
            cl.Add(rh.ToNurbsCurve());
            var hatch = Hatch.Create(cl, doc.HatchPatterns.FindName("Solid").Index, 0, 1.0, doc.ModelAbsoluteTolerance);

            var guids = new List<Guid>();

            guids.Add(doc.Objects.AddText(pnte));
            
            guids.Add(doc.Objects.AddText(kete, oa));
            guids.Add(doc.Objects.AddText(path, oa));
            guids.Add(doc.Objects.AddText(md, oa));
            guids.Add(doc.Objects.AddText(ndt, oa));
            guids.Add(doc.Objects.AddLine(line, oa));
            guids.Add(doc.Objects.AddLine(linediv, oa));
            guids.Add(doc.Objects.AddRectangle(r, oa));
            guids.Add(doc.Objects.AddRectangle(rh, oa));
            guids.Add(doc.Objects.AddHatch(hatch[0], oa));

            doc.Views.Redraw();
            m_guids = guids;
        }

        public static void deleteBox(RhinoDoc doc)
        {
            doc.Objects.Delete(m_guids, true);
            doc.Views.Redraw();
        }
    }
}