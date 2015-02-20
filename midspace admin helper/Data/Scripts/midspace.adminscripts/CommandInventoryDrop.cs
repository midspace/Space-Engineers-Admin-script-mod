namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Sandbox.Common.ObjectBuilders;
    using Sandbox.Definitions;
    using Sandbox.ModAPI;
    using VRage;
    using VRageMath;

    public class CommandInventoryDrop : ChatCommand
    {
        private readonly string[] _oreNames;
        private readonly string[] _ingotNames;
        private readonly MyPhysicalItemDefinition[] _physicalItems;
        private readonly string[] _physicalItemNames;

        public CommandInventoryDrop(string[] oreNames, string[] ingotNames, MyPhysicalItemDefinition[] physicalItems)
            : base(ChatCommandSecurity.Admin, "drop", new[] { "/drop" })
        {
            _oreNames = oreNames;
            _ingotNames = ingotNames;
            _physicalItems = physicalItems;

            // Make sure all Public Physical item names are unique, so they can be properly searched for.
            var names = new List<string>();
            foreach (var item in _physicalItems)
            {
                var baseName = item.DisplayNameEnum.HasValue ? item.DisplayNameEnum.Value.GetString() : item.DisplayNameString;
                var uniqueName = baseName;
                var index = 1;
                while (names.Contains(uniqueName, StringComparer.InvariantCultureIgnoreCase))
                {
                    index++;
                    uniqueName = string.Format("{0}{1}", baseName, index);
                }
                names.Add(uniqueName);
            }
            _physicalItemNames = names.ToArray();
        }

        public override void Help()
        {
            MyAPIGateway.Utilities.ShowMessage("/drop <type name|name> <amount>", "Drop a specified item (ore, ingot, or item), <name>, <amount>. ie, \"ore gold 98.23\", \"steel plate 25\"");
        }

        public override bool Invoke(string messageText)
        {
            if (messageText.StartsWith("/drop ", StringComparison.InvariantCultureIgnoreCase))
            {
                MyObjectBuilder_InventoryItem inventoryItem = null;

                var match = Regex.Match(messageText, @"/drop(?:\s{1,}(?<ITEM>[^\s]*)){1,3}", RegexOptions.IgnoreCase);
                if (match.Success && match.Groups["ITEM"].Captures.Count > 1)
                {
                    var item = match.Groups["ITEM"].Captures[0].Value;
                    var subitem = match.Groups["ITEM"].Captures.Count > 1 ? match.Groups["ITEM"].Captures[1].Value : string.Empty;
                    var strAmount = match.Groups["ITEM"].Captures.Count > 2 ? match.Groups["ITEM"].Captures[2].Value : "1";
                    decimal amount = 1;
                    decimal.TryParse(strAmount, out amount);

                    if (item.Equals("ore", StringComparison.InvariantCultureIgnoreCase))
                    {
                        foreach (var ore in _oreNames)
                        {
                            if (ore.Equals(subitem, StringComparison.InvariantCultureIgnoreCase))
                            {
                                inventoryItem = new MyObjectBuilder_InventoryItem() { Amount = MyFixedPoint.DeserializeString(amount.ToString(CultureInfo.InvariantCulture)), Content = new MyObjectBuilder_Ore() { SubtypeName = ore } };
                                break;
                            }
                        }
                    }

                    if (item.Equals("ingot", StringComparison.InvariantCultureIgnoreCase))
                    {
                        foreach (var ingot in _ingotNames)
                        {
                            if (ingot.Equals(subitem, StringComparison.InvariantCultureIgnoreCase))
                            {
                                inventoryItem = new MyObjectBuilder_InventoryItem() { Amount = MyFixedPoint.DeserializeString(amount.ToString(CultureInfo.InvariantCulture)), Content = new MyObjectBuilder_Ingot() { SubtypeName = ingot } };
                                break;
                            }
                        }
                    }
                }

                match = Regex.Match(messageText, @"/drop\s{1,}(?:(?<Key>.+)\s(?<Value>[+-]?((\d+(\.\d*)?)|(\.\d+)))|(?<Key>.+))", RegexOptions.IgnoreCase);
                if (match.Success && inventoryItem == null)
                {
                    var itemName = match.Groups["Key"].Value;
                    var strAmount = match.Groups["Value"].Value;
                    decimal amount = 1;
                    if (!decimal.TryParse(strAmount, out amount))
                        amount = 1;

                    // full name match.
                    var res = _physicalItemNames.FirstOrDefault(s => s.Equals(itemName, StringComparison.InvariantCultureIgnoreCase));
                    //MyAPIGateway.Utilities.ShowMessage("match1", (res.Value == null).ToString());

                    // need a good method for finding partial name matches.
                    if (res == null)
                    {
                        var matches = _physicalItemNames.Where(s => s.StartsWith(itemName, StringComparison.InvariantCultureIgnoreCase)).Distinct().ToArray();
                        //MyAPIGateway.Utilities.ShowMessage("match2", matches.Count.ToString());

                        if (matches.Length == 1)
                        {
                            res = matches.FirstOrDefault();
                        }
                        else
                        {
                            matches = _physicalItemNames.Where(s => s.IndexOf(itemName, StringComparison.InvariantCultureIgnoreCase) >= 0).Distinct().ToArray();
                            //matches = _physicalItemNames.Where(kvp => s.Contains(itemName, StringComparison.InvariantCultureIgnoreCase)).ToArray();  // .Contains() stops mod from loading for some reason.
                            //MyAPIGateway.Utilities.ShowMessage("match3", (matches.Count).ToString());
                            if (matches.Length == 1)
                            {
                                res = matches.FirstOrDefault();
                            }
                            else if (matches.Length > 1)
                            {
                                var options = String.Join(", ", matches);
                                MyAPIGateway.Utilities.ShowMessage("did you mean", options + " ?");
                                return true;
                            }
                        }
                    }

                    //MyAPIGateway.Utilities.ShowMessage("match4", (res.Value == null).ToString());

                    if (res != null)
                    {
                        var item = _physicalItems[Array.IndexOf(_physicalItemNames, res)];
                        if (item != null)
                        {
                            if (item.Id.TypeId == typeof(MyObjectBuilder_Ore) || item.Id.TypeId == typeof(MyObjectBuilder_Ingot))
                            {
                                if (amount < 0)
                                    amount = 1;
                            }
                            else
                            {
                                // must be whole numbers.
                                amount = Math.Round(amount, 0);
                                if (amount < 1)
                                    amount = 1;
                            }

                            //MyAPIGateway.Utilities.ShowMessage("ammount", amount.ToString() + "   '" + strAmount + "'");
                            //MyAPIGateway.Utilities.ShowMessage("Key", res.Key + " " + item.Id.ToString());
                            var myObject = Sandbox.Common.ObjectBuilders.Serializer.MyObjectBuilderSerializer.CreateNewObject(item.Id.TypeId, item.Id.SubtypeName);
                            inventoryItem = new MyObjectBuilder_InventoryItem() { Amount = MyFixedPoint.DeserializeString(amount.ToString(CultureInfo.InvariantCulture)), Content = myObject };
                        }
                    }
                }

                if (inventoryItem != null)
                {
                    MyObjectBuilder_FloatingObject floatingBuilder = new MyObjectBuilder_FloatingObject();
                    floatingBuilder.Item = inventoryItem;
                    floatingBuilder.PersistentFlags = MyPersistentEntityFlags2.InScene; // Very important

                    var worldMatrix = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.WorldMatrix;
                    Vector3D position;
                    if (MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.Parent == null)
                    {
                        position = worldMatrix.Translation + worldMatrix.Forward * 1.5f + worldMatrix.Up * 1.5f; // Spawn ore 1.5m in front of player.
                    }
                    else
                    {
                        position = worldMatrix.Translation + worldMatrix.Forward * 1.5f; // Spawn ore 1.5m in front of player in cockpit.
                    }

                    floatingBuilder.PositionAndOrientation = new MyPositionAndOrientation()
                    {
                        Position = position,
                        Forward = worldMatrix.Forward.ToSerializableVector3(),
                        Up = worldMatrix.Up.ToSerializableVector3(),
                    };

                    floatingBuilder.CreateAndSyncEntity();
                    return true;
                }
            }

            MyAPIGateway.Utilities.ShowMessage("Unknown Item", "Could not find the specified name.");
            return false;
        }
    }
}
