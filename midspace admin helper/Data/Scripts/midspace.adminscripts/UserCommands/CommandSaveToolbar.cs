namespace midspace.adminscripts
{
    using midspace.adminscripts.Messages.Sync;
    using Sandbox.Common.ObjectBuilders;
    using Sandbox.Game.Entities;
    using Sandbox.ModAPI;
    using IMyShipController = Sandbox.ModAPI.Ingame.IMyShipController;

    /// <summary>
    /// Saves the player's current cockpit toolbar back to the server.
    /// This command had to run on both Client and Server. Taking the Client's toolbar, and writing it back to the server.
    /// </summary>
    public class CommandSaveToolbar : ChatCommand
    {
        public CommandSaveToolbar()
            : base(ChatCommandSecurity.User, ChatCommandFlag.MultiplayerOnly | ChatCommandFlag.Client, "savetoolbar", new[] { "/savetoolbar", "/savecockpit" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/savetoolbar", "Saves the toolbar in the current cockpit.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            var cockpit = MyAPIGateway.Session.ControlledObject as IMyShipController;

            if (cockpit == null)
            {
                MyAPIGateway.Utilities.ShowMessage("savetoolbar", "You must occupy the cockpit to be saved.");
                return true;
            }

            var controller = (MyShipController)cockpit;
            MyObjectBuilder_ShipController cockpitBuilder = (MyObjectBuilder_ShipController)controller.GetObjectBuilderCubeBlock();
            var xmlString = MyAPIGateway.Utilities.SerializeToXML(cockpitBuilder.Toolbar);
            ConnectionHelper.SendMessageToServer(new MessageSyncSaveToolbar { EntityId = cockpit.EntityId, NewToolBar = xmlString });

            return true;
        }
    }
}
