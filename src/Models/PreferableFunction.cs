using COMPASS.Tools;
using System;
using System.Windows.Media;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COMPASS.Models
{
    public class PreferableFunction<T>: Tag
    {
        //parameterless ctor for serialisation
        internal PreferableFunction(){}
        public PreferableFunction(string name, Func<T,bool> func, int id = -1)
        {
            Content = name;
            Function = func;
            ID = id;
            BackgroundColor = (Color)ColorConverter.ConvertFromString("#16D68A");
        }

        public Func<T,bool> Function { get; init; }
    }
}
