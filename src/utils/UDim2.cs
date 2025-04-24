using System;
using SFML.Graphics;
using SFML.System;

namespace Terraria.utils
{
    public class UDim2
    {
        public FloatRect? Parent;
        public Vector2f Scale; // normalized according to screen size
        public Vector2f Offset;

        public Vector2f Position
        {
            get
            {
                return Utils.MultiplyVectors(Scale, Parent.HasValue ? Parent.Value.Position : (Vector2f)Utils.mainWindow.SfmlWindow.Size) + Offset;
            }
        }

        public Vector2f Size
        {
            get
            {
                return Utils.MultiplyVectors(Scale, Parent.HasValue ? Parent.Value.Size : (Vector2f)Utils.mainWindow.SfmlWindow.Size) + Offset;
            }
        }

        public UDim2()
        {
            this.Scale = new Vector2f();
            this.Offset = new Vector2f();
        }

        public UDim2(Vector2f scale, Vector2f offset)
        {
            this.Scale = scale;
            this.Offset = offset;
        }

        public UDim2(float scaleX, float scaleY, float offsetX, float offsetY)
        {
            this.Scale = new Vector2f(scaleX, scaleY);
            this.Offset = new Vector2f(offsetX, offsetY);
        }
    }
}
