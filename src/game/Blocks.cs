using SFML.Graphics;
using System.Text.Json;
using Terraria.utils;

namespace Terraria.game
{
    public static class Blocks
    {
        public static List<Block> AllBlocks = new List<Block>();

        /// <summary>
        /// Registers all blocks by loading their data from JSON files and creating a texture atlas.
        /// </summary>
        /// <returns>
        /// A <see cref="Texture"/> object representing the texture atlas containing all block textures.
        /// </returns>
        public static Texture RegisterBlocks()
        {
            AllBlocks.Clear();
            string[] BlockFiles = Directory.GetFiles("assets/blocks");
            Texture textureAtlas = new Texture((uint)(BlockFiles.Length * Constants.BLOCK_SIZE), Constants.BLOCK_SIZE);
            foreach (var file in BlockFiles)
            {
                if(File.Exists(file) && file.EndsWith(".json"))
                {
                    string jsonString = File.ReadAllText(file);
                    Block block = JsonSerializer.Deserialize<Block>(jsonString);
                    AllBlocks.Add(block);
                } else
                {
                    Console.WriteLine("Failed to load: %s", file);
                }
            }
            AllBlocks.Sort(delegate (Block a, Block b) { return a.id.CompareTo(b.id); });
            foreach (var block in AllBlocks) {
                if (block.id == -1)
                    continue;

                Image blockTexture = new Image($"assets/blocks/textures/{block.name}.png");
                textureAtlas.Update(blockTexture, (uint)(block.id * Constants.BLOCK_SIZE), 0);
            }
            Console.WriteLine("Initialized blocks!");

            return textureAtlas;
        }

        public static Block GetBlock(int id)
        {
            if (id < 0 || id >= AllBlocks.Count)
                return AllBlocks[0];
            return AllBlocks[id];
        }

        public static Block GetBlock(string name)
        {
            foreach (var block in AllBlocks)
            {
                if (block.name == name)
                    return block;
            }
            return AllBlocks[0];
        }
    }

    public enum CollisionType
    {
        None,
        Solid,
        Liquid,
    }

    public struct Block
    {
        public Block() { }

        public int id {  get; set; }
        public required string name { get; set; }
        public CollisionType collisionType { get; set; }
        public int lightLevel { get; set; } = 1;
        public bool isTransparent { get; set; } = false;
        public bool isBg { get; set; } = false;
        public int lightSource { get; set; } = 1;

        public static bool operator ==(Block a, Block b)
        {
            return a.id == b.id;
        }

        public static bool operator !=(Block a, Block b)
        {
            return a.id != b.id;
        }

        public override bool Equals(object? obj)
        {
            return obj != null && obj is Block block && id == block.id;
        }
    }
}
