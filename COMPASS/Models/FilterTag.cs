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
        public FilterTag(ObservableCollection<Tag> alltags,MetaData mdtype) : base(alltags)
        {
            MD = mdtype;
        }

        public MetaData MD; 
    }
}
