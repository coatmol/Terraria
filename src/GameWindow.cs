﻿using SFML.Graphics;
using SFML.System;
using SFML.Window;
using System.Diagnostics;
using Terraria.render;
using Terraria.physics;
using Terraria.utils;
using Terraria.world;
using Terraria.game;
using System.Numerics;
using Terraria.render.UI;
using ProtoBuf.Meta;

namespace Terraria
{
    class GameWindow
    {
        public readonly RenderWindow SfmlWindow;
        public readonly View CameraView;
        public readonly int WIDTH, HEIGHT;
        private Texture TextureAtlas;
        public bool IsFocused = true;

        public GameWindow()
        {
            this.WIDTH = 1360;
            this.HEIGHT = 720;

            this.SfmlWindow = new RenderWindow(new VideoMode((uint)WIDTH, (uint)HEIGHT), "Terraria - from the penny store");
            this.CameraView = new(new FloatRect(0, 0, WIDTH, HEIGHT));
            SfmlWindow.SetFramerateLimit(120);

            Utils.mainWindow = this;
            Constants.MainFont.SetSmooth(false);

            TextureAtlas = Blocks.RegisterBlocks();

            var serializationModel = RuntimeTypeModel.Default;
            serializationModel.Add(typeof(IntRect), false).SetSurrogate(typeof(IntRectSurrogate));

            UiRenderer.Init(SfmlWindow);
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
            List<Chunk> chunks = new List<Chunk>();
            VertexArray terrainWalls = world.GenerateBgWalls();

            int blockId = 0;

            for (int x = 0; x < Constants.WORLD_SIZE; x++)
            {
                chunks.Add(world.GenerateLightmap(world.GenerateCaves(world.GenerateNoise(x * Constants.CHUNK_SIZE.X), x * Constants.CHUNK_SIZE.X)));
                world.GenerateTerrain(chunks[x], chunks[x].ChunkBounds.Left);
            }

            RenderStates states = new(TextureAtlas);
            RenderStates tileStates = new(TextureAtlas) { Shader = world.tileShader };

            int SpawnPos = Constants.CHUNK_SIZE.X * Constants.BLOCK_SIZE * 25;
            PlayerCharacter player = new(new Vector2f(SpawnPos, world.GetHeight(SpawnPos) * Constants.BLOCK_SIZE - 24), world);

            Clock clock = new();
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            int frameCount = 0;
            double fps = 0.0;
            Text fpsText = new("120 FPS", Constants.MainFont);

            Vector2f MousePos = new();

            Chunk? currentChunk = null;
            List<Collider> currentChunkColliders = new List<Collider>();

            bool DebugMode = false;
            bool Freecam = false;
            bool SmartBreak = false;

            Sprite BlockSelection = new Sprite(new Texture("assets/sprites/block_select.png") { Repeated = true, Smooth = false }) { Color = new Color(255, 255, 255, 100)};
            Input CommandInput = new Input("Type a command here.", new UDim2(0, 1-0.05f, 0, 0), new UDim2(1, 0.05f, 0, 0));
            CommandInput.FillColor = new Color(0, 0, 0, 150);
            CommandInput.IsDisabled = true;

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
                    }else if(keyEvent.Code == Keyboard.Key.Slash)
                    {
                        CommandInput.IsFocused = true;
                        CommandInput.IsDisabled = !CommandInput.IsDisabled;
                    }
                    else if(keyEvent.Code == Keyboard.Key.Escape)
                    {
                        if (!CommandInput.IsDisabled)
                        {
                            CommandInput.IsDisabled = true;
                        }else
                        {
                            SfmlWindow.Close();
                        }
                    }
                }
            });

            EventManager.SubcribeToEvent(EventManager.EventType.CommandExecuted, (e) =>
            {
                if (e.Data is CommandEventArgs cmdEvent)
                {
                    if (cmdEvent.command == "place" && cmdEvent.args.Length > 0 && int.TryParse(cmdEvent.args[0], out int parsedBlockId))
                    {
                        blockId = parsedBlockId + 1;
                    }
                    else
                    {
                        Console.WriteLine("Invalid block ID provided.");
                    }
                }
            });

            while (SfmlWindow.IsOpen)
            {
                float deltaTime = clock.Restart().AsSeconds();
                MousePos = SfmlWindow.MapPixelToCoords(Mouse.GetPosition(SfmlWindow), CameraView);

                #region In-World
                Vector2f? blockSelectionPos = (Vector2f?)world.Raycast(MousePos, player.Position, 6);
                BlockSelection.Color = SmartBreak ? new Color(239, 217, 54, 100) : new Color(255, 255, 255, 25);
                if (DebugMode && Freecam)
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
                #region Input
                if (IsFocused && !CommandInput.IsFocused)
                {
                    SmartBreak = Keyboard.IsKeyPressed(Keyboard.Key.LControl);

                    if (Keyboard.IsKeyPressed(Keyboard.Key.E))
                        CameraView.Zoom(1.05f);
                    if (Keyboard.IsKeyPressed(Keyboard.Key.Q))
                        CameraView.Zoom(0.9f);

                    if (Keyboard.IsKeyPressed(Keyboard.Key.LShift))
                            player.Velocity = new Vector2f();

                    if (Mouse.IsButtonPressed(Mouse.Button.Left) && DebugMode && Freecam)
                        player.Position = MousePos - player.Size / 2;

                    else if (Mouse.IsButtonPressed(Mouse.Button.Left) && blockSelectionPos != null)
                        if (SmartBreak || blockSelectionPos == new Vector2f(
                                (float)Math.Round(MousePos.X / Constants.BLOCK_SIZE),
                                (float)Math.Round(MousePos.Y / Constants.BLOCK_SIZE)))
                            world.RemoveBlock((Vector2f)blockSelectionPos * Constants.BLOCK_SIZE);

                    if (Mouse.IsButtonPressed(Mouse.Button.Right))
                            world.PlaceBlock(MousePos, Blocks.GetBlock(blockId));
                    if (Keyboard.IsKeyPressed(Keyboard.Key.LControl) && Keyboard.IsKeyPressed(Keyboard.Key.S))
                        world.SaveToFile("world.wld");
                }
                #endregion

                player.Update(deltaTime, currentChunkColliders);

                SfmlWindow.SetView(CameraView);
                SfmlWindow.DispatchEvents();
                SfmlWindow.Clear(new Color(135, 206, 235, 255));
                Renderer.Render(SfmlWindow);
                SfmlWindow.Draw(terrainWalls, tileStates);

                for (int i = 0; i < Constants.WORLD_SIZE; i++)
                {
                    if(chunks.Count == 0) continue;
                    Chunk chunk = chunks[i];
                    if(DebugMode)
                    {
                        RectangleShape ChunkOutline = new((Vector2f)chunk.ChunkBounds.Size) { OutlineThickness = 2, FillColor = Color.Transparent, OutlineColor = Color.Black, Position = (Vector2f)chunk.ChunkBounds.Position };
                        SfmlWindow.Draw(ChunkOutline);
                    }

                    if (!chunk.IsInView(CameraView))
                        continue;

                    SfmlWindow.Draw(chunk.Vertices, states);
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
                    SfmlWindow.Draw(new Text(player.Velocity.X != 0 ? player.Velocity.ToString() : player.Position.ToString(), Constants.MainFont) { Position = (player.Position + player.Size / 2) + player.Velocity, CharacterSize = 12, FillColor = Color.Black });
                }


                SfmlWindow.Draw(player);

                #endregion
                #region UI
                SfmlWindow.SetView(SfmlWindow.DefaultView);
                SfmlWindow.Draw(fpsText);

                if (UiRenderer.Input(CommandInput))
                {
                    Commands.ExecuteCommand(CommandInput.Content, player);
                    CommandInput.Content = "";
                    CommandInput.IsDisabled = true;
                }

                #endregion

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
                IsFocused = true;
                EventManager.CallEvent(EventManager.EventType.WindowGainedFocus, e);
            };
            SfmlWindow.LostFocus += (sender, e) =>
            {
                IsFocused = false;
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
            SfmlWindow.TextEntered += (sender, e) =>
            {
                EventManager.CallEvent(EventManager.EventType.Typed, e);
            };
        }
    }
}
