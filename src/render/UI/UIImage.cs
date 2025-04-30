using SFML.Graphics;
using Terraria.utils;

namespace Terraria.render.UI
{
    public class UIImage : UIComponent, Drawable
    {
        public required Sprite sprite;

        public UIImage(Sprite texture)
            : this(texture, new UDim2(), new UDim2(1,0,1,0))
        { }

        public UIImage(Sprite texture, UDim2 size)
            : this(texture, new UDim2(), size)
        { }

        public UIImage(string imagePath, UDim2 pos, UDim2 size)
            : base(pos, size)
        {
            this.sprite = new Sprite(new Texture(imagePath));
        }

        public UIImage(Sprite texture, UDim2 pos, UDim2 size)
            : base(pos, size)
        {
            this.sprite = texture;
        }

        public new void Draw(RenderTarget target, RenderStates states)
        {
            base.Update();
            target.Draw(sprite, states);
        }
    }
}
