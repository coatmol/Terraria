using SFML.Graphics;
using SFML.System;
using SFML.Window;
using Terraria.physics;
using Terraria.utils;
using Terraria.world;

namespace Terraria.game
{
    class PlayerCharacter : RigidBody
    {
        private float dt;
        public bool isGrounded = true;
        private bool isFlipped = false;
        private readonly WorldGenerator world;

        private const float MAX_SPEED = 1.5f;
        private const float ACCELERATION = 5.0f;
        private const float DECELERATION = 22.0f;
        private const float GRAVITY = 9.8f;
        private const float TERMINAL_VELOCITY = 15.0f; // Max fall speed
        private const float JUMP_FORCE = -3.0f;

        public PlayerCharacter(Vector2f pos, WorldGenerator world) : base(pos, new Vector2f(16, 24), new Vector2f())
        {
            this.Texture = new Texture("assets/sprites/character.png");
            this.world = world;

            EventManager.SubcribeToEvent(EventManager.EventType.KeyPressed, (e) =>
            {
                if (e.Data is KeyEventArgs keyEvent)
                {
                    if (keyEvent.Code == Keyboard.Key.Space && isGrounded)
                    {
                        Velocity.Y = JUMP_FORCE;
                        isGrounded = false;
                    }
                }
            });
        }

        private void FlipTexture()
        {
            IntRect texRect = TextureRect;

            texRect.Left = texRect.Left + texRect.Width;
            texRect.Width = -texRect.Width;

            TextureRect = texRect;
            isFlipped = !isFlipped;
        }

        private void Simulate(List<Collider> colliders)
        {
            Collider playerRect = new Collider(Position, Size);
            Vector2f remainder = new();

            remainder += Velocity;
            int moveX = (int)Math.Round(remainder.X);
            if(moveX != 0)
            {
                remainder.X -= moveX;
                int moveSign = Utils.Sign(moveX);

                while (moveX != 0)
                {
                    playerRect = new Collider(playerRect.Position + new Vector2f(moveSign, 0), (Vector2f)playerRect.Size);
                    foreach (var collider in colliders)
                    {
                        if (playerRect.CheckCollisionAgainstRect(collider))
                        {
                            Velocity.X = 0;
                            goto ExitX;
                        }
                    }

                    Position += new Vector2f(moveSign, 0);
                    moveX -= moveSign;
                }
            }
            ExitX: { }

            int moveY = (int)Math.Round(remainder.Y);
            if (moveY != 0)
            {
                remainder.Y -= moveY;
                int moveSign = Utils.Sign(moveY);

                while (moveY != 0)
                {
                    playerRect = new Collider(playerRect.Position + new Vector2f(0, moveSign), (Vector2f)playerRect.Size);
                    foreach (var collider in colliders)
                    {
                        if (playerRect.CheckCollisionAgainstRect(collider))
                        {
                            if(Velocity.Y > 0)
                            {
                                Velocity.Y = 0;
                                isGrounded = true;
                            }
                            goto ExitY;
                        }
                    }

                    Position += new Vector2f(0, moveSign);
                    moveY -= moveSign;
                }
            }
            ExitY: { }
        }


        public void Update(float dt, List<Collider> colliders)
        {
            this.dt = dt;
            if (Keyboard.IsKeyPressed(Keyboard.Key.A))
            {
                Velocity.X = Utils.Approach(Velocity.X, -MAX_SPEED, ACCELERATION * dt);
                if (!isFlipped)
                    FlipTexture();
            }
            if (Keyboard.IsKeyPressed(Keyboard.Key.D))
            {
                Velocity.X = Utils.Approach(Velocity.X, MAX_SPEED, ACCELERATION * dt);
                if (isFlipped)
                    FlipTexture();
            }
            if (!Keyboard.IsKeyPressed(Keyboard.Key.A) && !Keyboard.IsKeyPressed(Keyboard.Key.D))
            {
                if(isGrounded)
                    Velocity.X = Utils.Approach(Velocity.X, 0, DECELERATION * dt);
            }
            Velocity.Y = Utils.Approach(Velocity.Y, TERMINAL_VELOCITY, GRAVITY * dt);

            Simulate(colliders);
        }
    }
}
