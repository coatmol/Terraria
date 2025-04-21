using SFML.Graphics;
using SFML.System;
using SFML.Window;
using Terraria.render.UI;
using Terraria.utils;

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

        public static bool Button(Button button)
        {
            window.Draw(button);

            if(button.GetRect().Contains(Utils.GetLocalMousePos()) && Mouse.IsButtonPressed(Mouse.Button.Left))
                return true;

            return false;
        }
    }
}
