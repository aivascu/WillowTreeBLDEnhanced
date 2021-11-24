using System;
using System.ComponentModel;
using WillowTree.CustomControls;

namespace WillowTree.Plugins
{
    [DesignTimeVisible(true)]
    public class StrictNumericUpDown : WTNumericUpDown
    {
        public StrictNumericUpDown()
        {
            this.ValueChanged += RestrictValueToBounds;
        }

        private void RestrictValueToBounds(object sender, EventArgs e)
        {
            if (this.Value > this.Maximum)
            {
                this.Value = this.Maximum;
            }

            if (this.Value < this.Minimum)
            {
                this.Value = this.Minimum;
            }
        }
    }
}
