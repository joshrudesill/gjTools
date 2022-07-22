using System.Collections.Generic;
using System.IO;
using Rhino;
using Rhino.Input;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.DocObjects;

namespace gjTools.Testing
{
    public class TriangleWriter : Command
    {
        public TriangleWriter()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static TriangleWriter Instance { get; private set; }

        public override string EnglishName => "TriangleWriter";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            if (RhinoGet.GetMultipleObjects("Select Meshes", false, ObjectType.Mesh, out ObjRef[] oRefs) != Result.Success)
                return Result.Cancel;

            // file structure
            // int Partcount
            // {
            //    int ID
            //    int TriangleCount
            //    float[2] CenterPoint (x, y)
            //    Triangles[TriangleCount] (
            //      float[2] Vert A 
            //      float[2] Vert B 
            //      float[2] Vert C 
            //    )
            // }
            using (BinaryWriter binWriter =
              new BinaryWriter(File.Open("D:\\AutoNest_CPP\\AutoNest_IMGUI\\ShapeLibrary.shlib", FileMode.Create), System.Text.Encoding.UTF8))
            {
                // byte counter
                long byt = 0;
                
                int count = 0;

                //  write the qty of parts to read
                int partCount = oRefs.Length;
                binWriter.Write(partCount); Log(binWriter, ref byt, partCount);


                foreach (ObjRef o in oRefs)
                {
                    RhinoApp.WriteLine(" ------------------ Starting a mesh object");
                    var mesh = o.Mesh();

                    // write the id
                    binWriter.Write(count++); Log(binWriter, ref byt, count);

                    // write the amount of faces
                    int TriCount = mesh.GetNgonAndFacesCount();
                    binWriter.Write(TriCount);  Log(binWriter, ref byt, TriCount);

                    // write the center point
                    var cent = mesh.GetBoundingBox(true).Center;
                    float cenx = (float)cent.X;
                    float ceny = (float)cent.Y;
                    binWriter.Write(cenx);  Log(binWriter, ref byt, cenx);
                    binWriter.Write(ceny);  Log(binWriter, ref byt, ceny);

                    for (int i = 0; i < mesh.Faces.Count; i++)
                    {
                        mesh.Faces.GetFaceVertices(i, out Point3f a, out Point3f b, out Point3f c, out Point3f d);

                        // write the data without z information
                        binWriter.Write(a.X); Log(binWriter, ref byt, a.X);
                        binWriter.Write(a.Y); Log(binWriter, ref byt, a.Y);
                        binWriter.Write(b.X); Log(binWriter, ref byt, b.X);
                        binWriter.Write(b.Y); Log(binWriter, ref byt, b.Y);
                        binWriter.Write(c.X); Log(binWriter, ref byt, c.X);
                        binWriter.Write(c.Y); Log(binWriter, ref byt, c.Y);
                    }

                    RhinoApp.WriteLine($"Wrote Mesh {o.ObjectId}   - Total Bytes: {byt}");
                }
                binWriter.Close();
                binWriter.Dispose();
            }
            return Result.Success;
        }

        private void Log (BinaryWriter o, ref long currentcount, float value)
        {
            long newbytes = o.BaseStream.Length - currentcount;
            currentcount += newbytes;
            RhinoApp.WriteLine($"Wrote float Value: {value} - {newbytes} bytes");
        }

        private void Log(BinaryWriter o, ref long currentcount, int value)
        {
            long newbytes = o.BaseStream.Length - currentcount;
            currentcount += newbytes;
            RhinoApp.WriteLine($"Wrote int Value: {value} - {newbytes} bytes");
        }
    }
}