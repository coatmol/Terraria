using SFML.Graphics;
using SFML.System;
using Terraria.physics;
using Terraria.utils;

namespace Terraria.world
{

    public class Chunk
    {
        public int[,] TerrainMap;
        public VertexArray Vertices;
        public readonly IntRect ChunkBounds;
        public int ID;

        public Chunk(int[,] terrainMap, IntRect chunkBounds)
        {
            TerrainMap = terrainMap;
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

        public int GetOffset()
        {
            return ChunkBounds.Width / 16;
        }

        public bool IsCurrentChunk(IntRect playerBB)
        {
            return ChunkBounds.Intersects(playerBB);
        }

        public List<IntRect> GetBlockRects()
        {
            List<IntRect> rects = new List<IntRect>();
            for (int vert = 0; vert < Vertices.VertexCount; vert += 4)
            {
                Vector2f pos = Vertices[(uint)vert].Position;
                Vector2f size = new Vector2f(Constants.BLOCK_SIZE, Constants.BLOCK_SIZE);
                IntRect rect = new((Vector2i)pos, (Vector2i)size);
                rects.Add(rect);
            }
            return rects;
        }

        public List<Collider> GetColliders()
        {
            List<Collider> colliders = new List<Collider>();
            for (int vert = 0; vert < Vertices.VertexCount; vert += 4)
            {
                Vector2f pos = Vertices[(uint)vert].Position;
                Vector2f size = new Vector2f(Constants.BLOCK_SIZE, Constants.BLOCK_SIZE);
                IntRect rect = new((Vector2i)pos, (Vector2i)size);
                Collider collider = new Collider(rect);
                colliders.Add(collider);
            }
            return colliders;
        }

        public List<Collider> GetMergedColliders()
        {
            int tileWidth = ChunkBounds.Width / Constants.BLOCK_SIZE;
            int tileHeight = ChunkBounds.Height / Constants.BLOCK_SIZE;

            bool[,] processed = new bool[tileWidth, tileHeight];
            List<Collider> colliders = new List<Collider>();

            for (int ty = 0; ty < tileHeight; ty++)
            {
                for (int tx = 0; tx < tileWidth; tx++)
                {
                    if (processed[tx, ty] || TerrainMap[tx, ty] == -1)
                        continue;

                    int rectWidth = 1;
                    while (tx + rectWidth < tileWidth && !processed[tx + rectWidth, ty] && TerrainMap[tx + rectWidth, ty] != -1)
                    {
                        rectWidth++;
                    }

                    int rectHeight = 1;
                    bool done = false;
                    while (ty + rectHeight < tileHeight && !done)
                    {
                        for (int i = 0; i < rectWidth; i++)
                        {
                            if (processed[tx + i, ty + rectHeight] || TerrainMap[tx + i, ty + rectHeight] == -1)
                            {
                                done = true;
                                break;
                            }
                        }
                        if (!done)
                            rectHeight++;
                    }

                    for (int i = 0; i < rectWidth; i++)
                    {
                        for (int j = 0; j < rectHeight; j++)
                        {
                            processed[tx + i, ty + j] = true;
                        }
                    }

                    int worldX = ChunkBounds.Left + tx * Constants.BLOCK_SIZE;
                    int worldY = ChunkBounds.Top + ty * Constants.BLOCK_SIZE;
                    int worldWidth = rectWidth * Constants.BLOCK_SIZE;
                    int worldHeight = rectHeight * Constants.BLOCK_SIZE;

                    Collider rect = new Collider(new Vector2f(worldX, worldY), new Vector2f(worldWidth, worldHeight)) { FillColor = Color.Red, OutlineColor = Color.Yellow, OutlineThickness = 3 };
                    colliders.Add(rect);
                }
            }

            return colliders;
        }

    }

    class WorldGenerator
    {
        public readonly int Width = Constants.CHUNK_SIZE.X;
        public readonly int Height = Constants.CHUNK_SIZE.Y;
        public readonly float Frequency;
        public readonly float Amplitude;
        private FastNoiseLite WorldNoise;
        private FastNoiseLite CaveNoise;
        public readonly int Seed;
        public readonly List<Chunk> Chunks = new List<Chunk>();

        public WorldGenerator(float frequency, float amplitude, int seed)
        {
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
                    if (y < terrainHeight)
                    {
                        terrain[x, y] = -1;
                    } else if (y == terrainHeight)
                    {
                        terrain[x, y] = 0;
                    } else if(y < terrainHeight + 5)
                    {
                        terrain[x, y] = 1;
                    } else
                    {
                        terrain[x, y] = 2;
                    }
                }
            }

            Chunk chunk = new(terrain, new IntRect(new Vector2i(offset * Width, 0), new Vector2i(Width * Constants.BLOCK_SIZE, Height * Constants.BLOCK_SIZE)));
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
                    if (terrain.TerrainMap[x, y] == -1)
                    {
                        newTerrain[x, y] = -1;
                        continue;
                    }

                    float cellularValue = CaveNoise.GetNoise(x + offset, y) * 1.3f;
                    float simplexValue = WorldNoise.GetNoise(x + offset, y) * 1.1f;
                    float cosValue = cosMix(cellularValue, simplexValue, 0.55f);
                    float noiseValue = mix(cosValue, simplexValue, 0.5f);
                    float normalized = (noiseValue + 1) / 2.0f;

                    newTerrain[x, y] = (normalized > 0.1f) ? terrain.TerrainMap[x, y] : -1;
                }
            }

            terrain.TerrainMap = newTerrain;
            return terrain;
        }

        public VertexArray GenerateTerrain(Chunk terrain, Texture tileSet, int offset = 0)
        {
            VertexArray vertices = new(PrimitiveType.Quads);
            int tileSize = Constants.BLOCK_SIZE;
            int tileSetWidth = (int)(tileSet.Size.X / tileSize);

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    int tileNumber = terrain.TerrainMap[x, y];

                    if (tileNumber == -1)
                        continue;

                    // Calculate position in tileset
                    int tu = tileNumber % tileSetWidth;
                    int tv = tileNumber / tileSetWidth;

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

                    quad[0].TexCoords = new Vector2f(tu * tileSize, tv * tileSize);
                    quad[1].TexCoords = new Vector2f((tu + 1) * tileSize, tv * tileSize);
                    quad[2].TexCoords = new Vector2f((tu + 1) * tileSize, (tv + 1) * tileSize);
                    quad[3].TexCoords = new Vector2f(tu * tileSize, (tv + 1) * tileSize);

                    // Append vertices to the vertex array
                    foreach (var vertex in quad)
                    {
                        vertices.Append(vertex);
                    }
                }
            }

            terrain.Vertices = vertices;
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
