public partial struct Channel
{
    public static class Environment
    {
        public static readonly Channel Prop = new Channel(1u << 4);
        public static readonly Channel Object = new Channel(1u << 5);
        public static readonly Channel ObjItems = new Channel(1u << 6);
        public static readonly Channel Events = new Channel(1u << 7);
    }
}
