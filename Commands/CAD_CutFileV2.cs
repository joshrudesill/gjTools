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
    public struct CutDatas
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
        public List<RhinoObject> GetCutObjInNestBox (RhinoObject nestBox)
        {
            var obj = new List<RhinoObject>();
            foreach(var o in GetCutObjects)
            {
                if (nestBox.Geometry.GetBoundingBox(true).Contains(o.Geometry.GetBoundingBox(true), true))
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

    [CommandStyle(Style.ScriptRunner)]
    public class eCAD_CutFile : Command
    {
        public eCAD_CutFile()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static eCAD_CutFile Instance { get; private set; }

        public override string EnglishName => "gjeCAD_CutFile";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var sql = new SQLTools();
            var lt = new LayerTools(doc);
            var info = new CutDatas { doc = doc };

            // get locations from DB
            var paths = new List<string>();
            var locationNames = new List<string>();
            foreach(var line in sql.queryLocations())
            {
                paths.Add(line.path);
                locationNames.Add(line.locName);
            }

            // File is a 3DM File, path data is available to add working Location
            if (doc.Name != "")
            {
                locationNames.Insert(0, "WorkingLocation");
                paths.Insert(0, doc.Path.Replace(doc.Name, ""));
            }

            // user chooses a cut location
            info.cutType = Dialogs.ShowListBox("Cut Location", "Choose a Location", locationNames) as string;
            if (info.cutType == null)
                return Result.Cancel;

            info.path = paths[locationNames.IndexOf(info.cutType)];

            // ask for the layer to send cut file out from
            var pLay = Dialogs.ShowListBox("Layers", "Choose a Layer", lt.getAllParentLayersStrings()) as string;
            if (pLay == null)
                return Result.Cancel;

            info.parentLayer = lt.CreateLayer(pLay);



            doc.Views.Redraw();
            return Result.Success;
        }


        public string CutName(RhinoDoc doc, CutDatas info)
        {

            return "farts";
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
        /// Send out the DXF file
        /// </summary>
        /// <param name="fullPath"></param>
        public void MakeDXF(string fullPath)
        {
            RhinoApp.RunScript("_-Export \"" + fullPath + "\" Scheme \"Vomela\" _Enter", false);
        }

        /// <summary>
        /// Create the top box with cut name
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public CutData MakeCutNameText(CutData info)
        {
            // Time to create the text
            var ds = info.dt.StandardDimstyle();
            info.txtHeader = info.dt.AddText("Cut Name:", new Point3d(0, 0, 0), ds, 0.75, 0, 0, 6);
            info.txtBase = info.dt.AddText(info.cutName, new Point3d(0,-1,0), ds, 1.5, 0, 0, 0);

            BoundingBox bb = new BoundingBox();
                bb.Union(info.txtHeader.GetBoundingBox(true));
                bb.Union(info.txtBase.GetBoundingBox(true));
            Point3d[] p = bb.GetCorners();

            info.box = new Rectangle3d(Plane.WorldXY,
                new Point3d(p[0].X - 0.5, p[0].Y - 0.5, 0),
                new Point3d(p[2].X + 0.5, p[2].Y + 0.5, 0));

            Point3d cp;
            Point3d pp = info.box.Corner(1);
            if (info.objNestBox.Count > 0)
            {
                cp = info.objNestBox[0].Curve().GetBoundingBox(true).GetCorners()[2];
            }
            else
            {
                bb = new BoundingBox();
                foreach(var o in info.objCrv)
                    bb.Union(o.Geometry().GetBoundingBox(true));
                cp = bb.GetCorners()[2];
            }

            // Move the box to location
            Transform move = Transform.Translation(cp.X - pp.X, cp.Y - pp.Y, 0);
            info.box.Transform(move);
            info.txtBase.Transform(move);
            info.txtHeader.Transform(move);

            return info;
        }
    }
}