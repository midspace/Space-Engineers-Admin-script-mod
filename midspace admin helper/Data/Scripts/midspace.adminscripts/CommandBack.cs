using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using VRageMath;

namespace midspace.adminscripts
{
    public class CommandBack : ChatCommand
    {

        public static readonly List<Vector3D> TeleportHistory = new List<Vector3D>();
        private static int CurrentIndex;

        public CommandBack()
            : base(ChatCommandSecurity.Admin, "back", new String[] { "/back" })
        {

        }

        public override void Help()
        {
            MyAPIGateway.Utilities.ShowMessage("/back", "Teleports you back to your previous location.");
        }

        public override bool Invoke(string messageText)
        {
            if (messageText.Equals("/back", StringComparison.InvariantCultureIgnoreCase))
            {
                if (TeleportHistory.Count == 0)
                {
                    MyAPIGateway.Utilities.ShowMessage("CommandBack", "No entry in history, perform a teleport first.");
                    return true;
                }

                //if we havn't initialized the index yet or we reached the bottom of the history, we start again from the top
                if (CurrentIndex == null || CurrentIndex == 0)
                    CurrentIndex = TeleportHistory.Count - 1;
                else
                    CurrentIndex -= 1;

                var position = TeleportHistory[CurrentIndex];
                //basically we perform a normal teleport without adding it to the history
                if (MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.Parent == null)
                {
                    // Move the player only.
                    MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.SetPosition(position);
                }
                else
                {
                    // Move the ship the player is piloting.
                    var cubeGrid = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.GetTopMostParent();
                    var grids = cubeGrid.GetAttachedGrids();
                    var worldOffset = position - MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.GetPosition();

                    foreach (var grid in grids)
                    {
                        grid.SetPosition(grid.GetPosition() + worldOffset);
                    }
                }
                return true;
            }
            return false;
        }

        public static void SaveTeleportInHistory(Vector3D position)
        {
            //add the position to the history and update the index so we start again from the top
            TeleportHistory.Add(position);
            CurrentIndex = TeleportHistory.Count - 1;
        }
    }
}
