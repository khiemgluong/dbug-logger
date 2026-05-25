public partial struct Channel
{
    public static class Editor
    {
        public static readonly Channel Characters = new Channel(1u << 15);
        public static readonly Channel Environment = new Channel(1u << 16);
    }
}
