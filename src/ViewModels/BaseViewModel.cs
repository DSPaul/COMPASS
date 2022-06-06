using COMPASS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COMPASS.ViewModels
{
    public abstract class BaseViewModel:ObservableObject
    {
        //So every viewmodel has access to all the others
        public static MainViewModel MVM { get; set; }
    }
}
