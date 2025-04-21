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

        // PHYSICS
        private const float MAX_SPEED = 1.5f;
        private const float ACCELERATION = 5.0f;
        private const float DECELERATION = 22.0f;
        private const float GRAVITY = 9.8f;
        private const float TERMINAL_VELOCITY = 15.0f;
        private const float JUMP_FORCE = -3.0f;

        // ANIMATION
        private readonly Vector2i FRAME_SIZE = new(16, 24);
        private const int FRAME_COUNT = 2;
        private float frameDuration = 0.15f;
        private float elapsedTime = 0;
        private float currentFrame = 0;

        public PlayerCharacter(Vector2f pos, WorldGenerator world) : base(pos, new Vector2f(16, 24), new Vector2f())
        {
            this.Texture = new Texture("assets/sprites/player.png");
            this.TextureRect = new IntRect(0, 0, 16, 24);
            this.world = world;

            EventManager.SubcribeToEvent(EventManager.EventType.KeyPressed, (e) =>
            {
                if (e.Data is KeyEventArgs keyEvent)
                {
                    if (keyEvent.Code == Keyboard.Key.Space && isGrounded && Utils.mainWindow.IsFocused)
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

        private void UpdateTextureRect()
        {
            int x = (int)(currentFrame * FRAME_SIZE.X);
            TextureRect = new IntRect(x, 0, FRAME_SIZE.X, FRAME_SIZE.Y);
            if(isFlipped)
                FlipTexture();
        }

        public void Update(float dt, List<Collider> colliders)
        {
            this.dt = dt;
            elapsedTime += dt;

            while (elapsedTime >= frameDuration)
            {
                if(Velocity.X != 0)
                {
                    elapsedTime -= frameDuration;
                    currentFrame = (currentFrame + 1) % FRAME_COUNT;
                    UpdateTextureRect();
                } else
                {
                    elapsedTime -= frameDuration;
                    currentFrame = 0;
                    UpdateTextureRect();
                }
            }

            if (Utils.mainWindow.IsFocused)
            {
                if (Keyboard.IsKeyPressed(Keyboard.Key.A))
                {
                    Velocity.X = Utils.Approach(Velocity.X, -MAX_SPEED, ACCELERATION * dt);
                    if (!isFlipped)
                    {
                        FlipTexture();
                        isFlipped = true;
                    }
                }
                if (Keyboard.IsKeyPressed(Keyboard.Key.D))
                {
                    Velocity.X = Utils.Approach(Velocity.X, MAX_SPEED, ACCELERATION * dt);
                    if (isFlipped)
                    {
                        FlipTexture();
                        isFlipped = false;
                    }
                }
                if (!Keyboard.IsKeyPressed(Keyboard.Key.A) && !Keyboard.IsKeyPressed(Keyboard.Key.D))
                {
                    if(isGrounded)
                        Velocity.X = Utils.Approach(Velocity.X, 0, DECELERATION * dt);
                }
            }
            Velocity.Y = Utils.Approach(Velocity.Y, TERMINAL_VELOCITY, GRAVITY * dt);

            Simulate(colliders);
        }
    }
}
