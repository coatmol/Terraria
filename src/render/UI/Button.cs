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
            this.text = new Text(text, Constants.MainFont) { FillColor = Color.Black };
            FixText();
            this.SetBorder(new System.Numerics.Vector4(4, 4, 4, 4));
        }

        public new void Draw(RenderTarget target, RenderStates states)
        {
            if(isDirty)
                FixText();
            base.Draw(target, states);
            target.Draw(text);
        }

        public void SetText(string text)
        {
            this.text = new Text(text, Constants.MainFont);
            this.text.Position = Position;
        }

        private void FixText()
        {
            text.Origin = text.GetGlobalBounds().Size / 2 + text.GetLocalBounds().Position;
            text.Position = Position + GetRect().Size / 2;
        }

        public void SetPosition(Vector2f pos)
        {
            Position = pos;
            this.text.Position = pos;
        }
    }
}
