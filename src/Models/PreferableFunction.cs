using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace COMPASS.Models
{
    /// <summary>
    /// Wrapper for Func with param T and bool return that should be tried in a set order until one succeeds
    /// which is indicated by the bool return value
    /// </summary>
    /// <typeparam name="T"> Type of argument of the function</typeparam>
    public class PreferableFunction<T> : ITag, IHasID
    {
        //parameterless ctor for serialisation
        internal PreferableFunction() { }
        public PreferableFunction(string name, Func<T, bool> func, int id = -1)
        {
            Content = name;
            Function = func;
            ID = id;
            BackgroundColor = (Color)ColorConverter.ConvertFromString("#16D68A")!;
        }

        //Implement ITag
        public string Content { get; init; }
        public Color BackgroundColor { get; set; }
        //Implement IHasID
        public int ID { get; set; }

        //Properties
        public Func<T, bool> Function { get; }

        //Try functions in order determined by list of preferable functions until one succeeds
        public static bool TryFunctions<A>(IEnumerable<PreferableFunction<A>> toTry, A arg)
        {
            bool success = false;
            foreach (var func in toTry)
            {
                success = func.Function(arg);
                if (success) break;
            }
            return success;
        }
    }
}
