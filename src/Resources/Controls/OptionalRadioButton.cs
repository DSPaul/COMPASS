using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
