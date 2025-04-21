// 9-Slice scaling image

using SFML.Graphics;
using SFML.System;
using System.Numerics;

namespace Terraria.render
{
    public class ScalingImage : Transformable, Drawable
    {
        protected Texture texture;
        protected uint left, top, right, bottom, width, height;
        protected VertexArray vertices;
        protected bool isDirty = true;
        public Color color = Color.White;

        public ScalingImage(Texture texture)
        {
            this.texture = texture;
            this.right = texture.Size.X;
            this.bottom = texture.Size.Y;
            this.width = texture.Size.X;
            this.height = texture.Size.Y;
            this.vertices = new VertexArray(PrimitiveType.TriangleStrip, 24);
        }

        public void Draw(RenderTarget target, RenderStates states)
        {
            if(isDirty)
            {
                UpdateVertices();
                isDirty = false;
            }
            if(texture != null)
            {
                states.Transform *= Transform;
                states.Texture = texture;
                target.Draw(vertices, states);
            }
        }

        public Vector2u GetSize()
        {
            return new Vector2u(width, height);
        }

        public FloatRect GetRect()
        {
            return new FloatRect(Position, new(width * Scale.X, height * Scale.Y));
        }

        public void SetSize(Vector2u size)
        {
            width = size.X;
            height = size.Y;
            isDirty = true;
        }

        public void SetWidth(uint w)
        {
            width = w;
            isDirty = true;
        }

        public void SetHeight(uint h)
        {
            height = h;
            isDirty = true;
        }

        public void SetTexture(Texture t)
        {
            texture = t;
            isDirty = true;
        }

        public Texture GetTexture() { return texture; }

        public void SetLeftBorder(uint border)
        {
            left = border;
            isDirty = true;
        }

        public void SetTopBorder(uint border)
        {
            top = border;
            isDirty = true;
        }

        public void SetBottomBorder(uint border)
        {
            bottom = border;
            isDirty = true;
        }

        public void SetRightBorder(uint border)
        {
            right = border;
            isDirty = true;
        }

        public void SetBorder(Vector4 border)
        {
            left = (uint)border.X;
            top = (uint)border.Y;
            right = (uint)border.Z;
            bottom = (uint)border.W;
        }

        protected void UpdateVertices()
        {
            uint[] triangleStripVertexOrder = {0, 4, 1, 5, 2, 6, 3, 7, 7, 11, 6, 10, 5, 9, 4, 8, 8, 12, 9, 13, 10, 14, 11, 15};

            if(texture != null)
            {
                uint[] xPos = { 0, left, width - right, width };
                uint[] yPos = { 0, top, height - bottom, height };
                Vector2f[] vertexPositions = new Vector2f[16];

                uint tWidth = texture.Size.X;
                uint tHeight = texture.Size.Y;

                uint[] xTexCoords = { 0, left, tWidth - right, tWidth };
                uint[] yTexCoords = { 0, top, tHeight - bottom, tHeight };
                Vector2f[] vertexTexCoords = new Vector2f[16];

                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        vertexPositions[i + 4 * j] = new Vector2f(xPos[i], yPos[j]);
                        vertexTexCoords[i + 4 * j] = new Vector2f(xTexCoords[i], yTexCoords[j]);
                    }
                }

                for (uint i = 0; i < 24; i++)
                {
                    vertices[i] = new Vertex(vertexPositions[triangleStripVertexOrder[i]], color, vertexTexCoords[triangleStripVertexOrder[i]]);
                }
            }
        }
    }
}
