using SFML.Graphics;
using SFML.System;
using SFML.Window;
using System.Diagnostics;
using Terraria.render;
using Terraria.physics;
using Terraria.utils;
using Terraria.world;
using Terraria.game;

namespace Terraria
{
    class GameWindow
    {
        public readonly RenderWindow SfmlWindow;
        public readonly View CameraView;
        public readonly Font Font;
        public readonly int WIDTH, HEIGHT;

        public GameWindow()
        {
            this.WIDTH = 1360;
            this.HEIGHT = 720;

            this.SfmlWindow = new RenderWindow(new VideoMode((uint)WIDTH, (uint)HEIGHT), "Terraria - from the penny store");
            this.CameraView = new(new FloatRect(0, 0, WIDTH, HEIGHT));
            SfmlWindow.SetFramerateLimit(120);

            Renderer.RegisterSprite(new Sprite(new Texture("assets/sprites/cat.jpg")));

            this.Font = new("assets/fonts/times new roman.ttf");
            Utils.mainWindow = this;

            RegisterEvents();
            Update();
        }

        private Chunk GetNextClosestChunk(Vector2f playerPos, List<Chunk> chunks)
        {
            List<Chunk> sortedChunks = new List<Chunk>(chunks);

            sortedChunks.Sort((a, b) =>
            {
                Vector2f aCenter = new Vector2f(
                    a.ChunkBounds.Position.X + a.ChunkBounds.Size.X / 2f,
                    a.ChunkBounds.Position.Y + a.ChunkBounds.Size.Y / 2f);
                Vector2f bCenter = new Vector2f(
                    b.ChunkBounds.Position.X + b.ChunkBounds.Size.X / 2f,
                    b.ChunkBounds.Position.Y + b.ChunkBounds.Size.Y / 2f);

                float aDistSq = (playerPos.X - aCenter.X) * (playerPos.X - aCenter.X) +
                                 (playerPos.Y - aCenter.Y) * (playerPos.Y - aCenter.Y);
                float bDistSq = (playerPos.X - bCenter.X) * (playerPos.X - bCenter.X) +
                                 (playerPos.Y - bCenter.Y) * (playerPos.Y - bCenter.Y);

                return aDistSq.CompareTo(bDistSq);
            });

            return sortedChunks[1];
        }

        private void Update()
        {
            PlayerCharacter player = new(new Vector2f(Constants.CHUNK_SIZE.X, Constants.CHUNK_SIZE.Y / 2.2f * 16)) { FillColor = Color.Blue};

            WorldGenerator world = new(0.02f, 20, 1773);
            Texture tileset = new Texture("assets/sprites/texture_atlas.png");
            List<Chunk> chunks = new List<Chunk>();
            List<VertexArray> terrainMeshes = new List<VertexArray>();
            for (int x = 0; x < 100; x++)
            {
                chunks.Add(world.GenerateCaves(world.GenerateNoise(x * Constants.CHUNK_SIZE.X), x * Constants.CHUNK_SIZE.X));
            }
            foreach (var chunk in chunks)
            {
                terrainMeshes.Insert(chunk.ID, world.GenerateTerrain(chunk, tileset, chunk.ChunkBounds.Left));
            }
            RenderStates states = new(tileset);

            Clock clock = new();
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            int frameCount = 0;
            double fps = 0.0;
            Text fpsText = new("120 FPS", Font);

            float speedMul = 3;
            Vector2f MousePos = new();

            Chunk currentChunk = chunks[2];
            List<Collider> currentChunkColliders = new List<Collider>();

            bool DebugMode = false;
            bool Freecam = false;

            EventManager.SubcribeToEvent(EventManager.EventType.KeyPressed, (e) =>
            {
                if (e.Data is KeyEventArgs keyEvent)
                {
                    if (keyEvent.Code == Keyboard.Key.F2)
                    {
                        DebugMode = !DebugMode;
                        Console.WriteLine($"Debug mode toggled to {DebugMode}");
                    }else if(keyEvent.Code == Keyboard.Key.P)
                    {
                        Freecam = !Freecam;
                        Console.WriteLine($"Freecam toggled to {Freecam}");
                    }
                }
            });

            while (SfmlWindow.IsOpen)
            {
                float deltaTime = clock.Restart().AsSeconds();
                MousePos = SfmlWindow.MapPixelToCoords(Mouse.GetPosition(SfmlWindow), CameraView);
                if(DebugMode && Freecam)
                {
                    CameraView.Center += (MousePos - CameraView.Center) * deltaTime;
                }
                else
                {
                    Vector2f desiredCamPos = player.Position + player.Size / 2;
                    Vector2f smoothedCamPos = Utils.LerpVector(CameraView.Center, desiredCamPos, 0.125f);
                    CameraView.Center = smoothedCamPos;
                }

                foreach (var chunk in chunks)
                {
                    if (chunk.ChunkBounds.Contains((Vector2i)player.Position))
                    {
                        currentChunk = chunk;
                        currentChunkColliders = currentChunk.GetMergedColliders();
                        currentChunkColliders.AddRange(GetNextClosestChunk(player.Position, chunks).GetMergedColliders());
                        break;
                    }
                }
                
                if (Keyboard.IsKeyPressed(Keyboard.Key.A))
                    player.Move(new Vector2f(-5000 * speedMul, 0), deltaTime);
                if (Keyboard.IsKeyPressed(Keyboard.Key.D))
                    player.Move(new Vector2f(5000 * speedMul, 0), deltaTime);
                if (Keyboard.IsKeyPressed(Keyboard.Key.W) && player.isGrounded)
                    player.Move(new Vector2f(0, -5000), 0.025f);
                if (Keyboard.IsKeyPressed(Keyboard.Key.E))
                    CameraView.Zoom(1.05f);
                if (Keyboard.IsKeyPressed(Keyboard.Key.Q))
                    CameraView.Zoom(0.9f);
                if (Keyboard.IsKeyPressed(Keyboard.Key.Escape))
                    SfmlWindow.Close();
                if (Keyboard.IsKeyPressed(Keyboard.Key.LShift))
                    player.Velocity = new Vector2f();

                player.Update(deltaTime, currentChunkColliders);

                SfmlWindow.SetView(CameraView);
                SfmlWindow.DispatchEvents();
                SfmlWindow.Clear(new Color(135, 206, 235, 255));
                Renderer.Render(SfmlWindow);

                for (int i = 0; i < terrainMeshes.Count; i++)
                {
                    if(DebugMode)
                    {
                        Chunk chunk = chunks[i];
                        RectangleShape ChunkOutline = new((Vector2f)chunk.ChunkBounds.Size) { OutlineThickness = 4, FillColor = Color.Transparent, OutlineColor = Color.Yellow, Position = (Vector2f)chunk.ChunkBounds.Position };
                        SfmlWindow.Draw(ChunkOutline);
                    }

                    if (!chunks[i].IsInView(CameraView))
                        continue;

                    SfmlWindow.Draw(terrainMeshes[i], states);
                }

                foreach (var collider in currentChunkColliders)
                {
                    if (DebugMode)
                        SfmlWindow.Draw(collider);
                }

                if(DebugMode)
                {
                    LineShape playerLookVector = new LineShape(player.Position + player.Size / 2, (player.Position + player.Size / 2) + player.Velocity, player.isGrounded ? Color.Blue : Color.Cyan);
                    LineShape playerRayDir = new LineShape(player.Position + player.Size / 2, (player.Position + player.Size / 2) + player.Velocity * deltaTime * 16, Color.Magenta);
                    SfmlWindow.Draw(playerLookVector.GetVertices(), PrimitiveType.Lines);
                    SfmlWindow.Draw(playerRayDir.GetVertices(), PrimitiveType.Lines);
                    SfmlWindow.Draw(new Text(player.Velocity.ToString(), Font) { Position = (player.Position + player.Size / 2) + player.Velocity, CharacterSize = 12, FillColor = Color.Black });
                }

                SfmlWindow.Draw(player);
                SfmlWindow.SetView(SfmlWindow.DefaultView);
                SfmlWindow.Draw(fpsText);

                SfmlWindow.Display();

                frameCount++;
                if (stopwatch.Elapsed.TotalSeconds >= 1.0)
                {
                    fps = frameCount / stopwatch.Elapsed.TotalSeconds;
                    fpsText.DisplayedString = $"FPS: {fps:F2} | DT: {deltaTime:F2}";
                    frameCount = 0;
                    stopwatch.Restart();
                }
            }
        }

        public void RegisterEvents()
        {
            SfmlWindow.Closed += (sender, e) =>
            {
                EventManager.CallEvent(EventManager.EventType.WindowClosed, e);
                SfmlWindow.Close();
            };
            SfmlWindow.Resized += (sender, e) =>
            {
                EventManager.CallEvent(EventManager.EventType.WindowResized, e);
                CameraView.Size = new Vector2f(e.Width, e.Height);
                CameraView.Center = new Vector2f(e.Width / 2, e.Height / 2);
            };
            SfmlWindow.GainedFocus += (sender, e) =>
            {
                EventManager.CallEvent(EventManager.EventType.WindowGainedFocus, e);
            };
            SfmlWindow.LostFocus += (sender, e) =>
            {
                EventManager.CallEvent(EventManager.EventType.WindowLostFocus, e);
            };
            SfmlWindow.MouseButtonPressed += (sender, e) =>
            {
                EventManager.CallEvent(EventManager.EventType.MouseButtonPressed, e);
            };
            SfmlWindow.MouseButtonReleased += (sender, e) =>
            {
                EventManager.CallEvent(EventManager.EventType.MouseButtonReleased, e);
            };
            SfmlWindow.MouseMoved += (sender, e) =>
            {
                EventManager.CallEvent(EventManager.EventType.MouseMoved, e);
            };
            SfmlWindow.MouseWheelScrolled += (sender, e) =>
            {
                EventManager.CallEvent(EventManager.EventType.MouseScrolled, e);
            };
            SfmlWindow.KeyPressed += (sender, e) =>
            {
                EventManager.CallEvent(EventManager.EventType.KeyPressed, e);
            };
            SfmlWindow.KeyReleased += (sender, e) =>
            {
                EventManager.CallEvent(EventManager.EventType.KeyReleased, e);
            };
            SfmlWindow.MouseEntered += (sender, e) =>
            {
                EventManager.CallEvent(EventManager.EventType.MouseEntered, e);
            };
            SfmlWindow.MouseLeft += (sender, e) =>
            {
                EventManager.CallEvent(EventManager.EventType.MouseLeft, e);
            };
        }
    }
}
