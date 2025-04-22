using SFML.Graphics;
using SFML.Window;

namespace Terraria.render.UI
{
    public class Input : Transformable, Drawable
    {
        public string Placeholder;
        public string Content = "";

        public Input(string placeholder)
        {
            Placeholder = placeholder;

            EventManager.SubcribeToEvent(EventManager.EventType.Typed, (e) =>
            {
                if (e.Data is KeyEventArgs)
                {
                    Content.Insert(Content.Length - 1, e.Data.ToString());
                }
            });
        }

        public void Draw(RenderTarget target, RenderStates states)
        {
        }
    }
}
