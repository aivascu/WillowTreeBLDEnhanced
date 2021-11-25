namespace WillowTreeSharp.Domain
{
    public sealed class BankEntry : WillowObject
    {
        public byte TypeId { get; set; }

        public int Quantity
        {
            get => this.values[0x0];
            set => this.values[0x0] = value;
        }
    }
}