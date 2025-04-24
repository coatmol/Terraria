using SFML.Graphics;
using SFML.System;

namespace Terraria.utils
{
    public class UIComponent : Transformable, Drawable
    {
        public UDim2 PosUDim = new UDim2();
        public UDim2 SizeUDim;
        public Vector2f Location {
            get { return PosUDim.Position; }
        }
        public Vector2f Size {
            get { return SizeUDim.Size; }
        }
        public float AspectRatio = 0.0f;

        public UIComponent(UDim2 size)
        {
            SizeUDim = size;
        }

        public UIComponent(UDim2 transform, UDim2 size)
        {
            PosUDim = transform;
            SizeUDim = size;
        }

        public void Draw(RenderTarget target, RenderStates states)
        {
            Update();
        }

        public void Update()
        {
            float width = Size.X;
            float height = Size.Y;

            if(AspectRatio != 0.0f)
            {
                if (AspectRatio > 1.0f)
                {
                    height = width / AspectRatio;
                }
                else
                {
                    width = height * AspectRatio;
                }
            }

            Position = Location;
            Scale = new Vector2f(width, height);
        }
    }
}
