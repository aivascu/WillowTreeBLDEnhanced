namespace WillowTreeSharp.Domain
{
    public class Weapon : WillowObject
    {
        public int Ammo
        {
            get => this.values[0];
            set => this.values[0] = value;
        }
    }
}