using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using Rhino.Input;
using Rhino.DocObjects;

namespace gjTools.Commands
{
    public class JTest : Command
    {
        public JTest()
        {
            Instance = this;
        }

        public static JTest Instance { get; private set; }
        public override string EnglishName => "asdf";
        
        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var layern = "";
            var res = RhinoGet.GetString("PN:", false, ref layern);
            LayerTools lt = new LayerTools(doc);
            lt.CreateLayer(layern + "-FLOOD-48IN");
            lt.CreateLayer(layern + "-FLOOD-54IN");
            return Result.Success;
        }
    }
}