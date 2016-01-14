namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Sandbox.ModAPI;
    using VRage.ModAPI;

    public class CommandPlanetDelete : ChatCommand
    {
        public CommandPlanetDelete()
            : base(ChatCommandSecurity.Admin, "deleteplanet", new[] { "/deleteplanet", "/delplanet" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/deleteplanet <#>", "Deletes the specified <#> planet.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            if (messageText.Equals("/deleteplanet", StringComparison.InvariantCultureIgnoreCase) ||
                messageText.Equals("/delplanet", StringComparison.InvariantCultureIgnoreCase))
            {
                var entity = Support.FindLookAtEntity(MyAPIGateway.Session.ControlledObject, false, false, false, false, true, false);
                if (entity != null)
                {
                    var planetEntity = entity as Sandbox.Game.Entities.MyPlanet;
                    return DeletePlanet(planetEntity);
                }

                MyAPIGateway.Utilities.ShowMessage("deleteplanet", "No planet targeted.");
                return true;
            }

            var match = Regex.Match(messageText, @"/((delplanet)|(deleteplanet))\s+(?<Key>.+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var planetName = match.Groups["Key"].Value;

                var currentPlanetList = new List<IMyVoxelBase>();
                MyAPIGateway.Session.VoxelMaps.GetInstances(currentPlanetList, v => v is Sandbox.Game.Entities.MyPlanet && v.StorageName.IndexOf(planetName, StringComparison.InvariantCultureIgnoreCase) >= 0);

                if (currentPlanetList.Count == 1)
                {
                    DeletePlanet(currentPlanetList.First());
                    return true;
                }
                else if (currentPlanetList.Count == 0)
                {
                    int index;
                    if (planetName.Substring(0, 1) == "#" && int.TryParse(planetName.Substring(1), out index) && index > 0 && index <= CommandPlanetsList.PlanetCache.Count && CommandPlanetsList.PlanetCache[index - 1] != null)
                    {
                        DeletePlanet(CommandPlanetsList.PlanetCache[index - 1]);
                        CommandPlanetsList.PlanetCache[index - 1] = null;
                        return true;
                    }
                }
                else if (currentPlanetList.Count > 1)
                {
                    MyAPIGateway.Utilities.ShowMessage("deleteplanet", "{0} Planets match that name.", currentPlanetList.Count);
                    return true;
                }

                MyAPIGateway.Utilities.ShowMessage("deleteplanet", "Planet name not found.");
                return true;
            }

            return false;
        }

        private bool DeletePlanet(IMyVoxelBase planetEntity)
        {
            if (planetEntity == null)
                return false;
            var name = planetEntity.StorageName;
            planetEntity.SyncObject.SendCloseRequest();
            MyAPIGateway.Utilities.ShowMessage("planet", "'{0}' deleted.", name);
            return true;
        }
    }
}
