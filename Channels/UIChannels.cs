public partial struct Channel
{
    public static class UserInterface
    {
        public static readonly Channel Overlay = new Channel(1u << 12);
        public static readonly Channel Terminal = new Channel(1u << 13);
        public static readonly Channel Scenes = new Channel(1u << 14);
    }
}
