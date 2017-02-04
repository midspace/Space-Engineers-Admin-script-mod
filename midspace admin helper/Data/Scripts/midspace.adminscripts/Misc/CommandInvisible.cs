namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Messages;
    using Sandbox.ModAPI;
    using VRage.Game.ModAPI;
    using VRage.ObjectBuilders;

    public class CommandInvisible : ChatCommand
    {
        /// <summary>
        /// Doesn't currently work.
        /// </summary>
        public CommandInvisible()
            : base(ChatCommandSecurity.Admin, "invisible", new[] { "/invisible" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/invisible <on|off>", "Turns invisible mode on or off for you only. Other players will still see your shadow, chest thrusters, and held weapons. Automated weapons will still target you.");
        }

        MyPersistentEntityFlags2? store = null;

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            var strings = messageText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            bool? state = null;

            if (strings.Contains("on", StringComparer.InvariantCultureIgnoreCase)
                    || strings.Contains("1", StringComparer.InvariantCultureIgnoreCase))
                state = true;

            if (strings.Contains("off", StringComparer.InvariantCultureIgnoreCase)
                || strings.Contains("0", StringComparer.InvariantCultureIgnoreCase))
                state = false;

            if (state.HasValue)
            {
                if (!store.HasValue)
                    store = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.PersistentFlags;

                if (!MyAPIGateway.Multiplayer.MultiplayerActive)
                {
                    MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.Visible = !state.Value;

                    // Setting the Visible only changes how the player skin appears.
                    // Shadows, chest thrusters, and held weapons are still visible to other players, as well as automated weapons will target the player.
                    // The following is test code in an attempt to also make those parts invisible.

                    //if (state.Value)
                    //    MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.PersistentFlags = MyPersistentEntityFlags2.None;
                    //else
                    //    MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.PersistentFlags = store.Value;
                    //MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.Physics.Enabled = state.Value;
                    //MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.Physics.Flags = Sandbox.Engine.Physics.RigidBodyFlag.
                }
                else
                {
                    ConnectionHelper.SendMessageToAll(new MessageSyncInvisible() { PlayerId = MyAPIGateway.Session.Player.IdentityId, VisibleState = !state.Value });
                    return true;
                }
            }

            // Display the current state.
            MyAPIGateway.Utilities.ShowMessage("Invisible", !MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.Visible ? "On" : "Off");
            return true;
        }

        public static void ProcessCommon(MessageSyncInvisible message)
        {
            Logger.Debug("Player Visible Change {0}", message.PlayerId, message.VisibleState);

            var players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players, p => p != null && p.IdentityId == message.PlayerId);
            IMyPlayer player = players.FirstOrDefault();

            if (player != null && player.Controller.ControlledEntity != null)
            {
                player.Controller.ControlledEntity.Entity.Visible = message.VisibleState;

                // display the state back to the original caller.
                if (!MyAPIGateway.Utilities.IsDedicated && MyAPIGateway.Session.Player != null && MyAPIGateway.Session.Player.IdentityId == message.PlayerId)
                    MyAPIGateway.Utilities.ShowMessage("Invisible", !MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.Visible ? "On" : "Off");
            }
        }
    }
}
