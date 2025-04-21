using SFML.Graphics;
using SFML.System;

namespace Terraria.render
{
    static class UiRenderer
    {
        private static RenderWindow window;

        public static void Init(RenderWindow w)
        {
            if (window == null)
                window = w;
            else
                Console.WriteLine("WARNING: UI Renderer already initialized.");
        }

        public static bool Button(String text, Vector2f pos, String image="assets/ui/button.png")
        {


            return false;
        }
    }
}
