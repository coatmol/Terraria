namespace Terraria.game
{
    public static class Commands
    {
        private static Dictionary<string, Action<string[], PlayerCharacter>> CommandsList = new Dictionary<string, Action<string[], PlayerCharacter>>()
           {
               {"fly", Fly },
               {"noclip", NoClip }
           };

        public static void ExecuteCommand(string cmd, PlayerCharacter player)
        {
            string[] command = FormatCommand(cmd);
            string[] args = command.Skip(1).ToArray();
            if (CommandsList.ContainsKey(command[0]))
                CommandsList[command[0]](args, player);
            else
                Console.WriteLine($"Command {command[0]} not found");
        }

        private static string[] FormatCommand(string cmd)
        {
            return cmd.Replace("/", "").ToLower().Split(' ');
        }

        private static void Fly(string[] args, PlayerCharacter player)
        {
            player.canFly = !player.canFly;
            Console.WriteLine("Executed fly");
        }

        private static void NoClip(string[] args, PlayerCharacter player)
        {
            player.canCollide = !player.canCollide;
            Console.WriteLine("Executed no clip");
        }
    }
}
