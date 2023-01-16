using System.Windows.Controls;

namespace COMPASS.Resources.Controls
{
    public class OptionalRadioButton : RadioButton
    {
        protected override void OnClick()
        {
            bool? wasChecked = this.IsChecked;
            base.OnClick();
            if (wasChecked == true)
                this.IsChecked = false;
        }
    }
}
