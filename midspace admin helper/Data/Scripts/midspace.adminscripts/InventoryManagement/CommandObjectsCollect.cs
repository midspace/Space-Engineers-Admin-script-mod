namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Timers;
    using midspace.adminscripts.Messages.Sync;
    using Sandbox.ModAPI;
    using VRage.Game;
    using VRage.Game.ModAPI;
    using VRageMath;

    public class CommandObjectsCollect : ChatCommand
    {
        private readonly Queue<Action> _workQueue = new Queue<Action>();
        private Timer _timer100;
        private static CommandObjectsCollect _instance;

        public CommandObjectsCollect()
            : base(ChatCommandSecurity.Admin, "collectobjects", new[] { "/collectobjects" })
        {
            if (_instance == null)
                _instance = this;

            _timer100 = new Timer(100);
            _timer100.Elapsed += TimerOnElapsed100;
        }

        ~CommandObjectsCollect()
        {
            if (_timer100 != null)
            {
                _timer100.Stop();
                _timer100 = null;
            }
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/collectobjects <range>", "Collects any floating objects in <range> of the player to player's location.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
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
                        var worldMatrix = MyAPIGateway.Session.Player.Controller.ControlledEntity.GetHeadMatrix(true, true, false);
                        destination = worldMatrix.Translation + worldMatrix.Forward * 1.5f; // Spawn item 1.5m in front of player for safety.
                    }
                    else
                    {
                        var worldMatrix = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.WorldMatrix;
                        destination = worldMatrix.Translation + worldMatrix.Forward * 1.5f + worldMatrix.Up * 0.5f; // Spawn item 1.5m in front of player in cockpit for safety.
                    }

                    if (!MyAPIGateway.Multiplayer.MultiplayerActive)
                        CollectObjects(0, destination, range);
                    else
                        ConnectionHelper.SendMessageToServer(new MessageSyncFloatingObjects { Type = SyncFloatingObject.Collect, Position = destination, Range = range });
                    return true;
                }
            }

            return false;
        }

        public static void CollectObjects(ulong steamId, Vector3D destination, double range)
        {
            var sphere = new BoundingSphereD(destination, range);
            var floatingList = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);
            //floatingList = floatingList.Where(e => (e is Sandbox.ModAPI.IMyFloatingObject) || (e is Sandbox.ModAPI.IMyCharacter)).ToList();
            floatingList = floatingList.Where(e => (e is IMyFloatingObject) || (e is Sandbox.Game.Entities.MyInventoryBagEntity)).ToList();

            _instance._timer100.Stop();
            _instance._workQueue.Clear();
            for (var i = 0; i < floatingList.Count; i++)
            {
                var item = floatingList[i];

                // Check for null physics and IsPhantom, to prevent picking up primitives.
                if (item.Physics != null && !item.Physics.IsPhantom)
                {
                    if (item is IMyCharacter)
                    {
                        var character = item.GetObjectBuilder() as MyObjectBuilder_Character;
                        if (!character.Health.HasValue || character.Health.Value > 0) // ignore living players
                        {
                            // TODO: not working currently. It causes body duplicates?

                            //item.Physics.ClearSpeed();
                            //_workQueue.Enqueue(delegate() { item.SetPosition(destination); });
                        }
                    }
                    else if (item is IMyFloatingObject || item is Sandbox.Game.Entities.MyInventoryBagEntity)
                    {
                        // Need to queue the objects, and relocate them over a number of frames, otherwise if they 
                        // are all moved simultaneously to the same point in space, they will become stuck.

                        _instance._workQueue.Enqueue(delegate()
                        {
                            //item.SyncObject.UpdatePosition(); // causes Null exception.

                            if (item.Physics != null)
                                MessageSyncEntity.Process(item, SyncEntityType.Position | SyncEntityType.Stop, destination);
                        });
                    }
                }
            }
            if (_instance._workQueue.Count > 0)
                _instance._timer100.Start();
        }

        public override void UpdateBeforeSimulation100()
        {
            if (_workQueue.Count > 0)
                _workQueue.Dequeue().Invoke();
        }

        private void TimerOnElapsed100(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            // this is a temporary use of timer, until we restructure the ChatCommandLogic to encapsulate ChatCommandService on the server side.

            MyAPIGateway.Utilities.InvokeOnGameThread(delegate ()
            {
                if (_workQueue.Count == 0)
                    _instance._timer100.Stop();

                if (_workQueue.Count > 0)
                    _workQueue.Dequeue().Invoke();
            });
        }
    }
}