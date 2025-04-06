using SFML.Graphics;

namespace Terraria
{
    static class Renderer
    {
        private static List<Sprite> SPRITES = new List<Sprite>();

        public static void Render(RenderWindow window)
        {
            foreach (var sprite in SPRITES)
            {
                window.Draw(sprite);
            }
        }

        public static void RegisterSprite(Sprite sprite)
        {
            SPRITES.Add(sprite);
        }
    }
}
