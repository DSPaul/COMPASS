using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace COMPASS.Models
{
    public class UserSettings
    {
        public static Data CurrentData;

        public static void SetData(string Folder)
        {
            CurrentData = new Data(Folder);
        }
    }
}
