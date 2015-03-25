namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using Sandbox.ModAPI;
    using Sandbox.Common.ObjectBuilders;

    public class CommandAsteroidSpread : ChatCommand
    {
        /// <summary>
        /// A grand idea to move asteroids. However the API does not support moving asteroids.
        /// I'm not sure if the API ever will. KeenSWH may just implement the feature in game and then this will become redundant.
        /// </summary>
        public CommandAsteroidSpread()
            : base(ChatCommandSecurity.Admin, ChatCommandFlag.Experimental, "spreadasteroids", new[] { "/spreadasteroids" })
        {
        }

        public override void Help(bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/spreadasteroids <distance>", "Will spread all asteroids out the specified <distance> from origin (0,0,0). + or - to move out or in.");
        }

        public override bool Invoke(string messageText)
        {
            if (messageText.StartsWith("/spreadasteroids", StringComparison.InvariantCultureIgnoreCase))
            {
                string asteroidName = null;
                var match = Regex.Match(messageText, @"/spreadasteroids\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    asteroidName = match.Groups["Key"].Value;
                }

                var currentAsteroidList = new List<IMyVoxelMap>();
                MyAPIGateway.Session.VoxelMaps.GetInstances(currentAsteroidList, v => asteroidName == null || v.StorageName.IndexOf(asteroidName, StringComparison.InvariantCultureIgnoreCase) >= 0);

                
                MyAPIGateway.Utilities.ShowMessage("Count", currentAsteroidList.Count.ToString());
                var index = 1;
                foreach (var voxelMap in currentAsteroidList)
                {

                    var entity = (IMyEntity)voxelMap;
                    var p = entity.GetPosition();
                    p.X += 10;
                    entity.SetPosition(p);

                    //voxelMap.PositionLeftBottomCorner.X = 10;

                    // TODO: Asteroids need to visually move in game.

                    var tempList = new List<MyObjectBuilder_EntityBase> { voxelMap.GetObjectBuilder() };

                    MyAPIGateway.Entities.RemapObjectBuilderCollection(tempList);
                    //tempList.ForEach(grid => MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(grid));
                    MyAPIGateway.Multiplayer.SendEntitiesCreated(tempList);

                    //MyAPIGateway.Entities.RegisterForDraw(entity);
                    //MyAPIGateway.Entities.dr
                    //MyAPIGateway.Entities.RegisterForUpdate(entity);
                    //MyAPIGateway.Entities.RemapObjectBuilderCollection
                    //MyAPIGateway.Multiplayer.SendEntitiesCreated
                    //MyAPIGateway.Entities.up

                    //entity.SyncObject.UpdatePosition();
                    //voxelMap.PositionLeftBottomCorner
                    
                    //MyAPIGateway.Utilities.ShowMessage(string.Format("#{0}", index++), voxelMap.Storage.Name);
                }

                return true;
            }

            return false;
        }
    }
}
