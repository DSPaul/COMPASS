using System.Collections.Generic;

namespace COMPASS.Models
{

    /// <summary>
    /// Class that contains all the global preferences, ready to be serialized
    /// </summary>
    public class GlobalPreferences
    {
        public List<int> OpenFilePriorityIDs { get; set; }
    }
}
