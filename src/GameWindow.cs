using SFML.Graphics;
using SFML.System;
using SFML.Window;
using System.Diagnostics;
using System.Linq.Expressions;
using Terraria.world;

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

            SfmlWindow.Closed += (sender, e) => SfmlWindow.Close();

            Update();
        }

        private void Update()
        {
            int ChunkWidth = 16;
            int ChunkHeight = 512;

            WorldGenerator world = new(ChunkWidth, ChunkHeight, 0.02f, 20, 1773);
            Texture tileset = new Texture("assets/sprites/test.png");
            List<Chunk> terrain = new List<Chunk>();
            List<VertexArray> terrainMeshes = new List<VertexArray>();
            for (int x = 0; x < 100; x++)
            {
                terrain.Add(world.GenerateCaves(world.GenerateNoise(x * ChunkWidth), x * ChunkWidth));
            }
            foreach (var chunk in terrain)
            {
                terrainMeshes.Insert(chunk.ID, world.GenerateTerrain(chunk, tileset, chunk.ChunkBounds.Left));
            }
            RenderStates states = new(tileset);

            Clock clock = new();
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            int frameCount = 0;
            double fps = 0.0;
            float speedMul = 1;
            Text fpsText = new("120 FPS", Font);

            while (SfmlWindow.IsOpen)
            {
                float deltaTime = clock.Restart().AsSeconds();

                if (Keyboard.IsKeyPressed(Keyboard.Key.A))
                    CameraView.Move(new Vector2f(-500 * deltaTime * speedMul, 0));
                if (Keyboard.IsKeyPressed(Keyboard.Key.D))
                    CameraView.Move(new Vector2f(500 * deltaTime * speedMul, 0));
                if (Keyboard.IsKeyPressed(Keyboard.Key.W))
                    CameraView.Move(new Vector2f(0, -500 * deltaTime * speedMul));
                if (Keyboard.IsKeyPressed(Keyboard.Key.S))
                    CameraView.Move(new Vector2f(0, 500 * deltaTime * speedMul));
                if (Keyboard.IsKeyPressed(Keyboard.Key.E))
                    CameraView.Zoom(1.05f);
                if (Keyboard.IsKeyPressed(Keyboard.Key.Q))
                    CameraView.Zoom(0.9f);
                if (Keyboard.IsKeyPressed(Keyboard.Key.Escape))
                    SfmlWindow.Close();
                if (Keyboard.IsKeyPressed(Keyboard.Key.LShift))
                    speedMul = 3;

                SfmlWindow.SetView(CameraView);
                SfmlWindow.DispatchEvents();
                SfmlWindow.Clear(new Color(135, 206, 235, 255));
                Renderer.Render(SfmlWindow);
                for (int i = 0; i < terrainMeshes.Count; i++)
                {
                    if(Keyboard.IsKeyPressed(Keyboard.Key.Y))
                    {
                        Chunk chunk = terrain[i];
                        RectangleShape ChunkOutline = new((Vector2f)chunk.ChunkBounds.Size);
                        ChunkOutline.Position = (Vector2f)chunk.ChunkBounds.Position;
                        ChunkOutline.OutlineThickness = 10;
                        ChunkOutline.FillColor = Color.Transparent;
                        ChunkOutline.OutlineColor = Color.Yellow;
                        SfmlWindow.Draw(ChunkOutline);
                    }

                    if (!terrain[i].IsInView(CameraView))
                        continue;

                    SfmlWindow.Draw(terrainMeshes[i], states);
                }
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
    }
}
