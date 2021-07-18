using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Input;
using Rhino.UI;

namespace gjTools.Commands
{
    public class ObjectsLayerChange : Command
    {
        public ObjectsLayerChange()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static ObjectsLayerChange Instance { get; private set; }

        public override string EnglishName => "ObjectsLayerChange";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // Get some objects
            var res = RhinoGet.GetMultipleObjects("Select Objects to Move to another Parent Layer", false, ObjectType.AnyObject, out ObjRef[] obj);
            if (res != Result.Success)
                return Result.Cancel;

            // ask for layer or to create a new
            var lt = new LayerTools(doc);
            var options = lt.getAllParentLayersStrings();
                options.Insert(0, "---Create New Layer---");
            // get the userinput
            var layName = (string)Dialogs.ShowListBox("Change Parent Layer", "Choose one", options);
            if (layName == null)
                return Result.Cancel;

            // See if we have to make a new
            Layer moveLay;
            if (layName == "---Create New Layer---")
                moveLay = DialogMakeNewLayer(lt);
            else
                moveLay = lt.CreateLayer(layName.ToUpper());

            if (moveLay == null)
                return Result.Cancel;

            // move objects to the new layer
            ChangeObjectParentLayer(doc, moveLay, obj);

            return Result.Success;
        }



        /// <summary>
        /// Ask user for name of a new parent layer in the Commandline
        /// <para>Default Suggest the next open default name</para>
        /// </summary>
        /// <param name="lt"></param>
        /// <returns>The new Parent Layer</returns>
        public Layer DialogMakeNewLayer(LayerTools lt)
        {
            var name = lt.doc.Layers.GetUnusedLayerName();
            var res = RhinoGet.GetString("New Layer Name", false, ref name);
            if (res != Result.Success)
                return null;

            return lt.CreateLayer(name.ToUpper());
        }

        /// <summary>
        /// Shift objects to another parent layer while maintaining the layer structure it came from
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="parentLay"></param>
        /// <param name="obj"></param>
        public void ChangeObjectParentLayer(RhinoDoc doc, Layer parentLay, ObjRef[] obj)
        {
            ChangeObjectParentLayer(doc, parentLay, new List<ObjRef>(obj));
        }
        /// <summary>
        /// Shift objects to another parent layer while maintaining the layer structure it came from
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="parentLay"></param>
        /// <param name="obj"></param>
        public void ChangeObjectParentLayer(RhinoDoc doc, Layer parentLay, List<ObjRef> obj)
        {
            var oldChildLayersIndex = new List<int>();
            var oldChildName = new List<string>();
            var newChildLayersIndex = new List<int>();
            foreach(var o in obj)
            {
                var roObj = o.Object();
                var lindex = roObj.Attributes.LayerIndex;
                var lname = doc.Layers[lindex].Name;

                if ((!oldChildLayersIndex.Contains(lindex) || !oldChildName.Contains(lname)) && doc.Layers[lindex].ParentLayerId != Guid.Empty)
                {
                    // layer not yet created
                    oldChildLayersIndex.Add(lindex);
                    oldChildName.Add(doc.Layers[lindex].Name);
                    var chLay = new Layer
                    {
                        Name = lname,
                        ParentLayerId = parentLay.Id,
                        Color = doc.Layers[lindex].Color
                    };

                    // add to the layer table
                    newChildLayersIndex.Add(doc.Layers.Add(chLay));
                }

                // move the object to the parent layer
                // check if it's not on a child layer
                if (doc.Layers[lindex].ParentLayerId == Guid.Empty)
                    roObj.Attributes.LayerIndex = parentLay.Index;
                else
                {
                    // check against the new layer list
                    roObj.Attributes.LayerIndex = newChildLayersIndex[oldChildLayersIndex.IndexOf(roObj.Attributes.LayerIndex)];
                }

                // commit the objects changes
                roObj.CommitChanges();
            }
        }
    }
}