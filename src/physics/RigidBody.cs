using SFML.Graphics;
using SFML.System;
using Terraria.utils;

namespace Terraria.physics
{
    public class RigidBody : Collider
    {
        public Vector2f Velocity;

        public RigidBody(Vector2f pos, Vector2f size, Vector2f vel) : base(pos, size)
        {
            Velocity = vel;
        }

        public bool DynamicallyCheckCollisionAgainstRect(RectangleShape target, ref Vector2f contactPoint, ref Vector2f contactNormal, ref float contactTime, float dt)
        {
            if (Math.Abs(Velocity.X) <= 0.01f && Math.Abs(Velocity.Y) <= 0.01f)
                return false;

            RectangleShape expandedTarget = new(target.Size + Size / 2);
            expandedTarget.Position = target.Position - Size / 2;

            Vector2f origin = Position + Size / 2;

            if (CheckCollisionAgainstRay(origin, Velocity * dt * ((Size.X + Size.Y) /8), expandedTarget, ref contactPoint, ref contactNormal, ref contactTime))
                return contactTime < 1.0f;
            else
                return false;

        }
    }
}
