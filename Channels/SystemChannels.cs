public partial struct Channel
{
    public static class System
    {
        public static readonly Channel Serializers = new Channel(1u << 8);
        public static readonly Channel Managers = new Channel(1u << 9);
        public static readonly Channel Utilities = new Channel(1u << 10);
        public static readonly Channel Console = new Channel(1u << 11);
    }
}
