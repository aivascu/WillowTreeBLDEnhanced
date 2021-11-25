namespace WillowTreeSharp.Domain
{
    public class Item : WillowObject
    {
        public int Quantity
        {
            get => this.values[0x0];
            set => this.values[0x0] = value;
        }
    }
}