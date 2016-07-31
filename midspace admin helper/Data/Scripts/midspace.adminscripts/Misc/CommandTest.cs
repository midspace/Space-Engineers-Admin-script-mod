namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Sandbox.Definitions;
    using Sandbox.ModAPI;
    using Sandbox.ModAPI.Interfaces;
    using VRage;
    using VRage.Game;
    using VRage.Game.ModAPI;
    using VRage.Library.Utils;
    using VRage.ModAPI;
    using VRage.ObjectBuilders;
    using VRage.Utils;
    using VRageMath;
    using IMyBlockGroup = Sandbox.ModAPI.Ingame.IMyBlockGroup;
    using IMyCameraController = VRage.Game.ModAPI.Interfaces.IMyCameraController;
    using IMyCargoContainer = Sandbox.ModAPI.Ingame.IMyCargoContainer;
    using IMyCockpit = Sandbox.ModAPI.Ingame.IMyCockpit;
    using IMyRemoteControl = Sandbox.ModAPI.Ingame.IMyRemoteControl;
    using IMyCameraBlock = Sandbox.ModAPI.Ingame.IMyCameraBlock;

    /// <summary>
    /// These command test various different things. It's not commented, because I just create them on the spur of the moment. 
    /// Some of them test to see how some API's work (because of the lack of documentation).
    /// Other's test to see if they are broken (as sometimes happens).
    /// Mainly these are used to check API's to see if there is a better, more effecient way of doing things.
    /// </summary>
    public class CommandTest : ChatCommand
    {
        public CommandTest()
            : base(ChatCommandSecurity.Admin, ChatCommandFlag.Client | ChatCommandFlag.Experimental, "test", new[] { "/test", "/test2", "/test3", "/test4", "/test5", "/test6", "/test7", "/test8A", "/test8B", "/test9", "/test10", "/test11", "/test12", "/test13", "/test14", "/test15", "/test16" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/test", "test commands");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            #region test

            if (messageText.Equals("/test", StringComparison.InvariantCultureIgnoreCase))
            {
                // for testing things.
                //MyAPIGateway.Utilities.ShowMessage("path", MyAPIGateway.Session.CurrentPath);

                //MyAPIGateway.Utilities.ShowMessage("size1", MyAPIGateway.Utilities.ConfigDedicated.SessionSettings.WorldSizeKm.ToString());
                //MyAPIGateway.Utilities.ShowMessage("size2", MyAPIGateway.Session.GetWorld().Checkpoint.Settings.WorldSizeKm.ToString());

                IMyConfigDedicated config = null;
                //List<string> admins = null;
                try
                {
                    config = MyAPIGateway.Utilities.ConfigDedicated;
                    config.Load();
                    //config.
                }
                catch (Exception)
                {
                    MyAPIGateway.Utilities.ShowMessage("Exception", "ConfigDedicated"); //ex.Message);
                }
                if (config != null)
                {
                    try
                    {
                        var players = new List<IMyPlayer>();
                        MyAPIGateway.Players.GetPlayers(players, p => p != null);
                        MyAPIGateway.Utilities.ShowMessage("Player Count", string.Format("{0}", players.Count));

                        var identities = new List<IMyIdentity>();
                        MyAPIGateway.Players.GetAllIdentites(identities);
                        MyAPIGateway.Utilities.ShowMessage("Identities Count", string.Format("{0}", identities.Count));

                        MyAPIGateway.Utilities.ShowMessage("Admin Count", string.Format("{0}", config.Administrators.Count));
                        //MyAPIGateway.Utilities.ShowMessage("WorldName", string.Format("{0}", config.WorldName));
                        //MyAPIGateway.Utilities.ShowMessage("WorldSize", string.Format("{0}", config.SessionSettings.WorldSizeKm));
                        MyAPIGateway.Utilities.ShowMessage("Mods Count", string.Format("{0}", config.Mods.Count));
                        //MyAPIGateway.Utilities.ShowMessage("IP", string.Format("{0}", config.IP));

                        var clients = MyAPIGateway.Session.GetWorld().Checkpoint.Clients;
                        MyAPIGateway.Utilities.ShowMessage("Client Count", clients == null ? "null" : string.Format("{0}", clients.Count));

                        if (clients != null)
                        {
                            var client = clients.FirstOrDefault(c => c.SteamId == MyAPIGateway.Multiplayer.MyId);
                            if (client != null)
                            {
                                MyAPIGateway.Utilities.ShowMessage("IsAdmin", string.Format("{0}", client.IsAdmin));
                            }
                        }
                    }
                    catch (Exception)
                    {
                        MyAPIGateway.Utilities.ShowMessage("Exception", "reading config"); //ex.Message);
                    }
                }
                return true;
            }

            #endregion

            #region test2

            if (messageText.Equals("/test2", StringComparison.InvariantCultureIgnoreCase))
            {
                // for testing things.
                var count = MyAPIGateway.Utilities.ConfigDedicated.Administrators.Count.ToString(CultureInfo.InvariantCulture);
                MyAPIGateway.Utilities.ShowMessage("Admins", string.Format("Count {0}", count));
                MyAPIGateway.Utilities.ShowMessage("Players", string.Format("Count {0}", MyAPIGateway.Players.Count));
                MyAPIGateway.Utilities.ShowMessage("MultiPlayers", string.Format("Count {0}", MyAPIGateway.Multiplayer.Players.Count));
                return true;
            }

            #endregion

            #region test3

            if (messageText.Equals("/test3", StringComparison.InvariantCultureIgnoreCase))
            {
                // for testing things.
                MyAPIGateway.Utilities.ShowMessage("MyId", "{0}", MyAPIGateway.Multiplayer.MyId);
                MyAPIGateway.Utilities.ShowMessage("SteamId", "{0}", MyAPIGateway.Session.Player.SteamUserId);
                MyAPIGateway.Utilities.ShowMessage("MyName", "{0}", MyAPIGateway.Multiplayer.MyName);
                MyAPIGateway.Utilities.ShowMessage("IsServer", "{0}", MyAPIGateway.Multiplayer.IsServer);
                MyAPIGateway.Utilities.ShowMessage("IsServerPlayer", "{0}", MyAPIGateway.Multiplayer.IsServerPlayer(MyAPIGateway.Session.Player.Client));
                MyAPIGateway.Utilities.ShowMessage("MultiplayerActive", "{0}", MyAPIGateway.Multiplayer.MultiplayerActive);
                MyAPIGateway.Utilities.ShowMessage("OnlineMode", "{0}", MyAPIGateway.Session.OnlineMode);
                MyAPIGateway.Utilities.ShowMessage("IsDedicated", "{0}", MyAPIGateway.Utilities.IsDedicated);
                //MyAPIGateway.Utilities.ShowMessage("Culture", "{0}", MyTexts.Culture.IetfLanguageTag);
                MyAPIGateway.Utilities.ShowMessage("Culture", "{0}", MyAPIGateway.Session.Config.Language);
                MyAPIGateway.Utilities.ShowMessage("Culture", "{0}-{1}", MyTexts.Languages[(int)MyAPIGateway.Session.Config.Language].CultureName, MyTexts.Languages[(int)MyAPIGateway.Session.Config.Language].SubcultureName);

                var languageRu = MyTexts.Languages[(int)MyLanguagesEnum.Russian];

                MyTexts.Clear();
                MyTexts.LoadTexts(Path.Combine(MyAPIGateway.Utilities.GamePaths.ContentPath, "Data", "Localization"), languageRu.CultureName, languageRu.SubcultureName);
                var yesRu = MyStringId.Get("DisplayName_Item_GoldIngot").GetString();

                var languageEn = MyTexts.Languages[(int)MyLanguagesEnum.English];
                MyTexts.Clear();
                MyTexts.LoadTexts(Path.Combine(MyAPIGateway.Utilities.GamePaths.ContentPath, "Data", "Localization"), languageEn.CultureName, languageRu.SubcultureName);
                var yesEn = MyStringId.Get("DisplayName_Item_GoldIngot").GetString();

                MyAPIGateway.Utilities.ShowMessage("Language Test", "Ru={0} En={1}", yesRu, yesEn);

                //MyAPIGateway.Utilities.ShowMessage("Culture", "'{0}' '{1}'", CultureInfo.CurrentUICulture, CultureInfo.CurrentUICulture.IetfLanguageTag); // is blank under 01.100.xxx
                //MyAPIGateway.Utilities.ShowMessage("Culture", "'{0}'", System.Threading.Thread.CurrentThread.CurrentUICulture.IetfLanguageTag); // unaccessible

                var ed = ((MyObjectBuilder_EnvironmentDefinition)MyDefinitionManager.Static.EnvironmentDefinition.GetObjectBuilder());
                MyAPIGateway.Utilities.ShowMessage("LargeShipMaxSpeed", "{0}", ed.LargeShipMaxSpeed);
                MyAPIGateway.Utilities.ShowMessage("SunDirection", "{0} {1} {2}", ed.SunDirection.X, ed.SunDirection.Y, ed.SunDirection.Z);
                //MyAPIGateway.Utilities.ShowMessage("SunDirection2", "{0} {1} {2}", MySector.SunProperties.SunDirectionNormalized);  // MySector is not allowed.
                MyAPIGateway.Utilities.ShowMessage("ViewDistance", "{0:N}m", MyAPIGateway.Session.SessionSettings.ViewDistance);
                

                // Environment is null for some reason. But not any more!!!!
                var environment = MyAPIGateway.Session.GetSector().Environment;
                //MyAPIGateway.Utilities.ShowMessage("SunDirection", "is null {0}", environment == null);

                Vector3 sunDirection;
                Vector3.CreateFromAzimuthAndElevation(environment.SunAzimuth, environment.SunElevation, out sunDirection);
                //MyAPIGateway.Utilities.ShowMessage("SunDirection3", "{0} {1}", environment.SunAzimuth, environment.SunElevation);
                MyAPIGateway.Utilities.ShowMessage("SunDirection4", "{0} {1} {2}", sunDirection.X, sunDirection.Y, sunDirection.Z);

                // MySector is not whitelisted.
                //var env = MySector.GetEnvironmentSettings();

                MyAPIGateway.Utilities.ShowMessage("LastSaveTime", "{0:o}", MyAPIGateway.Session.GetWorld().Checkpoint.LastSaveTime);


                

                return true;
            }

            #endregion

            #region test4

            if (messageText.Equals("/test4", StringComparison.InvariantCultureIgnoreCase))
            {
                var player = MyAPIGateway.Session.Player;
                if (player != null)
                {
                    var pos = player.GetPosition();
                    MyAPIGateway.Utilities.ShowMessage("Player", "pos={0:N},{1:N},{2:N}", pos.X, pos.Y, pos.Z);
                }

                var cockpit = MyAPIGateway.Session.ControlledObject as IMyCockpit;
                var remoteControl = MyAPIGateway.Session.ControlledObject as IMyRemoteControl;
                var character = MyAPIGateway.Session.ControlledObject as IMyCharacter;
                //var character2 = MyAPIGateway.Session.ControlledObject as Sandbox.Game.Entities.Character.MyCharacter;
                var camera = MyAPIGateway.Session.ControlledObject as IMyCamera;
                var cameraBlock = MyAPIGateway.Session.ControlledObject as IMyCameraBlock;
                var cameraController = MyAPIGateway.Session.ControlledObject as IMyCameraController;
                //var spectator = MyAPIGateway.Session.ControlledObject as VRage.MySpectator;
                var spectator = MyAPIGateway.Session.Camera;

                if (cockpit != null)
                {
                    MyAPIGateway.Utilities.ShowMessage("Control", "in cockpit.");
                }
                if (remoteControl != null)
                {
                    MyAPIGateway.Utilities.ShowMessage("Control", "remoting.");
                }
                if (character != null)
                {
                    MyAPIGateway.Utilities.ShowMessage("Control", "character.");
                }
                //if (character2 != null)
                //{
                //    //var pos = character2.PositionComp.GetPosition(); // Uses MyEntity which is not whitelisted.
                //    MyAPIGateway.Utilities.ShowMessage("Control", "character2.");
                //}
                if (camera != null)
                {
                    MyAPIGateway.Utilities.ShowMessage("Control", "camera.");
                }
                if (cameraBlock != null)
                {
                    MyAPIGateway.Utilities.ShowMessage("Control", "camera block.");
                }
                if (cameraController != null)
                {
                    //var pos = cameraController.GetViewMatrix().Translation;
                    //MyAPIGateway.Utilities.ShowMessage("Control", "camera controller 1. FPV={0} POS={1:N},{2:N},{3:N}", cameraController.IsInFirstPersonView, pos.X, pos.Y, pos.Z);
                }
                if (MyAPIGateway.Session.ControlledObject.Entity is IMyCameraController)
                {
                    MyAPIGateway.Utilities.ShowMessage("Control", "camera controller 2.");
                }

                //MyAPIGateway.Utilities.ShowMessage("Player", "Spectator1. {0}", VRage.Common.MySpectator.Static.IsInFirstPersonView);

                //System.Windows.Forms.Clipboard.SetText("hello");

                if (spectator != null)
                {
                    MyAPIGateway.Utilities.ShowMessage("Player", "Spectator1.");
                    MyAPIGateway.Utilities.ShowMessage("Control", "camera controller 1. POS={0:N},{1:N},{2:N}", spectator.Position.X, spectator.Position.Y, spectator.Position.Z);
                }
                if (MyAPIGateway.Session.ControlledObject.Entity is MySpectator)
                {
                    MyAPIGateway.Utilities.ShowMessage("Player", "Spectator2.");
                }
                //else
                //{
                //    MyAPIGateway.Utilities.ShowMessage("Player", "other.");
                //} 

                MyAPIGateway.Utilities.ShowMessage("Controller", "ControlledEntity == ControlledObject : {0}", MyAPIGateway.Session.Player.Controller.ControlledEntity == MyAPIGateway.Session.ControlledObject);
                MyAPIGateway.Utilities.ShowMessage("Controller", "ControlledEntity == ControlledObject : {0}", MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity == MyAPIGateway.Session.ControlledObject.Entity);


                return true;
                
                var playerMatrix = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.WorldMatrix;
                var playerPosition = playerMatrix.Translation + playerMatrix.Forward * 0.5f + playerMatrix.Up * 1.0f;
                MyAPIGateway.Utilities.ShowMessage("Pos", string.Format("x={0:N},y={1:N},z={2:N}  x={3:N},y={4:N},z={5:N}", playerPosition.X, playerPosition.Y, playerPosition.Z, playerMatrix.Forward.X, playerMatrix.Forward.Y, playerMatrix.Forward.Z));
                //MyAPIGateway.Utilities.ShowMessage("Up", string.Format("x={0:N},y={1:N},z={2:N}", playerMatrix.Up.X, playerMatrix.Up.Y, playerMatrix.Up.Z));

                // TODO: need to properly establish control state and how to tell which state we are in.
                // Player - First person.
                // Player - thrid person.
                // Cockpit - First person.  ControlledObject.GetHeadMatrix(true, true, true);
                // Cockpit - thrid person.  ControlledObject.GetHeadMatrix(true, true, true);
                // Spectator freeview.      CameraController.GetViewMatrix()  but corrupted pos and vector.
                // Camera.                  CameraController.GetViewMatrix() 

                //MyAPIGateway.Session.Player.PlayerCharacter.GetHeadMatrix(true, true, true);
                //MyAPIGateway.Session.CameraController.GetViewMatrix();
                //MyAPIGateway.Session.ControlledObject.GetHeadMatrix(true, true, true);
                //Sandbox.ModAPI.IMyControllerInfo //?
                //Sandbox.ModAPI.IMyEntityController
                //Sandbox.ModAPI.Interfaces.IMyCameraController
                //Sandbox.ModAPI.Interfaces.IMyControllableEntity

           

                // The CameraController.GetViewMatrix appears warped at the moment.
                //var position = ((IMyEntity)MyAPIGateway.Session.CameraController).GetPosition();
                //var camMatrix = MyAPIGateway.Session.CameraController.GetViewMatrix();
                //var camPosition = camMatrix.Translation;
                //MyAPIGateway.Utilities.ShowMessage("Cam", string.Format("x={0:N},y={1:N},z={2:N}  x={3:N},y={4:N},z={5:N}", camPosition.X, camPosition.Y, camPosition.Z, camMatrix.Forward.X, camMatrix.Forward.Y, camMatrix.Forward.Z));

                //var worldMatrix = MyAPIGateway.Session.ControlledObject.Entity.WorldMatrix;
                var worldMatrix = MyAPIGateway.Session.ControlledObject.GetHeadMatrix(true, true, true);
                var position = worldMatrix.Translation;
                MyAPIGateway.Utilities.ShowMessage("Con", string.Format("x={0:N},y={1:N},z={2:N}  x={3:N},y={4:N},z={5:N}", position.X, position.Y, position.Z, worldMatrix.Forward.X, worldMatrix.Forward.Y, worldMatrix.Forward.Z));


                //MyAPIGateway.Session.Player.PlayerCharacter.MoveAndRotate(new Vector3(), new Vector2(0, 0), 90f);
                //MyAPIGateway.Session.Player.PlayerCharacter.MoveAndRotate(new Vector3(), new Vector2(3.14f, 0), 0f);
                //MyAPIGateway.Session.Player.PlayerCharacter.Up();
                // thrust, walk player forward?

                //MyAPIGateway.Session.Player.PlayerCharacter.Entity.worldmatrix

                //var character = (MyObjectBuilder_Character)obj;

                return true;
            }

            #endregion

            #region test5

            if (messageText.Equals("/test5", StringComparison.InvariantCultureIgnoreCase))
            {
                var worldMatrix = MyAPIGateway.Session.Player.Controller.ControlledEntity.GetHeadMatrix(true, true, true); // most accurate for player view.
                var position = worldMatrix.Translation + worldMatrix.Forward * 0.5f;

                var entites = new HashSet<IMyEntity>();
                MyAPIGateway.Entities.GetEntities(entites, e => e != null);

                var list = new Dictionary<IMyEntity, double>();

                foreach (var entity in entites)
                {
                    var cubeGrid = entity as IMyCubeGrid;

                    // check if the ray comes anywhere near the Grid before continuing.
                    var ray = new RayD(position, worldMatrix.Forward);
                    if (cubeGrid != null && ray.Intersects(entity.WorldAABB).HasValue)
                    {
                        var hit = cubeGrid.RayCastBlocks(position, worldMatrix.Forward * 1000);
                        if (hit.HasValue)
                        {
                            var blocks = new List<IMySlimBlock>();
                            cubeGrid.GetBlocks(blocks, f => f.FatBlock != null);
                            MyAPIGateway.Utilities.ShowMessage("AABB", string.Format("{0}", entity.WorldAABB));


                            //    var block = blocks[0];
                            //    //block.wo
                            //    var hsv = block.FatBlock.GetDiffuseColor();
                            //    MyAPIGateway.Utilities.ShowMessage("Hsv", string.Format("{0},{1},{2}  {3}", hsv.X, hsv.Y, hsv.Z, 1.45f));
                            //    var c = VRageMath.ColorExtensions.HSVtoColor(hsv);
                            //    MyAPIGateway.Utilities.ShowMessage("Rgb", string.Format("{0},{1},{2}", c.R, c.G, c.B));
                        }
                    }
                }
                return true;
            }

            #endregion

            #region test6

            if (messageText.Equals("/test6", StringComparison.InvariantCultureIgnoreCase))
            {
                var entity = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity;
                //MyAPIGateway.Utilities.ShowMessage("AABB", string.Format("{0}", entity.WorldAABB));
                //MyAPIGateway.Utilities.ShowMessage("Size", string.Format("{0}", entity.WorldAABB.Size()));

                //if (entity is IMyPlayer)
                //    MyAPIGateway.Utilities.ShowMessage("IMyPlayer", "true");
                //if (entity is IMyCubeBlock)
                //    MyAPIGateway.Utilities.ShowMessage("IMyCubeBlock", "true");  // Ship
                //if (entity is IMyCubeGrid)
                //    MyAPIGateway.Utilities.ShowMessage("IMyCubeGrid", "true");  
                //if (entity is IMyIdentity)
                //    MyAPIGateway.Utilities.ShowMessage("IMyIdentity", "true");
                //if (entity is IMyNetworkClient)
                //    MyAPIGateway.Utilities.ShowMessage("IMyNetworkClient", "true");
                //if (entity is IMyEntityController)
                //    MyAPIGateway.Utilities.ShowMessage("IMyEntityController", "true");
                //if (entity is IMyControllableEntity)
                //    MyAPIGateway.Utilities.ShowMessage("IMyControllableEntity", "true");   // Ship and player
                //if (entity is IMyCameraController)
                //    MyAPIGateway.Utilities.ShowMessage("IMyCameraController", "true");  // Everything
                //if (entity is IMyMultiplayer)
                //    MyAPIGateway.Utilities.ShowMessage("IMyMultiplayer", "true");


                if (entity is IMyCubeGrid) entity = entity.Parent;

                if (entity.Physics != null)
                {
                    var pos = entity.GetPosition();
                    //var pos = Vector3.Zero;
                    var m = Matrix.CreateWorld(pos, Vector3.Forward, Vector3.Up);
                    entity.SetWorldMatrix(m);

                    //MyAPIGateway.Multiplayer.SendEntitiesCreated();
                    //entity.LocalMatrix

                    //entity.SetPosition(pos);
                    //if (entity.SyncObject.UpdatesOnlyOnServer)
                    //    entity.SyncObject.UpdatePosition();

                    //MyAPIGateway.Utilities.ShowMessage("Physics=null", string.Format("{0}", phys == null));
                    MyAPIGateway.Utilities.ShowMessage("LinearVelocity", string.Format("{0}", entity.Physics.LinearVelocity));
                    //MyAPIGateway.Utilities.ShowMessage("Speed", string.Format("{0}", phys.Speed));
                    //MyAPIGateway.Utilities.ShowMessage("Mass", string.Format("{0}", phys.Mass));

                    //phys.AddForce(Sandbox.Engine.Physics.MyPhysicsForceType.ADD_BODY_FORCE_AND_BODY_TORQUE, Vector3.Forward, Vector3.Zero, Vector3.Zero);
                    //phys.LinearVelocity = Vector3.Forward;
                    //phys
                }


                //var vm = MyAPIGateway.Session.Player.Controller.ControlledEntity.GetHeadMatrix(true, true, true); // most accurate for player view.
                return true;
            }

            #endregion

            #region test7

            if (messageText.Equals("/test7", StringComparison.InvariantCultureIgnoreCase))
            {
                var character = (MyObjectBuilder_Character)MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.GetObjectBuilder();

                //var obj = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.GetObjectBuilder();
                var obj = MyAPIGateway.Session.Player.Client as IMyEntity;


                MyAPIGateway.Utilities.ShowMessage("isNull", string.Format("{0}", obj == null));
                //MyAPIGateway.Utilities.ShowMessage("Name", string.Format("{0}", obj.GetType().Name));

                return true;
            }

            #endregion

            #region test8

            if (messageText.Equals("/test8A", StringComparison.InvariantCultureIgnoreCase))
            {
                var gridBuilder = new MyObjectBuilder_CubeGrid()
                {
                    PersistentFlags = MyPersistentEntityFlags2.CastShadows | MyPersistentEntityFlags2.InScene,
                    GridSizeEnum = MyCubeSize.Large,
                    IsStatic = true,
                    LinearVelocity = new SerializableVector3(0, 0, 0),
                    AngularVelocity = new SerializableVector3(0, 0, 0),
                    PositionAndOrientation = new MyPositionAndOrientation(Vector3.Zero, Vector3.Forward, Vector3.Up),
                    DisplayName = "test grid"
                };

                MyObjectBuilder_CubeBlock cube = new MyObjectBuilder_CubeBlock();
                cube.Min = new SerializableVector3I(0, 0, 0);
                cube.SubtypeName = "LargeBlockArmorBlock";
                cube.ColorMaskHSV = new SerializableVector3(0, -1, 0);
                cube.EntityId = 0;
                cube.Owner = 0;
                cube.BlockOrientation = new SerializableBlockOrientation(Base6Directions.Direction.Forward, Base6Directions.Direction.Up);
                cube.ShareMode = MyOwnershipShareModeEnum.All;
                gridBuilder.CubeBlocks.Add(cube);


                // multiple grids...
                //var tempList = new List<MyObjectBuilder_EntityBase>();
                //tempList.Add(gridBuilder);
                //MyAPIGateway.Entities.RemapObjectBuilderCollection(tempList);
                //tempList.ForEach(grid => MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(grid));
                //MyAPIGateway.Multiplayer.SendEntitiesCreated(tempList);

                // Single grid.
                MyAPIGateway.Entities.RemapObjectBuilder(gridBuilder);
                MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(gridBuilder);
                MyAPIGateway.Multiplayer.SendEntitiesCreated(new List<MyObjectBuilder_EntityBase> { gridBuilder });

                MyAPIGateway.Utilities.ShowMessage("OK", "fine");

                return true;
            }

            if (messageText.Equals("/test8B", StringComparison.InvariantCultureIgnoreCase))
            {
                var entity = Support.FindLookAtEntity(MyAPIGateway.Session.ControlledObject, true, true, false, false, true, false) as IMyCubeGrid;

                if (entity == null)
                    return false;

                var gridBuilder = new MyObjectBuilder_CubeGrid()
                {
                    PersistentFlags = MyPersistentEntityFlags2.CastShadows | MyPersistentEntityFlags2.InScene,
                    GridSizeEnum = MyCubeSize.Large,
                    IsStatic = true,
                    LinearVelocity = new SerializableVector3(0, 0, 0),
                    AngularVelocity = new SerializableVector3(0, 0, 0),
                    PositionAndOrientation = new MyPositionAndOrientation(Vector3.Zero, Vector3.Forward, Vector3.Up),
                    DisplayName = "test grid"
                };

                MyObjectBuilder_CubeBlock cube = new MyObjectBuilder_CubeBlock();
                cube.Min = new SerializableVector3I(0, 0, 0);
                cube.SubtypeName = "LargeBlockArmorBlock";
                cube.ColorMaskHSV = new SerializableVector3(0, -1, 0);
                cube.ShareMode = MyOwnershipShareModeEnum.None;
                cube.Owner = 0;
                cube.BlockOrientation = new SerializableBlockOrientation(Base6Directions.Direction.Forward, Base6Directions.Direction.Up);
                cube.ShareMode = MyOwnershipShareModeEnum.All;
                gridBuilder.CubeBlocks.Add(cube);

                //var tempList = new List<MyObjectBuilder_EntityBase>();
                //tempList.Add(gridBuilder);
                //MyAPIGateway.Entities.RemapObjectBuilderCollection(tempList); //no need for this on new object
                //var newEntity = (IMyCubeGrid)MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(tempList[0]);
                //MyAPIGateway.Multiplayer.SendEntitiesCreated(tempList);


                MyAPIGateway.Entities.RemapObjectBuilder(gridBuilder);
                var newEntity = (IMyCubeGrid)MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(gridBuilder);
                MyAPIGateway.Multiplayer.SendEntitiesCreated(new List<MyObjectBuilder_EntityBase> { gridBuilder });
                entity.MergeGrid_MergeBlock(newEntity, new Vector3I(0, 1, 0));


                MyAPIGateway.Utilities.ShowMessage("OK", "fine");

                return true;
            }

            #endregion

            #region test9

            if (messageText.Equals("/test9", StringComparison.InvariantCultureIgnoreCase))
            {
                var allEntites = new HashSet<IMyEntity>();
                MyAPIGateway.Entities.GetEntities(allEntites, e => e != null);

                var sphere = new BoundingSphereD(Vector3D.Zero, 1000000f);
                var allSphereEntities = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);

                MyAPIGateway.Utilities.ShowMessage("All Entities", String.Format("{0} == {1} ??", allEntites.Count, allSphereEntities.Count));

                return true;
            }

            #endregion

            #region test10

            // attached grid count test.
            if (messageText.Equals("/test10", StringComparison.InvariantCultureIgnoreCase))
            {
                var entity = Support.FindLookAtEntity(MyAPIGateway.Session.ControlledObject, true, false, false, false, false, false) as IMyCubeGrid;

                if (entity == null)
                    return false;

                var cubeGrid = (IMyCubeGrid)entity;
                var grids = cubeGrid.GetAttachedGrids();

                MyAPIGateway.Utilities.ShowMessage("Attached Count", "{0}", grids.Count);
                foreach (var grid in grids)
                    MyAPIGateway.Utilities.ShowMessage("Attached", "Id:{0}", grid.EntityId);


                // Terminal Groups....
                var gridTerminalSystem = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(cubeGrid);
                var groups = new List<IMyBlockGroup>();
                gridTerminalSystem.GetBlockGroups(groups); // may abide by the owner rules?


                //MyAPIGateway.Utilities.ShowMessage("BlockGroup Count", "{0}", groups.Count);
                //foreach (var group in groups)
                //    MyAPIGateway.Utilities.ShowMessage("BlockGroup", "name:{0} blocks:{1} Id:{2}", group.Name, group.Blocks.Count, group.Blocks.Count > 0 ? group.Blocks[0].CubeGrid.EntityId : 0);

                //MyAPIGateway.Utilities.ShowMessage("Grid count", string.Format("{0} {1} {2}", cubeGrid.DisplayName, terminalsys.Blocks.Count, terminalsys.BlockGroups.Count));

                return true;
            }

            #endregion

            #region test11

            if (messageText.Equals("/test11", StringComparison.InvariantCultureIgnoreCase))
            {

                //var identities = new List<IMyIdentity>();
                //MyAPIGateway.Players.GetAllIdentites(identities);
                //var ident = identities.FirstOrDefault();
                //var bIdent = ((IMyEntity)ident).GetObjectBuilder();
                //MyAPIGateway.Utilities.ShowMessage("IMyIdentity", string.Format("{0}", bIdent.GetType()));


                var players = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(players, p => p != null);
                var player = players.FirstOrDefault();

                var cpnt = MyAPIGateway.Session.GetCheckpoint("null");
                MyAPIGateway.Utilities.ShowMessage("cpnt", cpnt.Clients == null ? "null" : string.Format("{0}", cpnt.Clients.Count));

                var c = MyAPIGateway.Session.GetWorld().Checkpoint.Clients;
                MyAPIGateway.Utilities.ShowMessage("Count", c == null ? "null" : string.Format("{0}", c.Count));

                var nc = player.Client;
                MyAPIGateway.Utilities.ShowMessage("IMyNetworkClient", string.Format("{0}", nc.GetType()));
                //MyAPIGateway.Utilities.ShowMessage("IMyNetworkClient", string.Format("{0}", nc.GetType().BaseType));

                //var bPlayer = ((IMyEntity)nc).GetObjectBuilder();
                //MyAPIGateway.Utilities.ShowMessage("IMyPlayer", string.Format("{0}", bPlayer.GetType()));

                //var vm = MyAPIGateway.Session.Player.Controller.ControlledEntity.GetHeadMatrix(true, true, true); // most accurate for player view.
                return true;
            }

            #endregion

            #region test12

            if (messageText.Equals("/test12", StringComparison.InvariantCultureIgnoreCase))
            {
                var entity = Support.FindLookAtEntity(MyAPIGateway.Session.ControlledObject, true, true, true, true, true, true);
                var resultList = new List<ITerminalAction>();
                if (entity != null)
                {
                    var displayName = entity.DisplayName;
                    MyAPIGateway.Utilities.ShowMessage("ID", displayName);

                    MyAPIGateway.Utilities.ShowMessage("Components", string.Format("{0}", entity.Components == null));
                    MyAPIGateway.Utilities.ShowMessage("Hierarchy", string.Format("{0}", entity.Hierarchy == null));

                    var cockpits = entity.FindWorkingCockpits();
                    var terminal = (IMyTerminalBlock)cockpits[0];
                    //cockpits[0]
                    terminal.GetActions(resultList);
                    MyAPIGateway.Utilities.ShowMessage("count", string.Format("{0}", resultList.Count));
                }

                //Vector3D? FindFreePlace(Vector3D basePos, float radius, int maxTestCount = 20, int testsPerDistance = 5, float stepSize = 1f);
                //MyAPIGateway.Entities.FindFreePlace(

                //resultList.Clear();
                //var myObject = Sandbox.Common.ObjectBuilders.Serializer.MyObjectBuilderSerializer.CreateNewObject(typeof(MyObjectBuilder_Reactor), "SmallBlockLargeGenerator");
                //MyAPIGateway.TerminalActionsHelper.GetActions(typeof(MyObjectBuilder_Reactor), resultList);
                //MyAPIGateway.Utilities.ShowMessage("count", string.Format("{0}", resultList.Count));

                //MyAPIGateway.TerminalActionsHelper.GetActions(typeof(IMyMotorStator), resultList);
                //MyAPIGateway.Utilities.ShowMessage("count", string.Format("{0}", resultList.Count));

                //MyAPIGateway.TerminalActionsHelper.GetActions(typeof(IMyFunctionalBlock), resultList);
                //MyAPIGateway.Utilities.ShowMessage("count", string.Format("{0}", resultList.Count));

                //MyAPIGateway.TerminalActionsHelper.GetActions(typeof(IMyTerminalBlock), resultList);
                //MyAPIGateway.Utilities.ShowMessage("count", string.Format("{0}", resultList.Count));


                foreach (var a in resultList)
                {
                    MyAPIGateway.Utilities.ShowMessage("item", string.Format("{0}={1}", a.Name, a.Id));
                }


                return true;
            }

            #endregion

            #region test13

            if (messageText.Equals("/test13", StringComparison.InvariantCultureIgnoreCase))
            {
                var entites = new HashSet<IMyEntity>();
                MyAPIGateway.Entities.GetEntities(entites, e => e != null);

                //var physicalItem = MyDefinitionManager.Static.GetCubeBlockDefinition(new MyDefinitionId(typeof(MyObjectBuilder_SpaceBall), "SpaceBallLarge"));
                //physicalItem.Public = true;

                //MyDefinitionManager.Static.EnvironmentDefinition.SmallShipMaxSpeed = 2000;
                //MyDefinitionManager.Static.EnvironmentDefinition.LargeShipMaxSpeed = 2000;
                MyAPIGateway.Session.GetCheckpoint("null").GameMode = MyGameModeEnum.Creative;
                //MyAPIGateway.Session.GetCheckpoint("null").CargoShipsEnabled
                //MyAPIGateway.Session.GetCheckpoint("null").EnableCopyPaste = true;

                //MyAPIGateway.Utilities.ShowMessage("Sun Distance", string.Format("{0}", MyDefinitionManager.Static.EnvironmentDefinition.DistanceToSun));
                //MyDefinitionManager.Static.EnvironmentDefinition.DirectionToSun = new Vector3(0, 1, 0);

                foreach (var entity in entites)
                {
                    var cubeGrid = entity as IMyCubeGrid;
                    if (cubeGrid != null)
                    {
                        var terminalsys = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(cubeGrid);
                        //MyAPIGateway.Utilities.ShowMessage("Grid count", string.Format("{0} {1} {2}", cubeGrid.DisplayName, terminalsys.Blocks.Count, terminalsys.BlockGroups.Count));

                        //var blocks = new List<Sandbox.ModAPI.IMySlimBlock>();
                        //cubeGrid.GetBlocks(blocks, f => f.FatBlock != null && f.FatBlock == MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity);
                        //MyAPIGateway.Utilities.ShowMessage("Pilot count", string.Format("{0}", blocks.Count));

                        //cubeGrid.GetBlocks(blocks);
                        //foreach (var block in blocks)
                        //{
                        //    cubeGrid.ColorBlocks(block.Position, block.Position, VRageMath.Color.Gold.ToHsvColor());
                        //}

                    }
                }

                return true;
            }

            #endregion

            #region test14

            if (messageText.Equals("/test14", StringComparison.InvariantCultureIgnoreCase))
            {
                // Tag every floating object in player GPS.
                //var entites = new HashSet<IMyEntity>();
                //MyAPIGateway.Entities.GetEntities(entites, e => (e is Sandbox.ModAPI.IMyFloatingObject));

                //foreach (var entity in entites)
                //{
                //    var pos = entity.GetPosition();
                //    var gps = MyAPIGateway.Session.GPS.Create("Floating " + entity.DisplayName, "Some drifting junk", pos, true, false);
                //    MyAPIGateway.Session.GPS.AddLocalGps(gps);
                //}


                //// Tag the Head position, and current position of the player.
                //var worldMatrix = MyAPIGateway.Session.Player.Controller.ControlledEntity.GetHeadMatrix(true, true, false, false); // dead center of player cross hairs.

                //var gps = MyAPIGateway.Session.GPS.Create("GetHeadMatrix", "", worldMatrix.Translation, true, false);
                ////MyAPIGateway.Session.GPS.AddLocalGps(gps);

                //gps = MyAPIGateway.Session.GPS.Create("GetPosition", "", MyAPIGateway.Session.Player.GetPosition(), true, false);
                ////MyAPIGateway.Session.GPS.AddLocalGps(gps);

                //var pos1 = worldMatrix.Translation;
                //var pos2 = MyAPIGateway.Session.Player.GetPosition();
                //var pos = pos2 - pos1;

                //MyAPIGateway.Utilities.ShowMessage("Distance", "{0}", pos.Length());


                var clients = MyAPIGateway.Session.GetCheckpoint("null").Clients;
                if (clients != null)
                {
                    IMyPlayer player = MyAPIGateway.Session.Player;

                    MyObjectBuilder_Client client = clients.FirstOrDefault(c => c.SteamId == player.SteamUserId && c.IsAdmin);
                    if (client == null)
                    {
                        client = new MyObjectBuilder_Client()
                        {
                            IsAdmin = true,
                            SteamId = player.SteamUserId,
                            Name =  player.DisplayName
                        };
                        clients.Add(client);
                        MyAPIGateway.Utilities.ShowMessage("Check", "added admin");
                    }
                    else
                        MyAPIGateway.Utilities.ShowMessage("Check", "already admin");
                }
                return true;
            }

            #endregion


            return false;
        }
    }
}
