using System;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using System.Collections.Generic;
using Rhino.DocObjects;
using SQL;

namespace gjTools.Commands
{
    public class FinishMD : Command
    {
        public FinishMD()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static FinishMD Instance { get; private set; }
        static List<Guid> m_guids = new List<Guid>();
        public override string EnglishName => "FinishMD";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            createBox(doc.Layers.CurrentLayer, doc);
            return Result.Success;
        }

        public static void createBox(Layer layer, RhinoDoc doc)
        {
            LayerTools lt = new LayerTools(doc);
            List<Layer> cutLayers = lt.getAllCutLayers(layer);
            List<RhinoObject> allObjs = new List<RhinoObject>();
            allObjs.AddRange(doc.Objects.FindByLayer(layer));

            ObjectAttributes oa = new ObjectAttributes();
            oa.LayerIndex = layer.Index;

            Font MainFont = Font.FromQuartetProperties("Consolas", false, false); 
            Font DecorativeFont = Font.FromQuartetProperties("Arial", false, false);
            DimensionStyle DimStyle = doc.DimStyles[new DrawTools(doc).StandardDimstyle()];

            string kerfString = "";
            string partNumber = layer.Name;
            string UserName = SQLTool.queryVariableData().userFirstName.ToUpper();
            string CurrentDate = DateTime.Now.ToString();

            foreach (Layer lay in cutLayers)
            {
                kerfString += lay.Name.Replace("C_", "[") + ": ";
                double l = 0;
                new List<RhinoObject>(doc.Objects.FindByLayer(lay)).ForEach(x => { l += new ObjRef(x).Curve().GetLength(); }); // proud of this one.
                kerfString += Math.Ceiling(l).ToString() + "] ";
                allObjs.AddRange(doc.Objects.FindByLayer(lay));
            }

            BoundingBox bb = BoundingBox.Empty;
            RhinoObject.GetTightBoundingBox(allObjs, out bb);

            double scale;

            if (bb.GetEdges()[0].Length > bb.GetEdges()[1].Length && (1.43 < (bb.GetEdges()[0].Length / bb.GetEdges()[1].Length) || 1.29 > (bb.GetEdges()[0].Length / bb.GetEdges()[1].Length)))
            {
                scale = bb.GetEdges()[0].Length / 10.89; 
                RhinoApp.WriteLine("wider");
            }
            else
            {
                scale = bb.GetEdges()[1].Length / (8.385 - 0.75);
                RhinoApp.WriteLine("taller");
            }
            
            RhinoApp.WriteLine(scale.ToString());
            Point3d pts = new Point3d(bb.Center.X - (10.89 / 2), bb.Center.Y - ((8.385 + 0.75) / 2), 0);
            Plane pl = new Plane(pts, Vector3d.ZAxis);

            Rectangle3d r = new Rectangle3d(pl, 10.89 , .75 );
            pl.OriginY += 0.75;
            Rectangle3d r2 = new Rectangle3d(pl, 10.89 , 8.385 - 0.75 );
            pl.OriginY -= 0.75;
            pl.OriginX += .04;
            Rectangle3d rh = new Rectangle3d(pl, -.04 , .75);
            pl.OriginX -= .04;

            pl.Origin = new Point3d(r.Corner(3).X + 0.08 , r.Corner(3).Y - 0.08 , 0);

            var pnte = TextEntity.Create("PN: " + partNumber, pl, DimStyle, false, 0, 0);
            pnte.TextHeight = 0.145 ;
            pnte.Justification = TextJustification.TopLeft;
            pnte.SetBold(true);
            pnte.Font = MainFont;

            { pl.Origin = new Point3d(r.Corner(3).X + 0.09 , r.Corner(3).Y - 0.38 , 0); } 
            var kete = TextEntity.Create("Kerf: " + kerfString, pl, DimStyle, false, 0, 0);
            kete.TextHeight = 0.113 ;
            kete.Justification = TextJustification.TopLeft;
            kete.Font = MainFont;


            { pl.Origin = new Point3d(r.Corner(3).X + 0.09 , r.Corner(3).Y - 0.595 , 0); }
            var path = TextEntity.Create("Path: " + doc.Path, pl, DimStyle, false, 0, 0);
            path.TextHeight = 0.072 ;
            path.Justification = TextJustification.TopLeft;
            path.Font = MainFont;

            { pl.Origin = new Point3d(r.Corner(1).X - .14 , r.Corner(1).Y + .17 , 0); }
            var md = TextEntity.Create("Measure Drawing", pl, DimStyle, false, 0, 0);
            md.TextHeight = 0.13 ;
            md.Justification = TextJustification.BottomRight;
            md.Font = DecorativeFont;

            { pl.Origin = new Point3d(r.Corner(2).X - .07 , r.Corner(2).Y - .06 , 0); }
            var ndt = TextEntity.Create(UserName + "\n" + CurrentDate, pl, DimStyle, false, 0, 0);
            ndt.TextHeight = 0.063 ;
            ndt.Justification = TextJustification.TopRight;
            ndt.Font = MainFont;

            var line = new Line(new Point3d(r.Corner(3).X, r.Corner(3).Y - .3, 0), new Point3d(r.Corner(2).X, r.Corner(2).Y - .3, 0));
            var linediv = new Line(new Point3d(r.Corner(3).X + 9.12, r.Corner(3).Y, 0), new Point3d(r.Corner(3).X + 9.12, r.Corner(1).Y, 0));
            var cl = new List<Curve>();
            cl.Add(rh.ToNurbsCurve());
            var hatch = Hatch.Create(cl, doc.HatchPatterns.FindName("Solid").Index, 0, 1.0, doc.ModelAbsoluteTolerance);

            scale *= 1.02;

            var xForm = Transform.Scale(r2.Center, scale);

            r.Transform(xForm);
            r2.Transform(xForm);
            rh.Transform(xForm);
            md.Transform(xForm, DimStyle);
            ndt.Transform(xForm, DimStyle);
            pnte.Transform(xForm, DimStyle);
            kete.Transform(xForm, DimStyle);
            path.Transform(xForm, DimStyle);
            line.Transform(xForm);
            linediv.Transform(xForm);
            hatch[0].Transform(xForm);

            List<Guid> guids = new List<Guid>();

            guids.Add(doc.Objects.AddText(md, oa));
            guids.Add(doc.Objects.AddText(ndt, oa));
            guids.Add(doc.Objects.AddText(kete, oa));
            guids.Add(doc.Objects.AddText(pnte, oa));
            guids.Add(doc.Objects.AddText(path, oa));
            guids.Add(doc.Objects.AddLine(line, oa));
            guids.Add(doc.Objects.AddRectangle(r, oa));
            guids.Add(doc.Objects.AddRectangle(rh, oa));
            guids.Add(doc.Objects.AddLine(linediv, oa));
            guids.Add(doc.Objects.AddHatch(hatch[0], oa));

            oa.Visible = false;
            guids.Add(doc.Objects.AddRectangle(r2, oa));

            doc.Views.Redraw();
        }

        public static void deleteBox(RhinoDoc doc)
        {
            doc.Objects.Delete(m_guids, true);
            doc.Views.Redraw();
        }
    }
}