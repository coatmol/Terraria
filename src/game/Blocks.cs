namespace Terraria.game
{
    public static class Blocks
    {
        public static Block[] AllBlocks = new Block[]
        {
            new Block { id = -1, name = "Air", collisionType = CollisionType.Solid },
            new Block { id = 0, name = "Grass", collisionType = CollisionType.Solid },
            new Block { id = 1, name = "Dirt", collisionType = CollisionType.Solid },
            new Block { id = 2, name = "Stone", collisionType = CollisionType.Solid },
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
        public int id; // Slot on Texture Atlas
        public string name;
        public CollisionType collisionType;

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
