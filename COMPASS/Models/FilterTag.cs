using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static COMPASS.Tools.Enums;

namespace COMPASS.Models
{
    public class FilterTag:Tag
    {
        public FilterTag():base() 
        { }
        public FilterTag(ObservableCollection<FilterTag> alltags,MetaData mdtype)
        {
            int tempID = 0;
            while (alltags.Any(t => t.ID == tempID))
            {
                tempID++;
            }
            ID = tempID;
            MD = mdtype;
        }

        public MetaData MD;

        public override object GetGroup()
        {
            return MD;
        }

        public string GetFilterTerm()
        {
            return Content.Split(':')[1].Substring(1);
        }
    }
}
