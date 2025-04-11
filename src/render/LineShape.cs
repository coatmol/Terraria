using SFML.Graphics;
using SFML.System;

namespace Terraria.render
{
    class LineShape
    {
        public Vector2f Start;
        public Vector2f End;
        public Color Color;
        public LineShape(Vector2f start, Vector2f end, Color color)
        {
            Start = start;
            End = end;
            Color = color;
        }

        public Vertex[] GetVertices()
        {
            return [new Vertex(Start, Color), new Vertex(End, Color)];
        }
    }
}
