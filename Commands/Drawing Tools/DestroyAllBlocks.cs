using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;

namespace gjTools.Commands.Drawing_Tools
{
    public class DestroyAllBlocks : Command
    {
        public DestroyAllBlocks()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static DestroyAllBlocks Instance { get; private set; }

        public override string EnglishName => "DestroyAllBlocks";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var blocks = new List<InstanceDefinition>(doc.InstanceDefinitions.GetList(true));
            foreach (var b in blocks)
            {
                var NestBlocks = new List<InstanceObject>(b.GetReferences(1));
                foreach (var nest in NestBlocks)
                    doc.Objects.AddExplodedInstancePieces(nest, true, true);

                doc.InstanceDefinitions.Delete(b);
            }

            RhinoApp.WriteLine("All Blocks are Decimated");
            return Result.Success;
        }
    }
}