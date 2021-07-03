using System.Collections.Generic;
using System.Linq;
using Rhino;


/// <summary>
/// Dialog creation and result return
/// </summary>
namespace gjTools
{

    class DialogTools
    {
        RhinoDoc m_doc;
        SQLTools sql;
        public DialogTools(RhinoDoc doc)
        {
            m_doc = doc;
            sql = new SQLTools();
        }


        public Rhino.Input.Custom.GetObject selectObjects(string message)
        {
            Rhino.Input.Custom.GetObject go = new Rhino.Input.Custom.GetObject();
            go.SetCommandPrompt(message);
            Rhino.Input.GetResult gr = go.GetMultiple(0, -1);
            if (gr != Rhino.Input.GetResult.Object)
            {
                return null;
            }
            return go;
        }
    }
}
