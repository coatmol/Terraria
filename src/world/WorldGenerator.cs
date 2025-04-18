﻿using SFML.Graphics;
using SFML.System;
using SFML.Window;
using Terraria.game;
using Terraria.physics;
using Terraria.utils;

namespace Terraria.world
{
    public class WorldGenerator
    {
        public readonly int Width = Constants.CHUNK_SIZE.X;
        public readonly int Height = Constants.CHUNK_SIZE.Y;
        public readonly float Frequency;
        public readonly float Amplitude;
        private readonly FastNoiseLite WorldNoise;
        private readonly FastNoiseLite CaveNoise;
        public readonly int Seed;
        public readonly List<Chunk> Chunks = new List<Chunk>();
        private readonly Texture TileSet;

        public WorldGenerator(Texture tileset, float frequency, float amplitude, int seed)
        {
            this.Frequency = frequency;
            this.Amplitude = amplitude;
            this.Seed = seed;

            this.WorldNoise = new(seed);
            WorldNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
            WorldNoise.SetFrequency(frequency / 2f);
            WorldNoise.SetFractalType(FastNoiseLite.FractalType.DomainWarpProgressive);
            this.CaveNoise = new(seed);
            CaveNoise.SetNoiseType(FastNoiseLite.NoiseType.Cellular);
            CaveNoise.SetFrequency(frequency);
            CaveNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
            CaveNoise.SetFractalOctaves(3);

            this.TileSet = tileset;
        }

        public int GetHeight(int x, int offset = 0)
        {
            float noiseValue = MathF.Pow(WorldNoise.GetNoise(x + offset, 0) * 1.4f, 4);
            float ridge = -1 * MathF.Abs(noiseValue);
            float normalized = (ridge + 1) / 2.0f;
            int terrainHeight = (int)(normalized * Amplitude) + (Height / 2 - (int)(Amplitude / 2));
            return terrainHeight;
        }

        public Chunk GenerateNoise(int offset=0)
        {
            Block[,] terrain = new Block[Width, Height];

            for (int x = 0; x < Width; x++)
            {
                int terrainHeight = GetHeight(x, offset);

                for (int y = 0; y < Height; y++)
                {
                    if (y < terrainHeight)
                    {
                        terrain[x, y] = Blocks.GetBlock("Air");
                    } else if (y == terrainHeight)
                    {
                        terrain[x, y] = Blocks.GetBlock("Grass");
                    } else if(y < terrainHeight + 5)
                    {
                        terrain[x, y] = Blocks.GetBlock("Dirt");
                    } else
                    {
                        terrain[x, y] = Blocks.GetBlock("Stone");
                    }
                }
            }

            Chunk chunk = new(terrain, new IntRect(new Vector2i(offset * Width/(Width/Constants.BLOCK_SIZE), 0), new Vector2i(Width * Constants.BLOCK_SIZE, Height * Constants.BLOCK_SIZE)));
            chunk.ID = Chunks.Count;
            Chunks.Add(chunk);
            return chunk;
        }

        public Chunk GenerateCaves(Chunk chunk, int offset = 0)
        {
            Block[,] newTerrain = new Block[Width, Height];

            for(int x = 0; x < Width; x++)
            {
                for(int y = 0; y < Height; y++)
                {
                    if (chunk.TerrainMap[x, y] == Blocks.GetBlock("Air"))
                    {
                        newTerrain[x, y] = Blocks.GetBlock("Air");
                        continue;
                    }

                    float caveValue = CaveNoise.GetNoise(x + offset, y);
                    float worldValue = WorldNoise.GetNoise(x + offset, y);
                    float finalValue = Utils.LerpF(caveValue, worldValue, 0.5f);
                    float normalized = (finalValue + 1) / 2f;

                    newTerrain[x, y] = (normalized > 0.1f) ? chunk.TerrainMap[x, y] : Blocks.GetBlock("Air");
                }
            }

            chunk.TerrainMap = newTerrain;
            return chunk;
        }

        public Chunk CalculateLight(Chunk chunk)
        {
            chunk.TerrainMap = CalculateLight(chunk.TerrainMap);
            return chunk;
        }

        public Block[,] CalculateLight(Block[,] terrain)
        {
            #region Light Reset
            for (int x = 0; x < Constants.CHUNK_SIZE.X; x++)
            {
                for (int y = 0; y < Constants.CHUNK_SIZE.Y; y++)
                {
                    terrain[x, y].lightLevel = 1;
                }
            }
            #endregion

            Queue<Vector2i> lightQueue = new Queue<Vector2i>();
            Vector2i[] neighborOffsets = { new(-1, 0), new(1, 0), new(0, -1), new(0, 1) };

            #region Surface Illumination
            int[] heightMap = new int[Constants.CHUNK_SIZE.X];
            for (int x = 0; x < Constants.CHUNK_SIZE.X; x++)
            {
                for (int y = 0; y < Constants.CHUNK_SIZE.Y; y++)
                {
                    if (!terrain[x, y].isTransparent)
                    {
                        heightMap[x] = y;
                        terrain[x, y].lightLevel = Constants.MAX_LIGHT_LEVEL;
                        lightQueue.Enqueue(new Vector2i(x, y));
                        break;
                    }
                }
            }
            #endregion
            #region Light Propagation
            List<Vector2i> processed = new List<Vector2i>();
            while (lightQueue.Count > 0)
            {
                Vector2i pos = lightQueue.Dequeue();
                Block currentBlock = terrain[pos.X, pos.Y];

                foreach (var offset in neighborOffsets)
                {
                    Vector2i neighborPos = pos + offset;
                    if (processed.Contains(neighborPos))
                        continue;
                    if (neighborPos.X < Constants.CHUNK_SIZE.X && neighborPos.Y < Constants.CHUNK_SIZE.Y && neighborPos.X >= 0 && neighborPos.Y >= 0)
                    {
                        Block neighborBlock = terrain[neighborPos.X, neighborPos.Y];
                        int newLightLevel = currentBlock.lightLevel - 1;
                        if (!neighborBlock.isTransparent)
                        {
                            if (neighborBlock.lightLevel < newLightLevel)
                            {
                                neighborBlock.lightLevel = newLightLevel;
                                terrain[neighborPos.X, neighborPos.Y] = neighborBlock;
                                processed.Add(neighborPos);
                                lightQueue.Enqueue(neighborPos);
                            }
                        }
                    }
                }
            }
            #endregion
            return terrain;
        }

        public VertexArray GenerateTerrain(Chunk terrain, int offset = 0)
        {
            VertexArray vertices = new(PrimitiveType.Quads);
            int tileSize = Constants.BLOCK_SIZE;
            int tileSetWidth = (int)(TileSet.Size.X / tileSize);

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    Block block = terrain.TerrainMap[x, y];

                    if (block == Blocks.GetBlock("Air"))
                        continue;

                    int tu = block.id % tileSetWidth;
                    int tv = block.id / tileSetWidth;

                    int L = Math.Min(Math.Max(block.lightLevel, 1), Constants.MAX_LIGHT_LEVEL);
                    float fraction = L / (float)Constants.MAX_LIGHT_LEVEL;
                    byte lightCalc = (byte)(fraction * 255f);
                    Color lightColor = new Color(lightCalc, lightCalc, lightCalc, 255);

                    Vertex[] quad = new Vertex[4];

                    quad[0].Position = new Vector2f(x * tileSize + offset, y * tileSize);
                    quad[1].Position = new Vector2f((x + 1) * tileSize + offset, y * tileSize);
                    quad[2].Position = new Vector2f((x + 1) * tileSize + offset, (y + 1) * tileSize);
                    quad[3].Position = new Vector2f(x * tileSize + offset, (y + 1) * tileSize);

                    quad[0].Color = Color.White * lightColor;
                    quad[1].Color = Color.White * lightColor;
                    quad[2].Color = Color.White * lightColor;
                    quad[3].Color = Color.White * lightColor;

                    quad[0].TexCoords = new Vector2f(tu * tileSize, tv * tileSize);
                    quad[1].TexCoords = new Vector2f((tu + 1) * tileSize, tv * tileSize);
                    quad[2].TexCoords = new Vector2f((tu + 1) * tileSize, (tv + 1) * tileSize);
                    quad[3].TexCoords = new Vector2f(tu * tileSize, (tv + 1) * tileSize);

                    foreach (var vertex in quad)
                    {
                        vertices.Append(vertex);
                    }
                }
            }

            terrain.Vertices = vertices;
            return vertices;
        }

        public Chunk? GetChunkFromPosition(Vector2f pos)
        {
            int chunkId = (int)Math.Floor(pos.X / Constants.BLOCK_SIZE / Constants.CHUNK_SIZE.X);
            if (chunkId < 0 || chunkId >= Chunks.Count)
                return null;
            Chunk chunk = Chunks[chunkId];
            if (chunk == null)
                return null;
            return chunk;
        }

        public Block? GetBlock(Vector2f pos)
        {
            Chunk? chunk = GetChunkFromPosition(pos);
            if (chunk == null) return null;
            Vector2i localPos = (Vector2i)(pos / Constants.BLOCK_SIZE);
            if(localPos.X < 0 || localPos.Y < 0 || localPos.X > Constants.CHUNK_SIZE.X || localPos.Y > Constants.CHUNK_SIZE.Y)
                return null;
            return chunk.TerrainMap[localPos.X, localPos.Y];
        }

        public Vector2i? Raycast(Vector2f mousePos, Vector2f playerPos, float distance)
        {
            // 1) Calculate direction
            Vector2f dir = mousePos - playerPos;
            float length = MathF.Sqrt(dir.X * dir.X + dir.Y * dir.Y);
            dir /= length;

            // 2) Initialize DDA variables
            int mapX = (int)MathF.Floor(playerPos.X / Constants.BLOCK_SIZE);
            int mapY = (int)MathF.Floor(playerPos.Y / Constants.BLOCK_SIZE);
            int stepX = dir.X < 0 ? -1 : 1;
            int stepY = dir.Y < 0 ? -1 : 1;
            float deltaX = Constants.BLOCK_SIZE / MathF.Abs(dir.X);
            float deltaY = Constants.BLOCK_SIZE / MathF.Abs(dir.Y);
            float nextX = (mapX + (stepX > 0 ? 1 : 0)) * Constants.BLOCK_SIZE;
            float nextY = (mapY + (stepY > 0 ? 1 : 0)) * Constants.BLOCK_SIZE;
            float tMaxX = (nextX - playerPos.X) / dir.X;
            float tMaxY = (nextY - playerPos.Y) / dir.Y;

            // 3) Ray‐march loop
            for (int i = 0; i < distance; i++)
            {
                // Advance the ray
                if (tMaxX < tMaxY)
                {
                    tMaxX += deltaX;
                    mapX += stepX;
                }
                else
                {
                    tMaxY += deltaY;
                    mapY += stepY;
                }

                Chunk? chunk = GetChunkFromPosition(new Vector2f(
                    mapX * Constants.BLOCK_SIZE,
                    mapY * Constants.BLOCK_SIZE));
                if (chunk == null)
                    continue;

                int localX = mapX - chunk.ChunkBounds.Left / Constants.BLOCK_SIZE;
                int localY = mapY - chunk.ChunkBounds.Top / Constants.BLOCK_SIZE;

                if (localX < 0 || localY < 0 || localX >= Constants.CHUNK_SIZE.X || localY >= Constants.CHUNK_SIZE.Y)
                    return null;

                var tile = chunk.TerrainMap[localX, localY];
                if (tile.id != -1)
                    return new Vector2i(mapX, mapY);
            }

            return null;
        }



        public void PlaceBlock(Vector2f pos)
        {
            Chunk? chunk = GetChunkFromPosition(pos);
            if (chunk == null)
                return;
            chunk.PlaceBlock(pos);
        }

        public void RemoveBlock(Vector2f pos)
        {
            Chunk? chunk = GetChunkFromPosition(pos);
            if (chunk == null)
                return;
            chunk.RemoveBlock(pos);
        }
    }

    public class Chunk
    {
        public Block[,] TerrainMap;
        public VertexArray Vertices;
        public readonly IntRect ChunkBounds;
        public int ID;
        private bool IsDirty = false;

        public Chunk(Block[,] terrainMap, IntRect chunkBounds)
        {
            TerrainMap = terrainMap;
            ChunkBounds = chunkBounds;
            Vertices = new VertexArray(PrimitiveType.Quads);
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

        public void PlaceBlock(Vector2f pos)
        {
            int x = (int)(pos.X - ChunkBounds.Left) / Constants.BLOCK_SIZE;
            int y = (int)(pos.Y - ChunkBounds.Top) / Constants.BLOCK_SIZE;

            if (x < 0 || x >= TerrainMap.GetLength(0) || y < 0 || y >= TerrainMap.GetLength(1))
                return;
            if (TerrainMap[x, y] != Blocks.GetBlock("Air"))
                return;

            TerrainMap[x, y] = Blocks.GetBlock("Stone");
            IsDirty = true;
        }

        public void RemoveBlock(Vector2f pos)
        {
            int x = (int)(pos.X - ChunkBounds.Left) / Constants.BLOCK_SIZE;
            int y = (int)(pos.Y - ChunkBounds.Top) / Constants.BLOCK_SIZE;

            if (x < 0 || x >= TerrainMap.GetLength(0) || y < 0 || y >= TerrainMap.GetLength(1))
                return;
            if (TerrainMap[x, y] == Blocks.GetBlock("Air"))
                return;

            TerrainMap[x, y] = Blocks.GetBlock("Air");
            IsDirty = true;
        }

        public void Update(WorldGenerator world)
        {
            if(!IsDirty)
                return;

            TerrainMap = world.CalculateLight(TerrainMap);
            world.GenerateTerrain(this, ChunkBounds.Left);
            EventManager.CallEvent(EventManager.EventType.TerrainUpdated, ID);

            IsDirty = false;
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
                    if (processed[tx, ty] || TerrainMap[tx, ty] == Blocks.GetBlock("Air"))
                        continue;

                    int rectWidth = 1;
                    while (tx + rectWidth < tileWidth && !processed[tx + rectWidth, ty] && TerrainMap[tx + rectWidth, ty] != Blocks.GetBlock("Air"))
                    {
                        rectWidth++;
                    }

                    int rectHeight = 1;
                    bool done = false;
                    while (ty + rectHeight < tileHeight && !done)
                    {
                        for (int i = 0; i < rectWidth; i++)
                        {
                            if (processed[tx + i, ty + rectHeight] || TerrainMap[tx + i, ty + rectHeight] == Blocks.GetBlock("Air"))
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
}
