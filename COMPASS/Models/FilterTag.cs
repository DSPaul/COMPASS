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
        public FilterTag(ObservableCollection<FilterTag> alltags,FilterType filtertype, object filterValue = null)
        {
            int tempID = 0;
            while (alltags.Any(t => t.ID == tempID))
            {
                tempID++;
            }
            ID = tempID;
            FT = filtertype;
            FilterValue = filterValue;
        }

        readonly FilterType FT;
        public object FilterValue;

        public override object GetGroup()
        {
            return FT;
        }
    }
}
