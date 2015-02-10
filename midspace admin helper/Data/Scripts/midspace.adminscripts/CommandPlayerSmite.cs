namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Sandbox.ModAPI;
    using Sandbox.Common.ObjectBuilders;

    public class CommandPlayerSmite : ChatCommand
    {
        private readonly string[] _oreNames;

        public CommandPlayerSmite(string[] oreNames)
            : base(ChatCommandSecurity.Admin, "smite", new[] { "/smite" })
        {
            _oreNames = oreNames;
        }

        public override void Help()
        {
            MyAPIGateway.Utilities.ShowMessage("/smite <#>", "Drops meteor on the specified <#> player. Instant death in Survival mode. Cockpits do pretect a little, but can become collateral damage.");
        }

        public override bool Invoke(string messageText)
        {
            var match = Regex.Match(messageText, @"/smite\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var playerName = match.Groups["Key"].Value;
                var players = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(players, p => p != null);
                IMyPlayer selectedPlayer = null;

                var findPlayer = players.FirstOrDefault(p => p.DisplayName.Equals(playerName, StringComparison.InvariantCultureIgnoreCase));
                if (findPlayer != null)
                {
                    selectedPlayer = findPlayer;
                }

                int index;
                if (playerName.Substring(0, 1) == "#" && Int32.TryParse(playerName.Substring(1), out index) && index > 0 && index <= CommandPlayerStatus.IdentityCache.Count)
                {
                    var listplayers = new List<IMyPlayer>();
                    MyAPIGateway.Players.GetPlayers(listplayers, p => p.PlayerID == CommandPlayerStatus.IdentityCache[index - 1].PlayerId);
                    selectedPlayer = listplayers.FirstOrDefault();
                }

                if (selectedPlayer == null)
                {
                    MyAPIGateway.Utilities.ShowMessage("Smite", string.Format("No player named {0} found.", playerName));
                    return true;
                }

                MyAPIGateway.Utilities.ShowMessage("smiting", selectedPlayer.DisplayName);
                var worldMatrix = selectedPlayer.Controller.ControlledEntity.GetHeadMatrix(true, true, true);

                var meteor = new MyObjectBuilder_Meteor
                {
                    Item = new MyObjectBuilder_InventoryItem { Amount = 1, Content = new MyObjectBuilder_Ore { SubtypeName = _oreNames[0] } },
                    PersistentFlags = MyPersistentEntityFlags2.InScene, // Very important
                    PositionAndOrientation = new MyPositionAndOrientation
                    {
                        Position = (worldMatrix.Translation + worldMatrix.Up * 0.05f).ToSerializableVector3D(),
                        Forward = worldMatrix.Forward.ToSerializableVector3(),
                        Up = worldMatrix.Up.ToSerializableVector3(),
                    },
                    LinearVelocity = worldMatrix.Up * 200, // has to be faster than JetPack speed, otherwise it could be avoided.
                    // Update 01.052 seemed to have flipped the direction. It's Up instead of Down???
                    Integrity = 1
                };

                MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(meteor);

                return true;
            }

            return false;
        }
    }
}
