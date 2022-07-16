using BlackPearl.Controls.Contract;

namespace COMPASS.Tools
{
    //Enables multiselect combobox to create items
    public class CreatableLookUpContract : ILookUpContract
    {
        public bool SupportsNewObjectCreation => true;

        public object CreateObject(object sender, string searchString)
        {
            return searchString.Trim();
        }

        public bool IsItemEqualToString(object sender, object item, string seachString)
        {
            if (item is string)
                return string.Compare(seachString.ToLowerInvariant().Trim(), (item as string).ToLowerInvariant().Trim(), System.StringComparison.InvariantCultureIgnoreCase) == 0;
            else return false;
        }

        public bool IsItemMatchingSearchString(object sender, object item, string searchString)
        {
            if (item as string is null) return false;

            if (string.IsNullOrEmpty(searchString))
            {
                return true;
            }

            return ((string)item).ToLower().Trim().Contains(searchString.ToLower().Trim());
        }
    }
}
