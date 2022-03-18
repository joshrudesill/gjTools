using System.Collections.Generic;
using System;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.DocObjects;

namespace gjTools.Commands
{
    /// <summary>
    /// Keeps the grid values
    /// </summary>
    public class StraitBoxGrid
    {
        public int Rows = 1;
        public int Columns = 1;
        public double Spacing = 0.25;
        public readonly BoundingBox BB = BoundingBox.Empty;

        public StraitBoxGrid(List<ObjRef> oRefs)
        {
            foreach (ObjRef obj in oRefs)
                BB.Union(obj.Geometry().GetBoundingBox(true));
        }
    }

    /// <summary>
    /// translates Rhino groups to new ones only while this object is still alive
    /// </summary>
    public class StraightBoxGroupManager
    {
        private List<int> m_OrigGroups;
        private List<int> m_NewGroups;
        private RhinoDoc m_Doc;

        public StraightBoxGroupManager(RhinoDoc document)
        {
            m_OrigGroups = new List<int>();
            m_NewGroups = new List<int>();
            m_Doc = document;
        }

        public int TranslateGroup(int grp)
        {
            if (m_OrigGroups.Contains(grp))
            {
                return m_NewGroups[m_NewGroups.IndexOf(grp)];
            }
            else
            {
                int newgrp = m_Doc.Groups.Add();
                m_NewGroups.Add(newgrp);
                m_OrigGroups.Add(grp);
                return newgrp;
            }
        }
    }


    public class Nest_StraitBox_V2 : Command
    {
        public Nest_StraitBox_V2()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static Nest_StraitBox_V2 Instance { get; private set; }

        public override string EnglishName => "Nest_StraitBox_V2";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            if (RhinoGet.GetMultipleObjects("Select Objects", true, ObjectType.Curve, out ObjRef[] rGetObj) != Result.Success)
                return Result.Cancel;
            var rObj = new List<ObjRef>(rGetObj);

            var grid = new StraitBoxGrid(rObj);
            var gp = new StraitBoxDisplay(grid);

            if (gp.res == GetResult.Point)
                DuplicateObjects(doc, grid, rObj);

            return Result.Success;
        }

        private void DuplicateObjects(RhinoDoc doc, StraitBoxGrid grid, List<ObjRef> obj)
        {
            // get the curves
            var crv = new List<Curve>(obj.Count);

        }
    }


    /// <summary>
    /// Custom rhino getpoint that visially maintains the grid object
    /// </summary>
    public class StraitBoxDisplay : GetPoint
    {
        private StraitBoxGrid m_grid;
        private double m_partWidth;
        private double m_partHeight;
        private System.Drawing.Color m_clrBox = System.Drawing.Color.Aquamarine;

        /// <summary>
        /// The result of the command
        /// </summary>
        public GetResult res;

        /// <summary>
        /// starts the getpoint dialog
        /// </summary>
        /// <param name="grid"></param>
        public StraitBoxDisplay(StraitBoxGrid grid)
        {
            m_grid = grid;
            var edges = m_grid.BB.GetEdges();
            m_partWidth = edges[0].Length;
            m_partHeight = edges[1].Length;

            SetCommandPrompt($"Choose your GridNest Size (Spacing = {m_grid.Spacing})");
            AcceptNumber(true, true);

            while (true)
            {
                res = Get();

                if (res == GetResult.Cancel || res == GetResult.Nothing)
                    return;

                if (res == GetResult.Point)
                    break;

                if (res == GetResult.Number)
                {
                    m_grid.Spacing = Number();
                    SetCommandPrompt($"Choose your GridNest Size (Spacing = {m_grid.Spacing})");
                }
            }
        }

        protected override void OnDynamicDraw(GetPointDrawEventArgs e)
        {
            Point3d mp = e.CurrentPoint;
            BoundingBox bb = m_grid.BB;

            // get the drag data
            Point3d basePt = bb.Corner(true, true, true);
            double xlength = mp.X - basePt.X;
                   xlength *= (xlength > 0) ? 1 : -1;
            double ylength = mp.Y - basePt.Y;
                   ylength *= (ylength > 0) ? 1 : -1;

            // find out how many fit in the window
            m_grid.Columns = (int)(xlength / (m_partWidth + m_grid.Spacing));
            m_grid.Rows = (int)(ylength / (m_partHeight + m_grid.Spacing));

            // stop further calculation if the column and rows are 0 or 1
            if (m_grid.Columns <= 1 && m_grid.Rows <= 1)
            {
                e.Display.DrawBox(bb, m_clrBox);
                return;
            }

            // create the transforms
            var xForm = Transform.Translation(m_partWidth + m_grid.Spacing, 0, 0);
            var yForm = Transform.Translation(0, m_partHeight + m_grid.Spacing, 0);

            // keep an xbased original bb
            BoundingBox xbb = bb;

            // draw the grid
            for (int x = 0; x < m_grid.Columns; x++)
            {
                e.Display.DrawBox(bb, m_clrBox);

                for (int y = 0; y < m_grid.Rows - 1; y++)
                {
                    bb.Transform(yForm);
                    e.Display.DrawBox(bb, m_clrBox);
                }

                xbb.Transform(xForm);
                bb = xbb;
            }

            base.OnDynamicDraw(e);
        }
    }
}