using System;
using System.Collections.Generic;
using System.Text;

namespace COMPASS.Common.Models.Enums
{
    public enum ChangeDataLocationActions
    {
        Cancel = 0,
        /// <summary>
        /// Moves the user data to the new location
        /// </summary>
        Move = 1,
        /// <summary>
        /// Copies the user data to the new location
        /// </summary>
        Copy = 2,
        /// <summary>
        /// Leaves the user data behind and use whatever is the new location
        /// </summary>
        Leave = 3,
        /// <summary>
        /// Delete existing data and use whatever is the new location
        /// </summary>
        Wipe = 4,
    }
}
