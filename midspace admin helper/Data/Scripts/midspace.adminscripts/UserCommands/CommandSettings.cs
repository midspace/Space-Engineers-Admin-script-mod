namespace midspace.adminscripts
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Sandbox.Common.ObjectBuilders;
    using Sandbox.ModAPI;
    using VRage.Game;
    using VRage.Library.Utils;

    public class CommandSettings : ChatCommand
    {
        public CommandSettings()
            : base(ChatCommandSecurity.User, "settings", new[] { "/settings" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/settings", "Will display all current game settings.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            var info = new StringBuilder();
            var list = new List<string>();
            var yes = Localize.GetResource("Yes");
            var no = Localize.GetResource("No");

            info.AppendFormat("{0}: {1}\r\n", Localize.GetResource("Name"), MyAPIGateway.Session.Name);
            info.AppendFormat("{0}: {1}\r\n", Localize.GetResource("WorldSettings_Description"), MyAPIGateway.Session.Description);
            info.AppendFormat("{0}: {1:%d} days {1:hh\\:mm\\:ss}\r\n", "Session Time", MyAPIGateway.Session.ElapsedPlayTime); // This is the local session, not the server.
            info.AppendFormat("{0}: {1:%d} days {1:hh\\:mm\\:ss}\r\n", "Game Time", MyAPIGateway.Session.ElapsedGameTime()); // Total game time. Still in debate about sync with the server.

            info.AppendFormat("\r\n");

            var gameMode = "Unknown";
            switch (MyAPIGateway.Session.SessionSettings.GameMode)
            {
                case MyGameModeEnum.Creative: gameMode = Localize.GetResource("WorldSettings_GameModeCreative"); break;
                case MyGameModeEnum.Survival: gameMode = Localize.GetResource("WorldSettings_GameModeSurvival"); break;
            }
            info.AppendFormat("{0}: {1}\r\n", Localize.GetResource("WorldSettings_GameMode"), gameMode);


            var onlineMode = "Unknown";
            switch (MyAPIGateway.Session.OnlineMode)
            {
                case MyOnlineModeEnum.FRIENDS: onlineMode = Localize.GetResource("WorldSettings_OnlineModeFriends"); break;
                case MyOnlineModeEnum.OFFLINE: onlineMode = Localize.GetResource("WorldSettings_OnlineModeOffline"); break;
                case MyOnlineModeEnum.PRIVATE: onlineMode = Localize.GetResource("WorldSettings_OnlineModePrivate"); break;
                case MyOnlineModeEnum.PUBLIC: onlineMode = Localize.GetResource("WorldSettings_OnlineModePublic"); break;
            }
            info.AppendFormat("{0}: {1}\r\n", Localize.GetResource("WorldSettings_OnlineMode"), onlineMode);
            info.AppendFormat("{0}: {1}\r\n", Localize.GetResource("MaxPlayers"), MyAPIGateway.Session.MaxPlayers);

            var environmentHostility = "Unknown";
            switch (MyAPIGateway.Session.EnvironmentHostility)
            {
                case MyEnvironmentHostilityEnum.CATACLYSM: environmentHostility = Localize.GetResource("WorldSettings_EnvironmentHostilityCataclysm"); break;
                case MyEnvironmentHostilityEnum.CATACLYSM_UNREAL: environmentHostility = Localize.GetResource("WorldSettings_OnlineModeOffline"); break;
                case MyEnvironmentHostilityEnum.NORMAL: environmentHostility = Localize.GetResource("WorldSettings_EnvironmentHostilityCataclysmUnreal"); break;
                case MyEnvironmentHostilityEnum.SAFE: environmentHostility = Localize.GetResource("WorldSettings_EnvironmentHostilitySafe"); break;
            }
            info.AppendFormat("{0}: {1}\r\n", Localize.GetResource("WorldSettings_EnvironmentHostility"), environmentHostility);
            info.AppendFormat("{0}: {1}\r\n", Localize.GetResource("WorldSettings_AutoSave"), MyAPIGateway.Session.SessionSettings.AutoSave ? yes : no);
            //info.AppendFormat("Auto Save?? Test: {0} {1} {2}\r\n", MyAPIGateway.Session.AutoSaveInMinutes, MyAPIGateway.Session.SessionSettings.AutoSaveInMinutes, MyAPIGateway.Session.GetCheckpoint("null").Settings.AutoSaveInMinutes);
            //info.AppendFormat("Auto Save In Minutes: {0}\r\n", MyAPIGateway.Session.AutoSaveInMinutes); // Dedicated Server.
            info.AppendFormat("{0}: {1}\r\n", Localize.GetResource("WorldSettings_ScenarioEditMode"), MyAPIGateway.Session.SessionSettings.ScenarioEditMode ? yes : no);

            info.AppendFormat("\r\n");

            info.AppendFormat("{0}: x {1}\r\n", Localize.GetResource("WorldSettings_InventorySize"), MyAPIGateway.Session.InventoryMultiplier);
            info.AppendFormat("{0}: x {1}\r\n", Localize.GetResource("WorldSettings_AssemblerEfficiency"), MyAPIGateway.Session.AssemblerEfficiencyMultiplier);
            info.AppendFormat("{0}: x {1}\r\n", Localize.GetResource("WorldSettings_RefinerySpeed"), MyAPIGateway.Session.RefinerySpeedMultiplier);
            info.AppendFormat("{0}: x {1}\r\n", Localize.GetResource("WorldSettings_WelderSpeed"), MyAPIGateway.Session.WelderSpeedMultiplier);
            info.AppendFormat("{0}: x {1}\r\n", Localize.GetResource("WorldSettings_GrinderSpeed"), MyAPIGateway.Session.GrinderSpeedMultiplier);
            info.AppendFormat("{0}: {1:##,##0}\r\n", Localize.GetResource("MaxFloatingObjects"), MyAPIGateway.Session.MaxFloatingObjects);
            info.AppendFormat("{0}: {1:##,##0}\r\n", Localize.GetResource("MaxBackupSaves"), MyAPIGateway.Session.MaxBackupSaves);
            
            if (MyAPIGateway.Session.SessionSettings.WorldSizeKm == 0)
                info.AppendFormat("{0}: {1}\r\n", Localize.GetResource("WorldSettings_LimitWorldSize"), Localize.GetResource("WorldSettings_WorldSizeUnlimited"));
            else
                info.AppendFormat("{0}: {1:##,##0} Km\r\n", Localize.GetResource("WorldSettings_LimitWorldSize"), MyAPIGateway.Session.SessionSettings.WorldSizeKm);
            info.AppendFormat("{0}: x {1}\r\n", Localize.GetResource("WorldSettings_RespawnShipCooldown"), MyAPIGateway.Session.SessionSettings.SpawnShipTimeMultiplier);
            info.AppendFormat("{0}: {1:##,###} m\r\n", Localize.GetResource("WorldSettings_ViewDistance"), MyAPIGateway.Session.SessionSettings.ViewDistance);
            info.AppendFormat("{0}: {1}\r\n", Localize.GetResource("WorldSettings_EnableSunRotation"), MyAPIGateway.Session.SessionSettings.EnableSunRotation ? yes : no);
            info.AppendFormat("{0}: {1:N} minutes\r\n", Localize.GetResource("SunRotationPeriod"), MyAPIGateway.Session.SessionSettings.SunRotationIntervalMinutes);

            info.AppendFormat("\r\n");

            list.Add(string.Format("{0}: {1}", Localize.GetResource("WorldSettings_AutoHealing"), MyAPIGateway.Session.AutoHealing ? yes : no));
            list.Add(string.Format("{0}: {1}", Localize.GetResource("WorldSettings_EnableCopyPaste"), MyAPIGateway.Session.EnableCopyPaste ? yes : no));
            //list.Add(string.Format("{0}: {1}", Localize.GetResource("WorldSettings_ClientCanSave"), MyAPIGateway.Session.ClientCanSave ? yes : no)); // Obsolete.
            list.Add(string.Format("{0}: {1}", Localize.GetResource("WorldSettings_EnableWeapons"), MyAPIGateway.Session.WeaponsEnabled ? yes : no));
            list.Add(string.Format("{0}: {1}", Localize.GetResource("WorldSettings_RemoveTrash"), MyAPIGateway.Session.SessionSettings.RemoveTrash ? yes : no));
            list.Add(string.Format("{0}: {1}", Localize.GetResource("World_Settings_EnableOxygen"), MyAPIGateway.Session.SessionSettings.EnableOxygen ? yes : no));
            list.Add(string.Format("{0}: {1}", Localize.GetResource("World_Settings_EnableOxygenPressurization"), MyAPIGateway.Session.SessionSettings.EnableOxygenPressurization ? yes : no));
            list.Add(string.Format("{0}: {1}", Localize.GetResource("WorldSettings_DisableRespawnShips"), MyAPIGateway.Session.SessionSettings.DisableRespawnShips ? yes : no));
            list.Add(string.Format("{0}: {1}", Localize.GetResource("WorldSettings_EnableJetpack"), MyAPIGateway.Session.SessionSettings.EnableJetpack ? yes : no));
            list.Add(string.Format("{0}: {1}", Localize.GetResource("WorldSettings_EnableVoxelDestruction"), MyAPIGateway.Session.SessionSettings.EnableVoxelDestruction ? yes : no));
            list.Add(string.Format("{0}: {1}", Localize.GetResource("WorldSettings_RespawnShipDelete"), MyAPIGateway.Session.SessionSettings.RespawnShipDelete ? yes : no));
            list.Add(string.Format("{0}: {1}", Localize.GetResource("WorldSettings_ShowPlayerNamesOnHud"), MyAPIGateway.Session.SessionSettings.ShowPlayerNamesOnHud ? yes : no));
            list.Add(string.Format("{0}: {1}", Localize.GetResource("WorldSettings_ThrusterDamage"), MyAPIGateway.Session.SessionSettings.ThrusterDamage ? yes : no));
            list.Add(string.Format("{0}: {1}", Localize.GetResource("WorldSettings_EnableCargoShips"), MyAPIGateway.Session.SessionSettings.CargoShipsEnabled ? yes : no));
            list.Add(string.Format("{0}: {1}", Localize.GetResource("WorldSettings_EnableIngameScripts"), MyAPIGateway.Session.SessionSettings.EnableIngameScripts ? yes : no));
            list.Add(string.Format("{0}: {1}", Localize.GetResource("WorldSettings_Enable3rdPersonCamera"), MyAPIGateway.Session.SessionSettings.Enable3rdPersonView ? yes : no));
            list.Add(string.Format("{0}: {1}", Localize.GetResource("WorldSettings_SpawnWithTools"), MyAPIGateway.Session.SessionSettings.SpawnWithTools ? yes : no));
            list.Add(string.Format("{0}: {1}", Localize.GetResource("WorldSettings_EnableDrones"), MyAPIGateway.Session.SessionSettings.EnableDrones ? yes : no));
            list.Add(string.Format("{0}: {1}", Localize.GetResource("WorldSettings_EnableSpectator"), MyAPIGateway.Session.SessionSettings.EnableSpectator ? yes : no));
            list.Add(string.Format("{0}: {1}", Localize.GetResource("WorldSettings_PermanentDeath"), MyAPIGateway.Session.SessionSettings.PermanentDeath.HasValue ? (MyAPIGateway.Session.SessionSettings.PermanentDeath.Value ? yes : no) : no));
            list.Add(string.Format("{0}: {1}", Localize.GetResource("WorldSettings_DestructibleBlocks"), MyAPIGateway.Session.SessionSettings.DestructibleBlocks ? yes : no));
            list.Add(string.Format("{0}: {1}", Localize.GetResource("WorldSettings_EnableToolShake"), MyAPIGateway.Session.SessionSettings.EnableToolShake ? yes : no));
            list.Add(string.Format("{0}: {1}", Localize.GetResource("WorldSettings_Encounters"), MyAPIGateway.Session.SessionSettings.EnableEncounters ? yes : no));
            list.Add(string.Format("{0}: {1}", Localize.GetResource("WorldSettings_EnableConvertToStation"), MyAPIGateway.Session.SessionSettings.EnableConvertToStation ? yes : no));
            list.Add(string.Format("{0}: {1}", Localize.GetResource("WorldSettings_EnableWolfs"), MyAPIGateway.Session.SessionSettings.EnableWolfs.HasValue? (MyAPIGateway.Session.SessionSettings.EnableWolfs.Value ? yes : no) : no));
            list.Add(string.Format("{0}: {1}", Localize.GetResource("WorldSettings_EnableSpiders"), MyAPIGateway.Session.SessionSettings.EnableSpiders.HasValue ? (MyAPIGateway.Session.SessionSettings.EnableSpiders.Value ? yes : no) : no));
            list.Add(string.Format("{0}: {1}", Localize.GetResource("WorldSettings_StartInRespawnScreen"), MyAPIGateway.Session.SessionSettings.StartInRespawnScreen ? yes : no));
            list.Add(string.Format("{0}: {1}", "Maximum Drones", MyAPIGateway.Session.SessionSettings.MaxDrones));
            list.Add(string.Format("{0}: {1}", Localize.GetResource("WorldSettings_SoundMode") + " " + Localize.GetResource("WorldSettings_RealisticSound"), MyAPIGateway.Session.SessionSettings.RealisticSound ? yes : no));
//#if !STABLE
//            list.Add(string.Format("{0}: {1}", Localize.GetResource("WorldSettings_StationVoxelSupport"), MyAPIGateway.Session.SessionSettings.StationVoxelSupport ? yes : no));
//#endif

            // add the remaining settings as a sorted list (according to the localizaed labels).
            foreach (var str in list.OrderBy(e => e))
                info.AppendLine(str);

            info.AppendFormat("\r\n");

            var mods = MyAPIGateway.Session.GetCheckpoint("null").Mods;
            info.AppendFormat("{0}: {1:#,###0}\r\n", Localize.GetResource("WorldSettings_Mods"), mods.Count);
            foreach (var mod in mods.OrderBy(e => e.FriendlyName))
                info.AppendFormat("#{0} : '{1}'\r\n", mod.PublishedFileId, mod.FriendlyName);

            MyAPIGateway.Utilities.ShowMissionScreen("Game Settings", "", " ", info.ToString());


            // Other labels or settings unused or obsolete.
            // WorldSettings_EnablePlanets
            // WorldSettings_EnableFlora    MyAPIGateway.Session.SessionSettings
            // WorldSettings_FloraDensity   MyAPIGateway.Session.SessionSettings.FloraDensity
            //                              MyAPIGateway.Session.SessionSettings.HackSpeedMultiplier
            // WorldSettings_GameScenario
            // WorldSettings_Battle         MyAPIGateway.Session.SessionSettings.Battle
            // WorldSettings_FriendlyFire
            // WorldSettings_GameStyle
            // WorldSettings_Physics
            // WorldSettings_SoundInSpace
            // WorldSettings_SoundMode      MyAPIGateway.Session.SessionSettings.RealisticSound
            //                              MyAPIGateway.Session.SessionSettings.EnableStructuralSimulation
            //                              MaxActiveFracturePieces
            //                              PhysicsIterations
            //                              RealisticSound

            return true;
        }
    }
}
