using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Geometry;
using Rhino.DocObjects;
using Rhino.Commands;
using Rhino.Input.Custom;

namespace gjTools.Commands
{
    public struct CutObj
    {
        public RhinoDoc doc;
        public Layer parentLayer;

        public string cutType;
        public string cutName;
        public string path;
        public DrawTools dt;
    }
    public struct CutData
    {
        public string cutType;
        public string cutName;
        public string layerName;
        public string path;
        public List<ObjRef> objCrv;
        public List<ObjRef> objNestBox;
        public List<ObjRef> objText;
        public RhinoDoc doc;

        public TextEntity txtHeader;
        public TextEntity txtBase;
        public Rectangle3d box;

        public DrawTools dt;
        public LayerTools lt;
        public SQLTools sql;

        public CutData(RhinoDoc document)
        {
            doc = document;
            
            cutType = "";
            cutName = "";
            layerName = "";
            path = "";
            objCrv = new List<ObjRef>();
            objNestBox = new List<ObjRef>();
            objText = new List<ObjRef>();

            txtHeader = new TextEntity();
            txtBase = new TextEntity();
            box = new Rectangle3d();

            dt = new DrawTools(doc);
            lt = new LayerTools(doc);
            sql = new SQLTools();
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

        public override string EnglishName => "gjCAD_CutFile";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            CutData info = new CutData(doc);

            // get locations from DB
            var paths = new List<string>();
            var locationNames = new List<string>();
            foreach(var line in info.sql.queryLocations())
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
            info.cutType = Rhino.UI.Dialogs.ShowListBox("Cut Location", "Choose a Location", locationNames) as string;
            if (info.cutType == null)
                return Result.Cancel;

            info.path = paths[locationNames.IndexOf(info.cutType)];

            // ask for the layer to send cut file out from
            info.layerName = Rhino.UI.Dialogs.ShowListBox("Layers", "Choose a Layer", info.lt.getAllParentLayersStrings()) as string;
            if (info.layerName == null)
                return Result.Cancel;

            // get Cut name, Default cut name will be gotten later if other checks have passed
            if (info.cutType == "Router")
            {
                info.cutName = GetStringPremade("Router Name", new List<string> { info.layerName }, 0);
                if (info.cutName == null)
                    return Result.Cancel;
                info.cutName = info.cutName + "-ROUTE";
                double num = 0;
                Rhino.Input.RhinoGet.GetNumber("Route Number", false, ref num);
                if (num > 0)
                    info.cutName = info.cutName + num;
            }
            else if (info.cutType == "WorkingLocation")
            {
                info.cutName = info.layerName;
            }
            else
            {
                var variableData = info.sql.queryVariableData()[0];
                info.cutName = variableData.userInitials + variableData.cutNumber;
            }

            // decide what kind of cut file to make
            doc.Objects.UnselectAll(true);

            var cutLayers = info.lt.getAllCutLayers(doc.Layers.FindName(info.layerName));

            // test for multiple nest boxes
            if (NestBoxCounter(doc, info.lt.CreateLayer(info.layerName)))
            {
                var go = new GetObject();
                    go.SetCommandPrompt("Multiple Nestings on one Layer Detected, Choose Objects for Cut File");
                    go.GeometryFilter = ObjectType.Curve | ObjectType.Annotation;
                    go.GetMultiple(1, 0);

                if (go.CommandResult() != Result.Success)
                    return Result.Cancel;

                doc.Objects.UnselectAll(true);
                doc.Views.Redraw();
                foreach (var o in go.Objects())
                {
                    if (info.lt.ObjLayer(o.ObjectId).Name.Substring(0, 2) == "C_" || info.lt.ObjLayer(o.ObjectId).Name == "NestBox")
                    {
                        if (info.lt.ObjLayer(o.ObjectId).Name == "NestBox")
                            info.objNestBox.Add(o);
                        else if (o.TextEntity() != null)
                            info.objText.Add(o);
                        else if (o.Curve() != null)
                            info.objCrv.Add(o);
                        doc.Objects.Select(o.ObjectId);
                    }
                }
            }
            else
            {
                if (cutLayers == null)
                {
                    RhinoApp.WriteLine("No Cut layers were found for " + info.cutName);
                    return Result.Cancel;
                }
                    

                // Select only the objects needed
                foreach (var cl in cutLayers)
                {
                    var selSett = new ObjectEnumeratorSettings();
                        selSett.LayerIndexFilter = cl.Index;
                        selSett.ObjectTypeFilter = ObjectType.Curve | ObjectType.Annotation;
                        selSett.NormalObjects = true;

                    foreach (var ob in doc.Objects.GetObjectList(selSett))
                    {
                        var o = new ObjRef(ob);
                        if (info.lt.ObjLayer(o.ObjectId).Name == "NestBox")
                            info.objNestBox.Add(o);
                        else if (o.TextEntity() != null)
                            info.objText.Add(o);
                        else if (o.Curve() != null)
                            info.objCrv.Add(o);
                        doc.Objects.Select(o.ObjectId);
                    }
                }
            }

            // Check that objects are all polylines
            if (info.dt.CheckPolylines(info.objCrv, true))
            {
                info.dt.hideDynamicDraw();
            }
            else
            {
                var msg = GetStringPremade("Some Bad Lines are Present, Would you like to Continue?", new List<string> { "Yes", "No" }, 1);
                info.dt.hideDynamicDraw();

                if (msg == null || msg == "No")
                    return Result.Cancel;
            }

            // all is well if it reaches here, cut file is being created
            info = MakeCutNameText(info);
            if (info.cutName.Length > 0)
                MakeDXF(info.path + info.cutName + ".dxf");

            // update the credential in the DB
            var creds = info.sql.queryVariableData()[0];
                creds.cutNumber++;
            info.sql.updateVariableData(creds);

            // add the items into the drawing
            List<Guid> newObjects = new List<Guid> {
                doc.Objects.AddRectangle(info.box), 
                doc.Objects.AddText(info.txtBase),
                doc.Objects.AddText(info.txtHeader)
            };
            doc.Views.Redraw();

            // Time to scale the box
            double scaleNumber = Math.Round((info.objNestBox[0].Curve().GetBoundingBox(true).GetEdges()[0].Length * 0.29) / info.box.Width, 2);
            var group = doc.Groups.Add();
            Rhino.Input.RhinoGet.GetNumber("Scale Factor For box", false, ref scaleNumber);
            Transform scale = Transform.Scale(info.box.Corner(1), scaleNumber);

            // apply changes
            foreach (var g in newObjects)
            {
                doc.Groups.AddToGroup(group, g);
                doc.Objects.Transform(g, scale, true);
                var obj = doc.Objects.FindId(g);
                obj.Attributes.LayerIndex = doc.Layers.FindName(info.layerName).Index;
                obj.CommitChanges();
            }

            doc.Views.Redraw();
            return Result.Success;
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
        /// Returns true if there is more than one entity on the NestBox Layer
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="parentLayer"></param>
        /// <returns></returns>
        public bool NestBoxCounter(RhinoDoc doc, Layer parentLayer)
        {
            int nestLayerIndex = doc.Layers.FindByFullPath(parentLayer.Name + "::NestBox", -1);
            if (nestLayerIndex == -1)
            {
                return false;
            } else
            {
                var selSet = new ObjectEnumeratorSettings();
                    selSet.LayerIndexFilter = nestLayerIndex;
                    selSet.ObjectTypeFilter = ObjectType.Curve;

                var nestCount = new List<RhinoObject>(doc.Objects.GetObjectList(selSet));
                if (nestCount.Count > 1)
                    return true;
            }
            return false;
        }

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