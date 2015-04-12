namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Sandbox.Common.ObjectBuilders;
    using Sandbox.ModAPI;
    using VRageMath;

    public class CommandObjectsCollect : ChatCommand
    {
        private Queue<Action> _workQueue = new Queue<Action>();

        public CommandObjectsCollect()
            : base(ChatCommandSecurity.Admin, "collectobjects", new[] { "/collectobjects" })
        {
        }

        public override void Help(bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/collectobjects <range>", "Collects any floating objects in <range> of the player to player's location.");
        }

        public override bool Invoke(string messageText)
        {
            if (messageText.StartsWith("/collectobjects ", StringComparison.InvariantCultureIgnoreCase))
            {
                var match = Regex.Match(messageText, @"/collectobjects\s{1,}(?<R>[+-]?((\d+(\.\d*)?)|(\.\d+)))", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var range = double.Parse(match.Groups["R"].Value, CultureInfo.InvariantCulture);

                    Vector3D destination;

                    if (MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.Parent == null)
                    {
                        var worldMatrix = MyAPIGateway.Session.Player.Controller.ControlledEntity.GetHeadMatrix(true, true, true);
                        destination = worldMatrix.Translation + worldMatrix.Forward * 1.5f; // Spawn meteor 1.5m in front of player for safety.
                    }
                    else
                    {
                        var worldMatrix = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.WorldMatrix;
                        destination = worldMatrix.Translation + worldMatrix.Forward * 1.5f + worldMatrix.Up * 0.5f; // Spawn ore 1.5m in front of player in cockpit for safety.
                    }

                    var sphere = new BoundingSphereD(destination, range);
                    var floatingList = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);
                    floatingList = floatingList.Where(e => (e is Sandbox.ModAPI.IMyFloatingObject) || (e is Sandbox.ModAPI.IMyCharacter)).ToList();

                    foreach (var item in floatingList)
                    {
                        // Check for null physics and IsPhantom, to prevent picking up primitives.
                        if (item.Physics != null && !item.Physics.IsPhantom)
                        {
                            if (item is Sandbox.ModAPI.IMyCharacter)
                            {
                                var character = item.GetObjectBuilder() as MyObjectBuilder_Character;
                                if (!character.Health.HasValue || character.Health.Value > 0) // ignore living players
                                    continue;
                            }

                            if (!MyAPIGateway.Multiplayer.MultiplayerActive)
                            {
                                item.Physics.ClearSpeed();
                                // Need queue the objects, and relocate them over a number of frames, otherwise if they 
                                // are all moved simultaneously to the same point in space, they will become stuck.
                                _workQueue.Enqueue(delegate() { item.SetPosition(destination); });
                            }
                            else
                            {
                                ConnectionHelper.SendMessageToServer(ConnectionHelper.ConnectionKeys.StopAndMove, string.Format("{0}:{1}:{2}:{3}", item.EntityId, destination.X, destination.Y, destination.Z));
                            }
                        }
                    }

                    return true;
                }
            }

            return false;
        }

        public override void UpdateBeforeSimulation100()
        {
            if (_workQueue.Count > 0)
            {
                var action = _workQueue.Dequeue();
                action.Invoke();
            }
        }
    }
}