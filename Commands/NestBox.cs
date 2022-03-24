using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.DocObjects;

namespace gjTools.Commands
{
    public class NestBox : Command
    {
        public NestBox()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static NestBox Instance { get; private set; }

        public override string EnglishName => "NestBox";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // Get some Objects
            if (RhinoGet.GetMultipleObjects("Select Objects", false, ObjectType.Curve, out ObjRef[] sel) != Result.Success)
                return Result.Cancel;

            // This crushes the data
            var NestBox = new NestBoxMaker(doc, new List<ObjRef>(sel));
            if (NestBox.CutLayers.Count == 0)
            {
                RhinoApp.WriteLine("No Cut Layers Were in your Selection");
                return Result.Cancel;
            }

            double height = 1;
            if (RhinoGet.GetNumber("Layout Height (Values Below 3 Shrinks to the Content)", false, ref height) != Result.Success)
                return Result.Cancel;
            NestBox.SetSheetDims(height, -1);

            double width = 1;
            if (RhinoGet.GetNumber("Layout Width (Values Below 3 Shrinks to the Content)", false, ref width) != Result.Success)
                return Result.Cancel;
            NestBox.SetSheetDims(-1, width);

            DrawNestBox(NestBox);

            doc.Views.Redraw();
            return Result.Success;
        }

        public void DrawNestBox(NestBoxMaker NestBox)
        {
            var nestLayer = new LayerTools(NestBox.doc).CreateLayer("NestBox", NestBox.ParentLayer.Name);
            var grp = NestBox.doc.Groups.Add();
            var attr = new ObjectAttributes { LayerIndex = NestBox.ParentLayer.Index };
                attr.AddToGroup(grp);
            var attr1 = new ObjectAttributes { LayerIndex = nestLayer.Index, Name = "NestBox" };
                attr1.SetUserString("Width", NestBox.Width.ToString());
                attr1.SetUserString("Height", NestBox.Height.ToString());
                attr1.SetUserString("QtyObj", NestBox.Geoms.ObjCount.ToString());
                attr1.SetUserString("QtyGrp", NestBox.Geoms.GroupCount.ToString());
                attr1.SetUserString("MatlUsage", NestBox.SheetUsage.ToString());

            foreach (var te in NestBox.Geoms.TxtEnt)
                NestBox.doc.Objects.AddText(te, attr);

            NestBox.doc.Objects.AddRectangle(NestBox.Geoms.NestBox, attr1);
            NestBox.doc.Objects.AddRectangle(NestBox.Geoms.LabelBox, attr);
            NestBox.doc.Objects.AddLine(NestBox.Geoms.DividerLine, attr);
        }
    }






    public class NestBoxMaker
    {
        public RhinoDoc doc { get; private set; }
        public Layer ParentLayer { get; private set; }
        public LabelGeometry Geoms { get; private set; }

        public List<ObjRef> N_Objs { get; private set; }
        public List<Cut_Layer> CutLayers { get; private set; }
        public double Height { get; private set; }
        public double Width { get; private set; }
        public double SheetUsage { get; private set; }
        private Point3d Center = Point3d.Origin;

        /// <summary>
        /// Selection to have a nestBox object made
        /// </summary>
        /// <param name="Document"></param>
        /// <param name="objs"></param>
        public NestBoxMaker(RhinoDoc Document, List<ObjRef> objs)
        {
            doc = Document;
            N_Objs = objs;
            CutLayers = new List<Cut_Layer>(10);
            SheetUsage = 0;

            var leftovers = objs;
            while (true)
            {
                var cut = new Cut_Layer(leftovers);
                leftovers = cut.GetLeftovers();

                if (cut.IsCut)
                    CutLayers.Add(cut);

                // No leftovers
                if (leftovers.Count == 0) break;
            }

            if (CutLayers.Count > 0)
            {
                UpdateActualSizes();
                GetParentLayer();
                SetupGeometry();
            }
        }

        /// <summary>
        /// Sample on of the items for the parent layer
        /// <para>Also Known as the part number</para>
        /// </summary>
        private void GetParentLayer()
        {
            Guid ParentId = CutLayers[0].Layer.ParentLayerId;
            ParentLayer = doc.Layers.FindId(ParentId);
        }

        /// <summary>
        /// Write the width and height based on part sizes
        /// </summary>
        private void UpdateActualSizes()
        {
            var tmpBBox = BoundingBox.Empty;

            foreach(var c in CutLayers)
                tmpBBox.Union(c.BB);

            // Update the actual width and height
            var edges = tmpBBox.GetEdges();
            Width = edges[0].Length;
            Height = edges[1].Length;
            Center = tmpBBox.Center;
        }

        /// <summary>
        /// Round up to nearest Quarter inch
        /// </summary>
        /// <param name="Num"></param>
        /// <returns></returns>
        private double RoundQuarter(double Num)
        {
            double Rounded = Math.Round(Num * 4, 0) / 4;

            if (Num - Rounded > 0.11)
                Rounded += 0.25;

            return Rounded;
        }

        /// <summary>
        /// Set the Sheet Size
        /// </summary>
        /// <param name="height">-1 to skip</param>
        /// <param name="width">-1 to skip</param>
        public void SetSheetDims(double height = -1, double width = -1)
        {
            // larger than 3 Keep exact amount
            if (height != -1)
            {
                if (height > 3)
                    Height = height;
                else
                    Height = RoundQuarter((height * 2) + Height);
            }
            if (width != -1)
            {
                if (width > 3)
                    Width = width;
                else
                    Width = RoundQuarter((width * 2) + Width);
            }

            SetupGeometry();
        }

        /// <summary>
        /// Rectangle representing the NestBox
        /// </summary>
        /// <returns></returns>
        private void SetupGeometry()
        {
            var NestGeom = new LabelGeometry();
            var DimStyle = doc.DimStyles[new DrawTools(doc).StandardDimstyle()];

            var boxPlane = new Plane(Center + new Point3d(-Width / 2, -Height / 2, 0), Vector3d.ZAxis);

            double BaseTxtHeight = Width * 0.0135;
            var txtHeight = new List<double>(5)
            {
                BaseTxtHeight * 1.5,
                BaseTxtHeight * 0.90,
                BaseTxtHeight * 0.60,
                BaseTxtHeight * 0.90,
                BaseTxtHeight * 0.90
            };

            double inset = BaseTxtHeight / 1.5;
            var ptOffsets = new List<Point3d>(5)
            {
                new Point3d(inset, -inset, 0),
                new Point3d(inset, -(inset + txtHeight[0] * 1.5), 0),
                new Point3d(Width - inset, -inset, 0),
                new Point3d(inset, -(inset + (txtHeight[0] + txtHeight[1]) * 2), 0),
                new Point3d(inset, -(inset + (txtHeight[0] + txtHeight[1] + txtHeight[3]) * 2), 0)
            };

            var path = (doc.Path == null) ? "File Not Saved" : doc.Path;
            var name = new SQLTools().queryVariableData().userFirstName;
            int qty = 0;
            int grps = 0;
            string cutLengths = "";
            var tmpBB = BoundingBox.Empty;

            foreach(var c in CutLayers)
            {
                cutLengths += $"[{c.Name}: {(int)c.CutLength}] ";
                tmpBB.Union(c.BB);
                qty = (qty >= c.Obj.Count) ? qty : c.Obj.Count;
                grps += c.GroupCount;
            }
            NestGeom.ObjCount = qty;
            NestGeom.GroupCount = grps;
            if (grps > 0)
                qty = grps;

            SheetUsage = Width / qty;

            var strVals = new List<string>(5)
            {
                $"PN: {ParentLayer.Name}",
                $"Path: {path}",
                $"{name}\n{DateTime.Now}",
                $"Sheet Size: {Width}w x {Height}h    (Part Area: {Math.Round(tmpBB.GetEdges()[0].Length, 2)}w x {Math.Round(tmpBB.GetEdges()[1].Length, 2)}h)",
                $"Items up: {qty}   Kerf: {cutLengths}"
            };

            var txtList = new List<TextEntity>(5)
            {
                TextEntity.Create(strVals[0], ModifyPlane(boxPlane, ptOffsets[0]), DimStyle, false, 0, 0),
                TextEntity.Create(strVals[1], ModifyPlane(boxPlane, ptOffsets[1]), DimStyle, false, 0, 0),
                TextEntity.Create(strVals[2], ModifyPlane(boxPlane, ptOffsets[2]), DimStyle, false, 0, 0),
                TextEntity.Create(strVals[3], ModifyPlane(boxPlane, ptOffsets[3]), DimStyle, false, 0, 0),
                TextEntity.Create(strVals[4], ModifyPlane(boxPlane, ptOffsets[4]), DimStyle, false, 0, 0)
            };
            txtList[0].SetBold(true);
            txtList[3].SetBold(true);
            txtList[2].Justification = TextJustification.Right;

            tmpBB = BoundingBox.Empty;
            for (int i = 0; i < 5; i++)
            {
                txtList[i].TextHeight = txtHeight[i];
                txtList[i].Font = Font.FromQuartetProperties("Consolas", false, false);
                tmpBB.Union(txtList[i].GetBoundingBox(true));
            }

            NestGeom.TxtEnt = txtList;
            NestGeom.NestBox = new Rectangle3d(boxPlane, Width, Height);
            NestGeom.LabelBox = new Rectangle3d(boxPlane, Width, -(tmpBB.GetEdges()[1].Length + (inset * 2)));

            // Resize the path if too big
            var pBB = txtList[1].GetBoundingBox(true);
            if (pBB.GetEdges()[0].Length > NestGeom.LabelBox.Width + (inset * 2))
            {
                double scal = (NestGeom.LabelBox.Width - (inset * 2)) / pBB.GetEdges()[0].Length;
                var xForm = Transform.Scale(pBB.Corner(true, true, true), scal);
                txtList[1].Transform(xForm, DimStyle);
            }

            var LabelCenter = NestGeom.LabelBox.Center;
            NestGeom.DividerLine = new Line(
                LabelCenter + new Point3d(-(Width / 2) + (inset * 2), 0, 0), 
                LabelCenter + new Point3d(Width / 2 - (inset * 2), 0, 0)
                );

            Plane ModifyPlane(Plane pl, Point3d off)
            {
                var p = new Plane(pl);
                p.Origin += off;
                return p;
            }

            Geoms = NestGeom;
        }

        public struct LabelGeometry
        {
            public Rectangle3d NestBox { get; set; }
            public Rectangle3d LabelBox { get; set; }
            public Line DividerLine { get; set; }

            public int ObjCount { get; set; }
            public int GroupCount { get; set; }

            public List<TextEntity> TxtEnt { get; set; }
        }
    }
    

    public class Cut_Layer
    {
        public string Name { get; private set; }
        public Layer Layer { get; private set; }
        public bool IsCut { get; private set; }

        private List<ObjRef> Leftovers;
        public List<ObjRef> Obj { get; private set; }
        public double CutLength { get; private set; }
        public int GroupCount { get; private set; }

        public double Width { get; private set; }
        public double Height { get; private set; }
        public BoundingBox BB { get; private set; }


        /// <summary>
        /// Creat a Cut object
        /// </summary>
        /// <param name="objects"></param>
        public Cut_Layer(List<ObjRef> selection)
        {
            var doc = selection[0].Object().Document;
            Layer = doc.Layers[selection[0].Object().Attributes.LayerIndex];
            IsCut = false;
            Name = Layer.Name;

            // object Setup
            Obj = new List<ObjRef>(selection.Count);
            Leftovers = new List<ObjRef>(selection.Count);
            CutLength = 0;

            // is this a cut object layer
            if (Name.StartsWith("C_"))
            {
                IsCut = true;
                Name = Name.Replace("C_", "");

                // Process all parts an count groups
                ProcessParts(selection);

                if (BB.IsValid)
                {
                    var edge = BB.GetEdges();
                    Width = edge[0].Length;
                    Height = edge[1].Length;
                }
            }
        }

        /// <summary>
        /// Count groups and add similar cut parts
        /// </summary>
        /// <param name="objects"></param>
        /// <returns></returns>
        private void ProcessParts(List<ObjRef> objects)
        {
            // Temp to count all unique group indexes
            var tmpGroupList = new List<int>();
            var BBox = BoundingBox.Empty;

            // Process parts
            for (int i = 0; i < objects.Count; i++)
            {
                var o = objects[i];
                var o_attr = o.Object().Attributes;

                if (o_attr.LayerIndex != Layer.Index)
                {
                    Leftovers.Add(o);
                }
                else
                {
                    // belongs to this object
                    Obj.Add(o);
                    var partBB = o.Geometry().GetBoundingBox(true);
                    BBox.Union(partBB);

                    // Cut length
                    var crv = o.Curve();
                    if (crv != null)
                        CutLength += crv.GetLength(0.1);

                    // Group count
                    if (o_attr.GroupCount > 0)
                        if (!tmpGroupList.Contains(o_attr.GetGroupList()[0]))
                            tmpGroupList.Add(o_attr.GetGroupList()[0]);
                }
            }

            BB = BBox;
            GroupCount = tmpGroupList.Count;
        }

        /// <summary>
        /// Can be read exactly one time
        /// </summary>
        public List<ObjRef> GetLeftovers()
        {
            var cpy = new List<ObjRef>(Leftovers.Count);
            if (Leftovers.Count > 0)
            {
                cpy.AddRange(Leftovers);
                Leftovers.Clear();
            }

            return cpy;
        }
    }
}