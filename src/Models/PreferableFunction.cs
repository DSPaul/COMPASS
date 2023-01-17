using System;
using System.Windows.Media;

namespace COMPASS.Models
{
    public class PreferableFunction<T> : ITag, IHasID
    {
        //parameterless ctor for serialisation
        internal PreferableFunction() { }
        public PreferableFunction(string name, Func<T, bool> func, int id = -1)
        {
            Content = name;
            Function = func;
            ID = id;
            BackgroundColor = (Color)ColorConverter.ConvertFromString("#16D68A");
        }

        //Implement ITag
        public string Content { get; init; }
        public Color BackgroundColor { get; set; }
        //Implement IHasID
        public int ID { get; set; }

        //Properties
        public Func<T, bool> Function { get; init; }
    }
}
