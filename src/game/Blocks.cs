namespace Terraria.game
{
    public static class Blocks
    {
        public static Block[] AllBlocks = new Block[]
        {
            new Block { Id = -1, Name = "Air" },
            new Block { Id = 0, Name = "Grass" },
            new Block { Id = 1, Name = "Dirt" },
            new Block { Id = 2, Name = "Stone" },
        };

        public static Block GetBlock(int id)
        {
            if (id < 0 || id >= AllBlocks.Length)
                return AllBlocks[0];
            return AllBlocks[id];
        }

        public static Block GetBlock(string name)
        {
            foreach (var block in AllBlocks)
            {
                if (block.Name == name)
                    return block;
            }
            return AllBlocks[0];
        }
    }

    public struct Block
    {
        public int Id; // Slot on Texture Atlas
        public string Name;

        public static bool operator ==(Block a, Block b)
        {
            return a.Id == b.Id;
        }

        public static bool operator !=(Block a, Block b)
        {
            return a.Id != b.Id;
        }
    }
}
