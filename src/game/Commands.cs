namespace Terraria.game
{
    public static class Commands
    {
        private static Dictionary<string, Action<string[], PlayerCharacter>?> CommandsList = new Dictionary<string, Action<string[], PlayerCharacter>?>()
            {
                {"fly", Fly },
                {"noclip", NoClip },
                {"place", null } //set which block to place. TEMPORARY
            };

        public static void ExecuteCommand(string cmd, PlayerCharacter player)
        {
            string[] command = FormatCommand(cmd);
            string[] args = command.Skip(1).ToArray();
            if (CommandsList.TryGetValue(command[0], out var action))
            {
                if (action != null)
                    action(args, player);

                EventManager.CallEvent(EventManager.EventType.CommandExecuted, new CommandEventArgs
                {
                    command = command[0],
                    args = args
                });
            }
            else
                Console.WriteLine($"Command {command[0]} not found or not implemented");
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

    public struct CommandEventArgs
    {
        public string command { get; set; }
        public string[] args { get; set; }
    }
}
