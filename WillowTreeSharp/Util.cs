using System.Windows.Forms;

namespace WillowTree
{
    public static partial class Util
    {
        public static void SetNumericUpDown(NumericUpDown updown, decimal value)
        {
            if (value > updown.Maximum)
            {
                value = updown.Maximum;
            }
            else if (value < updown.Minimum)
            {
                value = updown.Minimum;
            }

            updown.Value = value;
        }
    }
}
