using SFML.Graphics;
using SFML.System;
using SFML.Window;
using System.Numerics;
using Terraria.physics;
using Terraria.utils;

namespace Terraria.game
{
    class PlayerCharacter : RigidBody
    {
        private Vector2f contactPoint, contactNormal;
        private float contactTime;
        public bool isGrounded = false;
        private bool isFlipped = false;
        private Vector2f Friction = new Vector2f(0.5f, 1);

        public PlayerCharacter(Vector2f pos) : base(pos, new Vector2f(16, 24), new Vector2f())
        {
            this.Texture = new SFML.Graphics.Texture("assets/sprites/character.png");

            EventManager.SubcribeToEvent(EventManager.EventType.KeyPressed, (e) =>
            {
                if (e.Data is KeyEventArgs keyEvent)
                {
                    if (keyEvent.Code == Keyboard.Key.Space && isGrounded)
                    {
                        Move(new Vector2f(0, -5000), 0.025f);
                        isGrounded = false;
                    }
                }
            });
        }

        public void Move(Vector2f direction, float dt)
        {
            Velocity += direction * dt;
        }

        private void FlipTexture()
        {
            IntRect texRect = TextureRect;

            texRect.Left = texRect.Left + texRect.Width;
            texRect.Width = -texRect.Width;

            TextureRect = texRect;
            isFlipped = !isFlipped;
        }

        public void Update(float dt, List<Collider> colliders)
        {
            if (Keyboard.IsKeyPressed(Keyboard.Key.A))
            {
                Move(new Vector2f(-15000, 0), dt);
                if (!isFlipped)
                    FlipTexture();
            }
            if (Keyboard.IsKeyPressed(Keyboard.Key.D))
            {
                Move(new Vector2f(15000, 0), dt);
                if (isFlipped)
                    FlipTexture();
            }

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
