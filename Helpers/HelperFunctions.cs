using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino;
using Rhino.Commands;

namespace gjTools
{
    interface IHelperFunctions
    {
        void showColorPallete();
    }

    class HelperFunctions : IHelperFunctions
    {
        public void showColorPallete()
        {
            SQLHelper sql = new SQLHelper();
        }
    }
}
