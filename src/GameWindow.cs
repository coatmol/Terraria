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
        private Texture TextureAtlas;

        public GameWindow()
        {
            this.WIDTH = 1360;
            this.HEIGHT = 720;

            this.SfmlWindow = new RenderWindow(new VideoMode((uint)WIDTH, (uint)HEIGHT), "Terraria - from the penny store");
            this.CameraView = new(new FloatRect(0, 0, WIDTH, HEIGHT));
            SfmlWindow.SetFramerateLimit(120);

            this.Font = new("assets/fonts/Andy Bold.ttf");
            Utils.mainWindow = this;

            TextureAtlas = Blocks.RegisterBlocks();
            TextureAtlas.Repeated = true;
            TextureAtlas.Smooth = false;

            RegisterEvents();
            Update();
        }

        private Chunk GetNextClosestChunk(Vector2f playerPos, List<Chunk> chunks)
        {
            List<Chunk> sortedChunks = new(chunks);

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
            int secondsSinceEpoch = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;

            WorldGenerator world = new(TextureAtlas, 0.02f, 20, secondsSinceEpoch);
            List<Chunk> chunks = [];
            List<VertexArray> terrainMeshes = [];
            VertexArray terrainWalls = world.GenerateBgWalls();

            for (int x = 0; x < Constants.WORLD_SIZE; x++)
            {
                chunks.Add(world.CalculateLight(world.GenerateCaves(world.GenerateNoise(x * Constants.CHUNK_SIZE.X), x * Constants.CHUNK_SIZE.X)));
            }
            foreach (var chunk in chunks)
            {
                terrainMeshes.Insert(chunk.ID, world.GenerateTerrain(chunk, chunk.ChunkBounds.Left));
            }
            RenderStates states = new(TextureAtlas);
            RenderStates tileStates = new(TextureAtlas);
            tileStates.Shader = world.tileShader;

            int SpawnPos = Constants.CHUNK_SIZE.X * Constants.BLOCK_SIZE * 25;
            PlayerCharacter player = new(new Vector2f(SpawnPos, world.GetHeight(SpawnPos) * Constants.BLOCK_SIZE - 24), world);

            EventManager.SubcribeToEvent(EventManager.EventType.TerrainUpdated, (e) =>
            {
                if (e.Data is int id)
                {
                    terrainMeshes[id] = world.Chunks[id].Vertices;
                }
            });

            Clock clock = new();
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            int frameCount = 0;
            double fps = 0.0;
            Text fpsText = new("120 FPS", Font);

            Vector2f MousePos = new();

            Chunk currentChunk = chunks.First();
            List<Collider> currentChunkColliders = new List<Collider>();

            bool DebugMode = false;
            bool Freecam = false;

            Sprite BlockSelection = new Sprite(new Texture("assets/sprites/block_select.png")) { Color = new Color(255, 255, 255, 100)};

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
                Vector2f? blockSelectionPos = (Vector2f?)world.Raycast(MousePos, player.Position, 6);
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
                    chunk.Update(world);

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
                if (Keyboard.IsKeyPressed(Keyboard.Key.E))
                    CameraView.Zoom(1.05f);
                if (Keyboard.IsKeyPressed(Keyboard.Key.Q))
                    CameraView.Zoom(0.9f);
                if (Keyboard.IsKeyPressed(Keyboard.Key.Escape))
                    SfmlWindow.Close();
                if (Keyboard.IsKeyPressed(Keyboard.Key.LShift))
                    player.Velocity = new Vector2f();
                if (Mouse.IsButtonPressed(Mouse.Button.Left) && DebugMode && Freecam)
                    player.Position = MousePos - player.Size / 2;
                else if (Mouse.IsButtonPressed(Mouse.Button.Left) && blockSelectionPos != null)
                    world.RemoveBlock((Vector2f)blockSelectionPos * Constants.BLOCK_SIZE);
                if (Mouse.IsButtonPressed(Mouse.Button.Right))
                    world.PlaceBlock(MousePos, Blocks.GetBlock("Torch"));

                player.Update(deltaTime, currentChunkColliders);

                SfmlWindow.SetView(CameraView);
                SfmlWindow.DispatchEvents();
                SfmlWindow.Clear(new Color(135, 206, 235, 255));
                Renderer.Render(SfmlWindow);
                SfmlWindow.Draw(terrainWalls, tileStates);

                for (int i = 0; i < terrainMeshes.Count; i++)
                {
                    if(DebugMode)
                    {
                        Chunk chunk = chunks[i];
                        RectangleShape ChunkOutline = new((Vector2f)chunk.ChunkBounds.Size) { OutlineThickness = 2, FillColor = Color.Transparent, OutlineColor = Color.Black, Position = (Vector2f)chunk.ChunkBounds.Position };
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

                if(blockSelectionPos != null)
                {
                    BlockSelection.Position = (Vector2f)(blockSelectionPos * Constants.BLOCK_SIZE);
                    SfmlWindow.Draw(BlockSelection);
                }

                if(DebugMode)
                {
                    LineShape playerLookVector = new LineShape(player.Position + player.Size / 2, (player.Position + player.Size / 2) + player.Velocity * 64, player.isGrounded ? Color.Blue : Color.Cyan);
                    LineShape playerRayDir = new LineShape(player.Position + player.Size / 2, (player.Position + player.Size / 2) + player.Velocity * deltaTime * 16, Color.Magenta);
                    LineShape playerMouseDir = new LineShape(player.Position + player.Size / 2, (blockSelectionPos != null ? (Vector2f)blockSelectionPos * Constants.BLOCK_SIZE : new Vector2f()), Color.Magenta);
                    playerLookVector.Draw(SfmlWindow);
                    playerRayDir.Draw(SfmlWindow);
                    playerMouseDir.Draw(SfmlWindow);
                    SfmlWindow.Draw(new Text(player.Velocity.X != 0 ? player.Velocity.ToString() : player.Position.ToString(), Font) { Position = (player.Position + player.Size / 2) + player.Velocity, CharacterSize = 12, FillColor = Color.Black });
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
