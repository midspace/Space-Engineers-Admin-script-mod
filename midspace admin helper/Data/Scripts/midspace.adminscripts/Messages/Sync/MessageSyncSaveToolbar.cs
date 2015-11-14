namespace midspace.adminscripts.Messages.Sync
{
    using System;
    using midspace.adminscripts.Messages.Communication;
    using ProtoBuf;
    using Sandbox.Common.ObjectBuilders;
    using Sandbox.Game.Entities;
    using Sandbox.ModAPI;

    [ProtoContract]
    public class MessageSyncSaveToolbar : MessageBase
    {
        [ProtoMember(1)]
        public long EntityId;

        [ProtoMember(2)]
        public string NewToolBar;

        public override void ProcessClient()
        {
            // no actions.
        }

        public override void ProcessServer()
        {
            if (!MyAPIGateway.Entities.EntityExists(EntityId))
            {
                MessageClientTextMessage.SendMessage(SenderSteamId, "Cockpit", "Failed to update.");
                return;
            }

            var terminalBlock = MyAPIGateway.Entities.GetEntityById(EntityId) as IMyTerminalBlock;
            if (terminalBlock == null)
            {
                MessageClientTextMessage.SendMessage(SenderSteamId, "Cockpit", "Failed to update.");
                return;
            }

            var controller = terminalBlock as MyShipController;
            if (controller == null)
            {
                MessageClientTextMessage.SendMessage(SenderSteamId, "Cockpit", "Failed to update.");
                return;
            }

            // Security checks. Only the player owning the block can update the toolbar.
            var player = MyAPIGateway.Players.FindPlayerBySteamId(SenderSteamId);
            if (player == null)
            {
                MessageClientTextMessage.SendMessage(SenderSteamId, "Cockpit", "Invalid player.");
                return;
            }

            if (terminalBlock.OwnerId != 0 && terminalBlock.OwnerId != player.PlayerID)
            {
                MessageClientTextMessage.SendMessage(SenderSteamId, "Cockpit", "You cannot update the cockpit. You must own it.");
                return;
            }

            try
            {
                var oldName = terminalBlock.CustomName; // CustomName is the only thing that doesn't properly save as the call to Init() will try to update it with a unique name.

                MyObjectBuilder_ShipController cockpitBuilder = (MyObjectBuilder_ShipController)controller.GetObjectBuilderCubeBlock(false);
                cockpitBuilder.Toolbar = MyAPIGateway.Utilities.SerializeFromXML<MyObjectBuilder_Toolbar>(NewToolBar);
                controller.Init(cockpitBuilder, controller.CubeGrid);
                terminalBlock.SetCustomName(oldName);
                MessageClientTextMessage.SendMessage(SenderSteamId, "Cockpit", "Toolbar updated!");
            }
            catch (Exception)
            {

                MessageClientTextMessage.SendMessage(SenderSteamId, "Cockpit", "Toolbar failed to save.");
            }
        }
    }
}
