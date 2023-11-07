using BlackPearl.Controls.Contract;
using System;

namespace COMPASS.Tools
{
    //Enables multiselect combobox to create items
    public class CreatableLookUpContract : ILookUpContract
    {
        public bool SupportsNewObjectCreation => true;

        public object CreateObject(object sender, string searchString) => searchString.Trim();

        public bool IsItemEqualToString(object sender, object item, string seachString)
        {
            if (item is string str)
            {
                return String.Compare(seachString.ToLowerInvariant().Trim(), str.ToLowerInvariant().Trim(), StringComparison.InvariantCultureIgnoreCase) == 0;
            }

            return false;
        }

        public bool IsItemMatchingSearchString(object sender, object item, string searchString)
        {
            if (item is not string str)
            {
                return false;
            }
            return String.IsNullOrEmpty(searchString) || str.ToLower().Trim().Contains(searchString.ToLower().Trim());
        }
    }
}
