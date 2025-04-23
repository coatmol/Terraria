using SFML.Graphics;
using SFML.System;

namespace Terraria.utils
{
    public static class Constants
    {
        public static readonly Font MainFont = new Font("assets/fonts/minecraft-dungeons.ttf");

        public const int BLOCK_SIZE = 16;
        public const int WORLD_SIZE = 50;
        public static Vector2i CHUNK_SIZE = new(32, 512);
        public const int MAX_LIGHT_LEVEL = 10;
    }
}
