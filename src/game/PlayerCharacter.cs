using SFML.System;
using Terraria.physics;
using Terraria.utils;

namespace Terraria.game
{
    class PlayerCharacter : RigidBody
    {
        private Vector2f contactPoint, contactNormal;
        private float contactTime;
        public bool isGrounded = false;
        private Vector2f Friction = new Vector2f(0.5f, 1);

        public PlayerCharacter(Vector2f pos) : base(pos, new Vector2f(16, 16), new Vector2f())
        {
        }

        public void Move(Vector2f direction, float dt)
        {
            Velocity += direction * dt;
        }

        public void Update(float dt, List<Collider> colliders)
        {
            bool groundCheck = false;

            Velocity += new Vector2f(0, 100) * dt * (isGrounded ? 0 : 1);
            Velocity = Utils.MultiplyVectors(Velocity, Friction);

            List<(int id, float t)> z = new List<(int, float)>();
            for (int i = 0; i < colliders.Count; i++)
            {
                Collider collider = colliders[i];
                if (CheckCollisionAgainstRect(collider))
                {
                    groundCheck = true;
                    if (!isGrounded)
                    {
                        Velocity.Y = 0;
                    }
                }
                if (DynamicallyCheckCollisionAgainstRect(collider, ref contactPoint, ref contactNormal, ref contactTime, dt))
                {
                    z.Add((i, contactTime));
                }
            }
            z.Sort((a, b) => a.t.CompareTo(b.t));
            foreach(var j in z)
            {
                if(DynamicallyCheckCollisionAgainstRect(colliders[j.id], ref contactPoint, ref contactNormal, ref contactTime, dt))
                {
                    if (contactNormal.Y == -1)
                        groundCheck = true;
                    if (contactTime < 0.0f)
                        Console.WriteLine("Inside collider");
                    else
                        Velocity += Utils.MultiplyVectors(contactNormal, new Vector2f(Math.Abs(Velocity.X), Math.Abs(Velocity.Y))) * (1 - contactTime);
                }
            }

            isGrounded = groundCheck;
            Position += Velocity * dt;
        }
    }
}
