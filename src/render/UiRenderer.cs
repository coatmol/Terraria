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
        public static Dictionary<string, Texture> UIAssets = new Dictionary<string, Texture>() {
            {"ButtonUp", new Texture("assets/ui/Button.png") },
            { "ButtonDown", new Texture("assets/ui/ButtonDown.png") },
            { "ButtonSelected", new Texture("assets/ui/ButtonSelected.png") }
        };

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

        public static bool Input(Input input)
        {
            window.Draw(input);

            return input.Update();
        }
    }
}
