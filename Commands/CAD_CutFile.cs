using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Geometry;
using Rhino.DocObjects;
using Rhino.Commands;
using Rhino.Input.Custom;
using Rhino.UI;

namespace gjTools.Commands
{
    public struct CutData
    {
        public RhinoDoc doc;
        public Layer parentLayer;

        public string cutType;
        public string cutName;
        public string path;
        
        public List<Layer> GetAllSubLayers
        {
            get
            {
                var lays = new List<Layer>();
                var sublays = parentLayer.GetChildren();
                if (lays != null)
                    lays.AddRange(sublays);
                return lays;
            }
        }
        public List<Layer> GetAllCutLayers
        {
            get
            {
                var cutLays = new List<Layer>();
                foreach (var l in GetAllSubLayers)
                    if (l.Name.Substring(0, 2) == "C_" || l.Name == "NestBox")
                        cutLays.Add(l);
                return cutLays;
            }
        }
        public List<RhinoObject> GetCutObjects
        {
            get
            {
                var obj = new List<RhinoObject>();
                var ss = new ObjectEnumeratorSettings
                {
                    ObjectTypeFilter = ObjectType.Curve | ObjectType.Annotation
                };
                foreach(var l in GetAllCutLayers)
                {
                    ss.LayerIndexFilter = l.Index;
                    obj.AddRange(doc.Objects.GetObjectList(ss));
                }
                return obj;
            }
        }
        public List<Guid> GetCutObjInNestBoxID(RhinoObject nestBox)
        {
            var obj = new List<Guid>();
            foreach (var o in GetCutObjInNestBox(nestBox))
                obj.Add(o.Id);
            return obj;
        }
        public List<RhinoObject> GetCutObjInNestBox(RhinoObject nestBox)
        {
            var obj = new List<RhinoObject>();
            var nestBB = nestBox.Geometry.GetBoundingBox(false);

            foreach (var o in GetCutObjects)
            {
                if (nestBB.Contains(o.Geometry.GetBoundingBox(true), false))
                    obj.Add(o);
            }
            return obj;
        }
        public List<RhinoObject> GetNestBox
        {
            get
            {
                var nestBox = new List<RhinoObject>();
                int nestIndex = doc.Layers.FindByFullPath(parentLayer.Name + "::NestBox", -1);
                if (nestIndex == -1)
                    return nestBox;

                var ss = new ObjectEnumeratorSettings
                {
                    ObjectTypeFilter = ObjectType.Curve,
                    LayerIndexFilter = nestIndex
                };
                nestBox.AddRange(doc.Objects.GetObjectList(ss));
                return nestBox;
            }
        }
    }

    public struct Paths
    {
        public RhinoDoc doc;
        public SQLTools sql;

        public Paths(RhinoDoc document, SQLTools sqlt)
        {
            doc = document;
            sql = sqlt;
        }
        public List<string> cutTypes
        {
            get
            {
                var types = new List<string>
                {
                    "LocalTemp",
                    "Router",
                    "Default",
                    "CustomDefault"
                };
                if (doc.Path != null)
                    types.Add("WorkingLocation");
                return types;
            }
        }
        public List<string> paths
        {
            get
            {
                var loc = sql.queryLocations();
                var path = new List<string>
                {
                    loc[3].path,
                    loc[1].path,
                    loc[0].path,
                    loc[0].path
                };
                if (doc.Path != null)
                    path.Add(doc.Path);
                return path;
            }
        }
        public string DefaultChoice
        {
            get
            {
                return cutTypes[2];
            }
        }
        public string PathOfType(string locationName)
        {
            if (locationName == null)
                return locationName;
            return paths[cutTypes.IndexOf(locationName)];
        }
    }



    [CommandStyle(Style.ScriptRunner)]
    public class CAD_CutFile : Command
    {
        public CAD_CutFile()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static CAD_CutFile Instance { get; private set; }

        public override string EnglishName => "CAD_CutFile";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var sql = new SQLTools();
            var lt = new LayerTools(doc);
            var info = new CutData { doc = doc };
            doc.Objects.UnselectAll();

            // get locations from DB
            var loc = new Paths(doc, sql);

            // user chooses a cut location
            info.cutType = Dialogs.ShowListBox("Cut Location", "Choose a Location", loc.cutTypes, loc.DefaultChoice) as string;
            if (info.cutType == null)
                return Result.Cancel;

            info.path = loc.PathOfType(info.cutType);

            // ask for the layer to send cut file out from
            var pLay = Dialogs.ShowListBox("Layers", "Choose a Layer", lt.getAllParentLayersStrings(), doc.Layers.CurrentLayer.Name) as string;
            if (pLay == null)
                return Result.Cancel;

            info.parentLayer = lt.CreateLayer(pLay);

            // find intended nesting box if multiple
            var NestingBox = CheckMultipleNestBox(info);
            if (NestingBox == null)
                return Result.Cancel;

            // get the name of the cutfile
            info.cutName = CutName(doc, info, loc, sql);
            if (info.cutName == null)
                return Result.Cancel;

            // make the cut display box
            MakeCutNameText(info, NestingBox);
            doc.Objects.UnselectAll();

            // Make the DXF
            MakeDXF(info, NestingBox);

            doc.Views.Redraw();
            return Result.Success;
        }




        /// <summary>
        /// Ask the user to select a nestbox if there are multiple
        /// </summary>
        /// <param name="info"></param>
        /// <returns>nestbox or null</returns>
        public RhinoObject CheckMultipleNestBox(CutData info)
        {
            var nestBox = info.GetNestBox;
            if (nestBox.Count > 1)
            {
                var show = new Rhino.Display.CustomDisplay(true);
                foreach (var o in nestBox)
                    show.AddCurve((Curve)o.Geometry, System.Drawing.Color.Aquamarine, 5);
                info.doc.Views.Redraw();

                while (true)
                {
                    var sel = Rhino.Input.RhinoGet.GetOneObject("Select a Highlighted NestBox", false, ObjectType.Curve, out ObjRef newNestBox);
                    if (sel != Result.Success)
                    {
                        show.Dispose();
                        info.doc.Views.Redraw();
                        return null;
                    }
                    else
                    {
                        foreach(var o in nestBox)
                            if (newNestBox.ObjectId == o.Id)
                            {
                                show.Dispose();
                                info.doc.Views.Redraw();
                                RhinoApp.WriteLine("Using selected Nesting Box, thanks");
                                return newNestBox.Object();
                            }
                    }
                    RhinoApp.WriteLine("That wasnt a valid NestBox on layer {0}...", info.parentLayer.Name);
                    info.doc.Objects.UnselectAll();
                    info.doc.Views.Redraw();
                }
            }
            if (nestBox.Count == 1)
            {
                return nestBox[0];
            }
            
            return null;
        }

        /// <summary>
        /// Determine the cut name based on info gathered
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="info"></param>
        /// <param name="loc"></param>
        /// <param name="sql"></param>
        /// <returns></returns>
        public string CutName(RhinoDoc doc, CutData info, Paths loc, SQLTools sql)
        {
            if (info.cutType == loc.cutTypes[2])
            {
                var vb = sql.queryVariableData()[0];
                var cutNum = string.Format("{0}{1}", vb.userInitials, vb.cutNumber);
                vb.cutNumber++;
                sql.updateVariableData(vb);
                return cutNum;
            }
            else if (info.cutType == loc.cutTypes[3])
            {
                var num = 1000;
                var res = Rhino.Input.RhinoGet.GetInteger("Specify Cut Number", false, ref num, 1000, 10000);
                if (res != Result.Success)
                    return null;
                var vb = sql.queryVariableData()[0];
                return string.Format("{0}{1}", vb.userInitials, num);
            }
            else if (info.cutType == loc.cutTypes[1])
            {
                var route = "";
                if (doc.Path != null)
                {
                    if (doc.Path.Contains("J0"))
                    {
                        var regx = new System.Text.RegularExpressions.Regex(@"J\d{9}");
                        var job = regx.Match(doc.Path).Value;
                        route = GetStringPremade("Choose a Route Name", new List<string> { info.parentLayer.Name, job }, 0);
                    }
                }
                else
                {
                    route = GetStringPremade("Choose a Route Name", new List<string> { info.parentLayer.Name }, 0);
                }
                if (route == null)
                    return null;

                int rNum = 0;
                var res = Rhino.Input.RhinoGet.GetInteger("Route Number", true, ref rNum);
                if (res == Result.Nothing || rNum == 0)
                    return $"{route}-ROUTE";
                if (res == Result.Cancel)
                    return null;
                if (res == Result.Success)
                    return $"{route}-ROUTE{rNum}";
            }

            return info.parentLayer.Name;
        }

        /// <summary>
        /// Rhino's getstring custom with options bundled up
        /// </summary>
        /// <param name="prompt"></param>
        /// <param name="options"></param>
        /// <param name="defaultIndex"></param>
        /// <returns></returns>
        public string GetStringPremade(string prompt, List<string> options, int defaultIndex)
        {
            var gs = new GetString();

            if (options.Count > 0)
            {
                foreach (var o in options)
                    gs.AddOption(o);
                gs.SetDefaultString(options[defaultIndex]);
            }

            gs.SetCommandPrompt(prompt);
            var result = gs.Get();

            if (result == Rhino.Input.GetResult.Option)
                return gs.Option().EnglishName;
            else if (result == Rhino.Input.GetResult.String)
                return gs.StringResult().Trim();
            else
                return null;
        }

        /// <summary>
        /// Makes a DXF CutFile if the directory exists
        /// </summary>
        /// <param name="info"></param>
        /// <returns>True if the file output is successfull</returns>
        public bool MakeDXF(CutData info, RhinoObject nestbox)
        {
            if (!System.IO.Directory.Exists(info.path))
            {
                // Directory is not reachable right now
                RhinoApp.WriteLine($"Path: {info.path}");
                RhinoApp.WriteLine("The Above Cut Directory is not available, No CutFile was Made..");
                return false;
            }

            // check for Non-Polylines
            var dt = new DrawTools(info.doc);
            var obj = info.GetCutObjInNestBox(nestbox);

            if (!dt.CheckPolylines(obj, true))
            {
                var tmp = "";
                Rhino.Input.RhinoGet.GetString("Not all lines are Polylines...", true, ref tmp);
                dt.hideDynamicDraw();
            }
            else
            {
                dt.hideDynamicDraw();
                info.doc.Objects.Select(info.GetCutObjInNestBoxID(nestbox));
                return RhinoApp.RunScript($"_-Export \"{info.path}{info.cutName}.dxf\" Scheme \"Vomela\" _Enter", false);
            }
            
            return false;
        }

        /// <summary>
        /// Create the top box with cut name
        /// </summary>
        /// <param name="info"></param>
        /// <param name="nestBox"></param>
        public void MakeCutNameText(CutData info, RhinoObject nestBox)
        {
            // Time to create the text
            var dt = new DrawTools(info.doc);
            var ds = dt.StandardDimstyle();
            var txtHeader = dt.AddText("Cut Name:", new Point3d(0, 0, 0), ds, 0.75, 0, 0, 6);
            var txtName = dt.AddText(info.cutName.ToUpper(), new Point3d(0,-1,0), ds, 1.5, 0, 0, 0);

            BoundingBox bb = new BoundingBox();
                bb.Union(txtHeader.GetBoundingBox(true));
                bb.Union(txtName.GetBoundingBox(true));
            bb.Inflate(0.5);

            var textBox = new Rectangle3d(Plane.WorldXY, bb.GetCorners()[0], bb.GetCorners()[2]);
            var nc = nestBox.Geometry.GetBoundingBox(true).GetCorners()[2];

            // Move the box to location
            Transform move = Transform.Translation(nc.X - textBox.Corner(1).X, nc.Y - textBox.Corner(1).Y, 0);
            Transform scale;
            if (info.cutType == "Router")
                scale = Transform.Scale(nc, (nestBox.Geometry.GetBoundingBox(true).GetEdges()[0].Length * 0.4) / textBox.Width);
            else
                scale = Transform.Scale(nc, (nestBox.Geometry.GetBoundingBox(true).GetEdges()[0].Length * 0.25) / textBox.Width);

            textBox.Transform(move);
            textBox.Transform(scale);
            txtHeader.Transform(move);
            txtHeader.Transform(scale, info.doc.DimStyles[ds]);
            txtName.Transform(move);
            txtName.Transform(scale, info.doc.DimStyles[ds]);

            // add them to the drawing
            var cutBox = new List<RhinoObject> {
                info.doc.Objects.FindId(info.doc.Objects.AddText(txtHeader)),
                info.doc.Objects.FindId(info.doc.Objects.AddText(txtName)),
                info.doc.Objects.FindId(info.doc.Objects.AddRectangle(textBox))
            };

            // make a group
            var group = info.doc.Groups.Add();

            // change their layer
            foreach(var o in cutBox)
            {
                o.Attributes.LayerIndex = info.parentLayer.Index;
                o.Attributes.AddToGroup(group);
                o.CommitChanges();
            }
            info.doc.Views.Redraw();
        }
    }
}