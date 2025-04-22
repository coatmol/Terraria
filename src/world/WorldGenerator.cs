using ProtoBuf;
using SFML.Graphics;
using SFML.System;
using Terraria.game;
using Terraria.physics;
using Terraria.utils;

namespace Terraria.world
{
    public struct WorldData
    {
        public int seed;
        public List<Chunk> chunks;
    }

    public class WorldGenerator
    {
        public WorldData worldData;
        public readonly int Width = Constants.CHUNK_SIZE.X;
        public readonly int Height = Constants.CHUNK_SIZE.Y;
        public readonly float Frequency;
        public readonly float Amplitude;
        private FastNoiseLite WorldNoise;
        private FastNoiseLite CaveNoise;
        private Texture TileSet;
        public Shader tileShader;

        public WorldGenerator(Texture tileset, float frequency, float amplitude, int seed)
        {
            this.Frequency = frequency;
            this.Amplitude = amplitude;
            this.worldData = new WorldData
            {
                seed = seed,
                chunks = new List<Chunk>()
            };
            Init(tileset);
        }
        
        public WorldGenerator(Texture tileset, float frequency, float amplitude, string filePath)
        {
            LoadFromFile(filePath);

            this.Frequency = frequency;
            this.Amplitude = amplitude;
            Init(tileset);
        }

        private void Init(Texture tileset)
        {
            this.WorldNoise = new FastNoiseLite(worldData.seed);
            WorldNoise.SetNoiseType(FastNoiseLite.NoiseType.Cellular);
            WorldNoise.SetFrequency(this.Frequency);
            WorldNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
            WorldNoise.SetFractalOctaves(15);

            this.CaveNoise = new FastNoiseLite(worldData.seed);
            CaveNoise.SetNoiseType(FastNoiseLite.NoiseType.Cellular);
            CaveNoise.SetFrequency(this.Frequency);
            CaveNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
            CaveNoise.SetFractalOctaves(5);

            if (!Shader.IsAvailable)
                throw new NotSupportedException("Shaders not supported");

            tileShader = new Shader(null, null, "assets/shaders/tile-frag.glsl");
            this.TileSet = tileset;
        }

        private void BuildTerrain()
        {
            for (int x = 0; x < Constants.WORLD_SIZE; x++)
            {
                int offset = x * Constants.CHUNK_SIZE.X;
                Chunk terrain = GenerateLightmap(GenerateCaves(GenerateNoise(offset), offset));
                Chunk finalChunk = GenerateTerrain(terrain, terrain.ChunkBounds.Left);
                worldData.chunks.Add(finalChunk);
            }
            EventManager.CallEvent(EventManager.EventType.WorldLoaded, worldData);
            Console.WriteLine("Built terrain!");
        }

        public void SaveToFile(string filePath)
        {
            using (var file = File.Create(filePath))
            {
                Serializer.Serialize(file, worldData.chunks);
                Console.WriteLine($"Successfully saved world to {file.Name}");
            }
        }

        public void LoadFromFile(string filePath)
        {
            using (var file = File.OpenRead(filePath))
            {
                worldData = Serializer.Deserialize<WorldData>(file);
                foreach (Chunk chunk in worldData.chunks)
                {
                    chunk.DeflattenMap();
                    GenerateLightmap(chunk);
                    GenerateTerrain(chunk, chunk.ID);
                }
            }
        }

        public int GetHeight(int x, int offset = 0)
        {
            float noiseValue = MathF.Pow(WorldNoise.GetNoise(x + offset, 0) * 1.35f, 4);
            float ridge = 1 * MathF.Abs(noiseValue);
            float normalized = (noiseValue + 1) / 2.0f;
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
            chunk.ID = worldData.chunks.Count;
            worldData.chunks.Add(chunk);
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
                    float finalValue = Utils.LerpF(caveValue, worldValue, 0.75f);
                    float normalized = (finalValue + 1) / 2f;

                    float middleHeight = Height / 2f;
                    float distanceFromMiddle = MathF.Abs(normalized * Amplitude - middleHeight);

                    float falloff = MathF.Exp(-MathF.Pow(distanceFromMiddle / middleHeight, 2)) * 4;

                    normalized *= falloff;

                    newTerrain[x, y] = (normalized > 0.1f) ? chunk.TerrainMap[x, y] : Blocks.GetBlock("Air");
                }
            }

            chunk.TerrainMap = newTerrain;
            return chunk;
        }

        public Chunk GenerateLightmap(Chunk chunk)
        {
            chunk.TerrainMap = CalculateLight(chunk);
            return chunk;
        }

        public Block[,] CalculateLight(Chunk chunk)
        {
            Block[,] terrain = chunk.TerrainMap;
            Queue<Vector2i> lightQueue = new Queue<Vector2i>();
            Vector2i[] neighborOffsets = { new(-1, 0), new(1, 0), new(0, -1), new(0, 1) };

            #region Light Reset
            Parallel.For(0, Constants.CHUNK_SIZE.X, x => {
                for (int y = 0; y < Constants.CHUNK_SIZE.Y; y++)
                {
                    terrain[x, y].lightLevel = terrain[x, y].lightSource;
                    if (terrain[x, y].lightSource > 1)
                    {
                        lock (lightQueue)
                        {
                            lightQueue.Enqueue(new Vector2i(x, y));
                        }
                    }
                }
            });
            #endregion
            #region Surface Illumination
            int[] heightMap = new int[Constants.CHUNK_SIZE.X];
            Parallel.For(0, Constants.CHUNK_SIZE.X, x => {
                for (int y = 0; y < Constants.CHUNK_SIZE.Y; y++)
                {
                    if (!terrain[x, y].isTransparent)
                    {
                        heightMap[x] = y;
                        terrain[x, y].lightLevel = Constants.MAX_LIGHT_LEVEL;
                        lock (lightQueue)
                        {
                            lightQueue.Enqueue(new Vector2i(x, y));
                        }
                        break;
                    } else
                    {
                        if(x - 1 >= 0 && !terrain[x - 1, y].isTransparent)
                        {
                            terrain[x - 1, y].lightLevel = Constants.MAX_LIGHT_LEVEL;
                            lock (lightQueue)
                            {
                                lightQueue.Enqueue(new Vector2i(x - 1, y));
                            }
                        }
                        else if (x + 1 < Constants.CHUNK_SIZE.X && !terrain[x + 1, y].isTransparent)
                        {
                            terrain[x + 1, y].lightLevel = Constants.MAX_LIGHT_LEVEL;
                            lock (lightQueue)
                            {
                                lightQueue.Enqueue(new Vector2i(x + 1, y));
                            }
                        }
                    }
                }
            });
            #endregion
            #region Light Propagation
            bool[,] processed = new bool[Constants.CHUNK_SIZE.X, Constants.CHUNK_SIZE.Y];
            while (lightQueue.Count > 0)
            {
                Vector2i pos = lightQueue.Dequeue();
                Block currentBlock = terrain[pos.X, pos.Y];
                Vector2i neighborPos = new Vector2i();

                foreach (var offset in neighborOffsets)
                {
                    neighborPos.X = pos.X + offset.X;
                    neighborPos.Y = pos.Y + offset.Y;

                    if (neighborPos.X >= Constants.CHUNK_SIZE.X || neighborPos.Y >= Constants.CHUNK_SIZE.Y || neighborPos.X < 0 || neighborPos.Y < 0)
                        continue;

                    if (processed[neighborPos.X, neighborPos.Y])
                        continue;

                    Block neighborBlock = terrain[neighborPos.X, neighborPos.Y];
                    int newLightLevel = currentBlock.lightLevel - 1;
                    if (neighborBlock.lightLevel < newLightLevel)
                    {
                        neighborBlock.lightLevel = newLightLevel;
                        terrain[neighborPos.X, neighborPos.Y] = neighborBlock;
                        processed[neighborPos.X, neighborPos.Y] = true;
                        lightQueue.Enqueue(neighborPos);
                    }
                }
            }
            #endregion
            return terrain;
        }

        public void CalculateLight(Chunk chunk, Vector2i lightSourcePosition)
        {
            Block[,] terrain = chunk.TerrainMap;
            Queue<Vector2i> lightQueue = new Queue<Vector2i>();
            Vector2i[] neighborOffsets = { new(-1, 0), new(1, 0), new(0, -1), new(0, 1) };
            bool[,] processed = new bool[Constants.CHUNK_SIZE.X, Constants.CHUNK_SIZE.Y];

            lightQueue.Enqueue(lightSourcePosition);

            while (lightQueue.Count > 0)
            {
                Vector2i pos = lightQueue.Dequeue();
                Block currentBlock = terrain[pos.X, pos.Y];
                Vector2i neighborPos = new Vector2i();

                foreach (var offset in neighborOffsets)
                {
                    neighborPos.X = pos.X + offset.X;
                    neighborPos.Y = pos.Y + offset.Y;

                    if (neighborPos.X >= Constants.CHUNK_SIZE.X || neighborPos.Y >= Constants.CHUNK_SIZE.Y || neighborPos.X < 0 || neighborPos.Y < 0)
                        continue;

                    if (processed[neighborPos.X, neighborPos.Y])
                        continue;

                    Block neighborBlock = terrain[neighborPos.X, neighborPos.Y];
                    int newLightLevel = currentBlock.lightLevel - 1;
                    if (neighborBlock.lightLevel < newLightLevel)
                    {
                        neighborBlock.lightLevel = newLightLevel;
                        terrain[neighborPos.X, neighborPos.Y] = neighborBlock;
                        processed[neighborPos.X, neighborPos.Y] = true;
                        lightQueue.Enqueue(neighborPos);
                    }
                }
            }

            GenerateTerrain(chunk, chunk.ChunkBounds.Left);
            EventManager.CallEvent(EventManager.EventType.TerrainUpdated, chunk.ID);
        }

        public Chunk GenerateTerrain(Chunk chunk, int offset = 0)
        {
            VertexArray vertices = new(PrimitiveType.Quads);
            int tileSetWidth = (int)(TileSet.Size.X / Constants.BLOCK_SIZE);

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    Block block = chunk.TerrainMap[x, y];

                    if (block == Blocks.GetBlock("Air"))
                        continue;

                    int tu = block.id % tileSetWidth;
                    int tv = block.id / tileSetWidth;

                    int L = Math.Min(Math.Max(block.lightLevel, 1), Constants.MAX_LIGHT_LEVEL);
                    float fraction = L / (float)Constants.MAX_LIGHT_LEVEL;
                    byte lightCalc = (byte)(fraction * 255f);
                    Color lightColor = new Color(lightCalc, lightCalc, lightCalc, 255);

                    Vertex[] quad = new Vertex[4];

                    quad[0].Position = new Vector2f(x * Constants.BLOCK_SIZE + offset, y * Constants.BLOCK_SIZE);
                    quad[1].Position = new Vector2f((x + 1) * Constants.BLOCK_SIZE + offset, y * Constants.BLOCK_SIZE);
                    quad[2].Position = new Vector2f((x + 1) * Constants.BLOCK_SIZE + offset, (y + 1) * Constants.BLOCK_SIZE);
                    quad[3].Position = new Vector2f(x * Constants.BLOCK_SIZE + offset, (y + 1) * Constants.BLOCK_SIZE);

                    quad[0].Color = Color.White * lightColor;
                    quad[1].Color = Color.White * lightColor;
                    quad[2].Color = Color.White * lightColor;
                    quad[3].Color = Color.White * lightColor;

                    quad[0].TexCoords = new Vector2f(tu * Constants.BLOCK_SIZE, tv * Constants.BLOCK_SIZE);
                    quad[1].TexCoords = new Vector2f((tu + 1) * Constants.BLOCK_SIZE, tv * Constants.BLOCK_SIZE);
                    quad[2].TexCoords = new Vector2f((tu + 1) * Constants.BLOCK_SIZE, (tv + 1) * Constants.BLOCK_SIZE);
                    quad[3].TexCoords = new Vector2f(tu * Constants.BLOCK_SIZE, (tv + 1) * Constants.BLOCK_SIZE);

                    foreach (var vertex in quad)
                    {
                        vertices.Append(vertex);
                    }
                }
            }

            chunk.Vertices = vertices;
            return chunk;
        }
        
        public VertexArray GenerateBgWalls()
        {
            Block[,] walls = new Block[Constants.CHUNK_SIZE.X * Constants.WORLD_SIZE, Constants.CHUNK_SIZE.Y];
            for (int x = 0; x < Constants.CHUNK_SIZE.X * Constants.WORLD_SIZE; x++)
            {
                int terrainHeight = GetHeight(x);

                for (int y = 0; y < Height; y++)
                {
                    if (y < terrainHeight)
                    {
                        walls[x, y] = Blocks.GetBlock("Air");
                    }
                    else
                    {
                        walls[x, y] = Blocks.GetBlock("DirtBG");
                    }
                }
            }
            return GenerateBgWalls(walls);
        }

        public VertexArray GenerateBgWalls(Block[,] walls)
        {
            int W = walls.GetLength(0), H = walls.GetLength(1);
            bool[,] merged = new bool[W, H];
            var mesh = new VertexArray(PrimitiveType.Quads);

            for (int x = 0; x < W; ++x)
                for (int y = 0; y < H; ++y)
                {
                    if (merged[x, y] || !walls[x, y].isBg)
                        continue;

                    int tileId = walls[x, y].id;

                    // — Greedy expand width —
                    int maxW = 1;
                    while (x + maxW < W
                        && !merged[x + maxW, y]
                        && walls[x + maxW, y].id == tileId)
                        maxW++;

                    // — Greedy expand height —
                    int maxH = 1;
                    bool ok = true;
                    while (y + maxH < H && ok)
                    {
                        for (int dx = 0; dx < maxW; dx++)
                        {
                            if (merged[x + dx, y + maxH]
                             || walls[x + dx, y + maxH].id != tileId)
                            {
                                ok = false; break;
                            }
                        }
                        if (ok) maxH++;
                    }

                    // — Mark merged tiles —
                    for (int dy = 0; dy < maxH; dy++)
                        for (int dx = 0; dx < maxW; dx++)
                            merged[x + dx, y + dy] = true;

                    tileShader.SetUniform("uTexture", TileSet);
                    tileShader.SetUniform("uTileIndex", tileId);
                    tileShader.SetUniform("uAtlasSize", new Vector2f(TileSet.Size.X, TileSet.Size.Y));

                    float x1 = x * Constants.BLOCK_SIZE;
                    float y1 = y * Constants.BLOCK_SIZE;
                    float x2 = (x + maxW) * Constants.BLOCK_SIZE;
                    float y2 = (y + maxH) * Constants.BLOCK_SIZE;

                    Vertex[] quad =
                    {
                        new Vertex(new Vector2f(x1, y1), Color.White, new Vector2f(x1, y1)),
                        new Vertex(new Vector2f(x2, y1), Color.White, new Vector2f(x2 + maxW, y1)),
                        new Vertex(new Vector2f(x2, y2), Color.White, new Vector2f(x2 + maxW, y2 + maxH)),
                        new Vertex(new Vector2f(x1, y2), Color.White, new Vector2f(x1, y2 + maxH)),
                    };
                    foreach (var v in quad) mesh.Append(v);
                }

            return mesh;
        }


        public Chunk? GetChunkFromPosition(Vector2f pos)
        {
            int chunkId = (int)Math.Floor(pos.X / Constants.BLOCK_SIZE / Constants.CHUNK_SIZE.X);
            if (chunkId < 0 || chunkId >= worldData.chunks.Count)
                return null;
            Chunk chunk = worldData.chunks[chunkId];
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
                if (tile.id != -1 || tile.isBg)
                    return new Vector2i(mapX, mapY);
            }

            return null;
        }



        public void PlaceBlock(Vector2f pos, Block block)
        {
            Chunk? chunk = GetChunkFromPosition(pos);
            if (chunk == null)
                return;
            chunk.PlaceBlock(pos, block);
        }

        public void RemoveBlock(Vector2f pos)
        {
            Chunk? chunk = GetChunkFromPosition(pos);
            if (chunk == null)
                return;
            chunk.RemoveBlock(pos);
        }
    }

    [ProtoContract]
    public class Chunk
    {
        [ProtoMember(1)]
        public int ID;
        public bool IsDirty = false;
        [ProtoMember(2)]
        public Block[] blocks
        {
            get { return FlattenMap(); }
        }
        public Block[,] TerrainMap;
        public VertexArray Vertices;
        [ProtoMember(3)]
        public readonly IntRect ChunkBounds;

        public Chunk(Block[,] terrainMap, IntRect chunkBounds)
        {
            TerrainMap = terrainMap;
            ChunkBounds = chunkBounds;
            Vertices = new VertexArray(PrimitiveType.Quads);
        }

        public void DeflattenMap()
        {
            for (int i = 0; i < blocks.Length; i++)
            {
                int x = i / Constants.CHUNK_SIZE.Y;
                int y = i % Constants.CHUNK_SIZE.Y;

                int id = blocks[i].id;
                TerrainMap[x, y] = Blocks.GetBlock(id);
            }
        }

        public Block[] FlattenMap()
        {
            int w = TerrainMap.GetLength(0), h = TerrainMap.GetLength(1);
            var flat = new Block[w * h];
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    flat[x * h + y] = TerrainMap[x, y];

            return flat;
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

        public void PlaceBlock(Vector2f pos, Block block)
        {
            int x = (int)(pos.X - ChunkBounds.Left) / Constants.BLOCK_SIZE;
            int y = (int)(pos.Y - ChunkBounds.Top) / Constants.BLOCK_SIZE;

            if (x < 0 || x >= TerrainMap.GetLength(0) || y < 0 || y >= TerrainMap.GetLength(1))
                return;
            if (TerrainMap[x, y] != Blocks.GetBlock("Air"))
                return;

            TerrainMap[x, y] = block;
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

            TerrainMap = world.CalculateLight(this);
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
                    if (processed[tx, ty] || TerrainMap[tx, ty].collisionType == CollisionType.None)
                        continue;

                    int rectWidth = 1;
                    while (tx + rectWidth < tileWidth && !processed[tx + rectWidth, ty] && TerrainMap[tx + rectWidth, ty].collisionType != CollisionType.None)
                    {
                        rectWidth++;
                    }

                    int rectHeight = 1;
                    bool done = false;
                    while (ty + rectHeight < tileHeight && !done)
                    {
                        for (int i = 0; i < rectWidth; i++)
                        {
                            if (processed[tx + i, ty + rectHeight] || TerrainMap[tx + i, ty + rectHeight].collisionType == CollisionType.None)
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
