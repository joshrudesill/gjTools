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
            // setting up variables that are required
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
            
            // Calculating kerf and creating kerf string
            foreach(Layer lay in cutLayers)
            {
                kerfString += lay.Name.Replace("C_", "[") + ": ";
                double l = 0;
                new List<RhinoObject>(doc.Objects.FindByLayer(lay)).ForEach(x => { l += new ObjRef(x).Curve().GetLength(); }); // proud of this one.
                kerfString += Math.Ceiling(l).ToString() + "] ";
                allObjs.AddRange(doc.Objects.FindByLayer(lay));
            }

            BoundingBox bb;
            RhinoObject.GetTightBoundingBox(allObjs, out bb); //bb of all objects on layer
            
            var scale = 0.0; //getting scale
            if (bb.GetEdges()[0].Length > bb.GetEdges()[1].Length)
            {
                scale = bb.GetEdges()[0].Length / 10.85; // if part wider than tall (10.85 is width of box in its original form)
            } 
            else if (bb.GetEdges()[0].Length < bb.GetEdges()[1].Length)
            {
                scale = bb.GetEdges()[1].Length / ((8.415-.75) * .95); // part taller than wide 8.415 is how tall the 8.5 x 11 is if the 11 is scaled down to 10.85 then you take off .75 because thats how tall the inof box is
            }                                                              // then you take off 5% to not have the part or dims touch info box.
            else
            {
                scale = bb.GetEdges()[1].Length / ((8.415 - .75) * .95);
            }
            var dtm = ((8.415 * scale) - (bb.GetEdges()[1].Length)) / 2; // dtm or distance to move is for moving the box down to have the part centered vertically based on the outside bounding box.
            Plane pl = new Plane(bb.GetCorners()[0], Vector3d.ZAxis); //starting plane for object placement
            pl.OriginY -= dtm + ((scale * .75) / 2); // moving origin down the dtm plus an extra half the height of the info box once it is scaled. (remember the dtm is calculated based on the entire boudning box)
            pl.OriginX -= ((scale * 10.85) - (bb.GetEdges()[0].Length)) / 2; // centering boxes horizontally
            pl.OriginX += .02 * scale; // this is to move it the last bit because of the hatch. Can be on above line but its already crowded so deal with it.
            var DimStyle = doc.DimStyles[new DrawTools(doc).StandardDimstyle()]; //dim style
            Rectangle3d r = new Rectangle3d(pl, 10.85 * scale, .75 * scale); //creating scaled info box
            Rectangle3d rh = new Rectangle3d(pl, -.04 * scale, .75 * scale); // hatch box
           
            pl.OriginX -= .04 * scale; //moving origin back to bottom left of hatch box
            Rectangle3d rbb = new Rectangle3d(pl, 10.89 * scale, 8.415 * scale); // creating bounding box. 10.89 is from 10.85 + .04 , quick maths. Mad?
            var f = Font.FromQuartetProperties("Consolas", false, false); //font junk
            var ff = Font.FromQuartetProperties("Arial", false, false);

            pl.Origin = new Point3d(r.Corner(3).X + 0.08 * scale, r.Corner(3).Y - 0.08 * scale, 0); // creating text. Do I really need to explain this?

            var pnte = TextEntity.Create("PN: " + pn, pl, DimStyle, false, 0, 0);
            pnte.TextHeight = 0.145 * scale;
            pnte.Justification = TextJustification.TopLeft;
            pnte.SetBold(true);
            pnte.Font = f;

            
            { pl.Origin = new Point3d(r.Corner(3).X + 0.09 * scale, r.Corner(3).Y - 0.38 * scale, 0); } //scoped for swag purposes
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

            //drawing lines
            var line = new Line(new Point3d(r.Corner(3).X, r.Corner(3).Y - .3 * scale, 0), new Point3d(r.Corner(2).X, r.Corner(2).Y - .3 * scale, 0));
            var linediv = new Line(new Point3d(r.Corner(3).X + 9.12 * scale, r.Corner(3).Y, 0), new Point3d(r.Corner(3).X + 9.12 * scale, r.Corner(1).Y, 0));
            var cl = new List<Curve>();
            cl.Add(rh.ToNurbsCurve());
            var hatch = Hatch.Create(cl, doc.HatchPatterns.FindName("Solid").Index, 0, 1.0, doc.ModelAbsoluteTolerance); //hatch

            var guids = new List<Guid>();

            //adding to doc and guids prop simultaneously. Who needs multithreading
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
            
            oa.Visible = false; // for debug purposes
            guids.Add(doc.Objects.AddRectangle(rbb, oa));

            doc.Views.Redraw(); //redraw that shit
            m_guids = guids; // setting member variable equal to locally scoped variable
        }

        public static void deleteBox(RhinoDoc doc) // clean up
        {
            doc.Objects.Delete(m_guids, true);
            doc.Views.Redraw();
        }
    }
}