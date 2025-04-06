using SFML.Graphics;
using SFML.System;

namespace Terraria.world
{

    public class Chunk
    {
        public int[,] TerrainVertices;
        public readonly IntRect ChunkBounds;
        public int ID;

        public Chunk(int[,] terrainVertices, IntRect chunkBounds)
        {
            TerrainVertices = terrainVertices;
            ChunkBounds = chunkBounds;
        }

        public bool IsInView(View cameraView)
        {
            int left = (int)(cameraView.Center.X - cameraView.Size.X / 2);
            int top = (int)(cameraView.Center.Y - cameraView.Size.Y / 2);
            int width = (int)cameraView.Size.X;
            int height = (int)cameraView.Size.Y;
            IntRect viewRect = new IntRect(left, top, width, height);

            return ChunkBounds.Intersects(viewRect);
        }

    }

    class WorldGenerator
    {
        public readonly int Width;
        public readonly int Height;
        public readonly float Frequency;
        public readonly float Amplitude;
        private FastNoiseLite WorldNoise;
        private FastNoiseLite CaveNoise;
        public readonly int Seed;
        public readonly List<Chunk> Chunks = new List<Chunk>();

        public WorldGenerator(int width, int height, float frequency, float amplitude, int seed)
        {
            this.Width = width;
            this.Height = height;
            this.Frequency = frequency;
            this.Amplitude = amplitude;
            this.Seed = seed;

            this.WorldNoise = new(seed);
            WorldNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
            WorldNoise.SetFrequency(frequency/1.5f);
            this.CaveNoise = new(seed);
            CaveNoise.SetNoiseType(FastNoiseLite.NoiseType.Cellular);
            CaveNoise.SetFrequency(frequency * 10);
        }

        public Chunk GenerateNoise(int offset=0)
        {
            int[,] terrain = new int[Width, Height];

            for (int x = 0; x < Width; x++)
            {
                float noiseValue = WorldNoise.GetNoise(x + offset, 0);

                float normalized = (noiseValue + 1) / 2.0f;
                int terrainHeight = (int)(normalized * Amplitude) + (Height / 2 - (int)(Amplitude / 2));

                for (int y = 0; y < Height; y++)
                {
                    terrain[x, y] = (y >= terrainHeight) ? 0 : -1;
                }
            }

            Chunk chunk = new(terrain, new IntRect(new Vector2i(offset * Width, 0), new Vector2i(Width * 16, Height * 16)));
            chunk.ID = Chunks.Count;
            Chunks.Add(chunk);
            return chunk;
        }

        public Chunk GenerateCaves(Chunk terrain, int offset = 0)
        {
            int[,] newTerrain = new int[Width, Height];

            for(int x = 0; x < Width; x++)
            {
                for(int y = 0; y < Height; y++)
                {
                    if (terrain.TerrainVertices[x, y] == -1)
                    {
                        newTerrain[x, y] = -1;
                        continue;
                    }

                    float cellularValue = CaveNoise.GetNoise(x + offset, y);
                    float simplexValue = WorldNoise.GetNoise(x + offset, y);
                    float cosValue = cosMix(cellularValue, simplexValue, 0.55f);
                    float noiseValue = mix(cosValue, simplexValue, 0.5f);
                    float normalized = (noiseValue + 1) / 2.0f;

                    newTerrain[x, y] = (normalized > 0.1f) ? 0 : -1;
                }
            }

            terrain.TerrainVertices = newTerrain;
            return terrain;
        }

        public VertexArray GenerateTerrain(Chunk terrain, Texture tileSet, int offset = 0)
        {
            VertexArray vertices = new(PrimitiveType.Quads);
            int tileSize = 16;
            int tileSetWidth = (int)(tileSet.Size.X / tileSize);

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    int tileNumber = terrain.TerrainVertices[x, y];

                    if (tileNumber == -1)
                        continue;

                    // Calculate position in tileset
                    //int tu = tileNumber % tileSetWidth;
                    //int tv = tileNumber / tileSetWidth;

                    // Define the four corners of the tile
                    Vertex[] quad = new Vertex[4];

                    // Position each corner
                    quad[0].Position = new Vector2f(x * tileSize + offset, y * tileSize);
                    quad[1].Position = new Vector2f((x + 1) * tileSize + offset, y * tileSize);
                    quad[2].Position = new Vector2f((x + 1) * tileSize + offset, (y + 1) * tileSize);
                    quad[3].Position = new Vector2f(x * tileSize + offset, (y + 1) * tileSize);

                    // Assign texture coordinates
                    Vector2f topLeft = new Vector2f(0, 0);
                    Vector2f topRight = new Vector2f(tileSize, 0);
                    Vector2f bottomRight = new Vector2f(tileSize, tileSize);
                    Vector2f bottomLeft = new Vector2f(0, tileSize);

                    quad[0].Color = Color.White;
                    quad[1].Color = Color.White;
                    quad[2].Color = Color.White;
                    quad[3].Color = Color.White;

                    quad[0].TexCoords = topLeft;
                    quad[1].TexCoords = topRight;
                    quad[2].TexCoords = bottomRight;
                    quad[3].TexCoords = bottomLeft;

                    // Append vertices to the vertex array
                    foreach (var vertex in quad)
                    {
                        //Console.WriteLine(vertex);
                        vertices.Append(vertex);
                    }
                }
            }

            return vertices;
        }

        float mix(float a, float b, float t)
        {
            return a * (1 - t) + b * t;
        }

        float cosMix(float a, float b, float t)
        {
            float mu2 = (1 - MathF.Cos(t * MathF.PI)) / 2;
            return a * (1 - mu2) + b * mu2;
        }
    }
}
