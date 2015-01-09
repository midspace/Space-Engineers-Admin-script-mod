namespace midspace.adminscripts
{
    using System;

    using Sandbox.ModAPI;

    public class CommandIdentify : ChatCommand
    {
        public CommandIdentify()
            : base(ChatCommandSecurity.Admin, "id", new[] { "/id" })
        {
        }

        public override void Help()
        {
            MyAPIGateway.Utilities.ShowMessage("/id", "Identifies the name of the object the player is looking at.");
        }

        public override bool Invoke(string messageText)
        {
            if (messageText.Equals("/id", StringComparison.InvariantCultureIgnoreCase))
            {
                var entity = Support.FindLookAtEntity(MyAPIGateway.Session.ControlledObject);
                if (entity != null)
                {
                    var displayName = entity.DisplayName;
                    if (entity is IMyVoxelMap)
                        displayName = ((IMyVoxelMap)entity).StorageName;

                    if (entity is IMyCubeGrid)
                    {
                        var cockpits = entity.FindWorkingCockpits();
                        // TODO: determine if any cockpits are occupied.
                    }

                    MyAPIGateway.Utilities.ShowMessage("ID", displayName);
                    return true;
                }
            }

            return false;
        }
    }
}
