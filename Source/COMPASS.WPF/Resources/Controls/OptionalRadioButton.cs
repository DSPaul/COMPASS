using System.Windows.Controls;

namespace COMPASS.Resources.Controls
{
    public class OptionalRadioButton : RadioButton
    {
        protected override void OnClick()
        {
            bool? wasChecked = IsChecked;
            base.OnClick();
            if (wasChecked == true)
            {
                IsChecked = false;
            }
        }
    }
}
