using SFML.Graphics;
using SFML.System;
using Terraria.utils;

namespace Terraria.render.UI
{
    class Button : ScalingImage, Drawable
    {
        private Text text;

        public Button(Texture texture, string text) : base(texture)
        {
            this.text = new Text(text, Constants.MainFont);
            this.text.Position = Position;
            this.SetBorder(new System.Numerics.Vector4(4, 4, 4, 4));
        }

        public new void Draw(RenderTarget target, RenderStates states)
        {
            base.Draw(target, states);
            target.Draw(text);
        }

        public void SetText(string text)
        {
            this.text = new Text(text, Constants.MainFont);
            this.text.Position = Position;
        }

        public void SetPosition(Vector2f pos)
        {
            Position = pos;
            this.text.Position = pos;
        }
    }
}
