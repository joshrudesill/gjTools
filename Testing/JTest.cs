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
        enum testenum
        {
            t1 , t2,t3,t4
        }
        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            MDBox.deleteBox(doc);
            return Result.Success;
        }
    }
}