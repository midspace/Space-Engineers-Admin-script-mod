namespace midspace.adminscripts
{
    using System;

    using Sandbox.ModAPI;

    public class CommandVoxelClear : ChatCommand
    {
        public static bool ActiveVoxelDeleter { get; private set; }

        public CommandVoxelClear()
            : base(ChatCommandSecurity.Admin, "voxelclear", new[] { "/voxelclear" })
        {
        }

        public override void Help()
        {
            MyAPIGateway.Utilities.ShowMessage("/voxelclear [on|off]", "Voxel cell clearing, will remove single voxel cells that the tip of the hand drill touches.");
        }

        public override bool Invoke(string messageText)
        {
            // voxelclear [on] [off]
            if (messageText.StartsWith("/voxelclear ", StringComparison.InvariantCultureIgnoreCase))
            {
                var strings = messageText.Split(' ');
                if (strings.Length > 1)
                {
                    if (strings[1].Equals("on", StringComparison.InvariantCultureIgnoreCase) || strings[1].Equals("1", StringComparison.InvariantCultureIgnoreCase))
                    {
                        ActiveVoxelDeleter = true;
                        return true;
                    }

                    if (strings[1].Equals("off", StringComparison.InvariantCultureIgnoreCase) || strings[1].Equals("0", StringComparison.InvariantCultureIgnoreCase))
                    {
                        ActiveVoxelDeleter = false;
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
