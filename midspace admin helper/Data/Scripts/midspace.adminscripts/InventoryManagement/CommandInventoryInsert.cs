namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using midspace.adminscripts.Messages.Sync;
    using Sandbox.Common.ObjectBuilders;
    using Sandbox.Definitions;
    using Sandbox.Game.Entities;
    using Sandbox.ModAPI;
    using VRage;
    using VRage.Game;
    using VRage.Game.Entity;
    using VRage.ModAPI;
    using VRage.ObjectBuilders;

    public class CommandInventoryInsert : ChatCommand
    {
        private readonly string[] _oreNames;
        private readonly string[] _ingotNames;
        private readonly MyPhysicalItemDefinition[] _physicalItems;
        private readonly string[] _physicalItemNames;

        public CommandInventoryInsert(string[] oreNames, string[] ingotNames, MyPhysicalItemDefinition[] physicalItems)
            : base(ChatCommandSecurity.Admin, "invins", new[] { "/invins", "/invinsert" })
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

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/invins <type name|name> <amount>", "Adds a specified item to the targeted player, backpack, cube (ore, ingot, or item), <name>, <amount>. ie, \"ore gold 98.23\", \"steel plate 25\"");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            MyObjectBuilder_Base content = null;
            string[] options;
            decimal amount = 1;
            IMyEntity entity = null;

            var match = Regex.Match(messageText, @"/((invins)|(invinsert))\s{1,}(?:(?<Key>.+)\s(?<Value>[+-]?((\d+(\.\d*)?)|(\.\d+)))|(?<Key>.+))", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                entity = Support.FindLookAtEntity(MyAPIGateway.Session.ControlledObject, false, true, true, false, false, true);
                if (entity == null)
                {
                    MyAPIGateway.Utilities.ShowMessage("Target", "Nothing is targeted.");
                    return true;
                }

                Sandbox.Game.MyInventory inventory;
                if (!((MyEntity)entity).TryGetInventory(out inventory))
                {
                    MyAPIGateway.Utilities.ShowMessage("Target", "Cannot hold inventory.");
                    return true;
                }

                var itemName = match.Groups["Key"].Value;
                var strAmount = match.Groups["Value"].Value;
                if (!decimal.TryParse(strAmount, out amount))
                    amount = 1;

                if (!Support.FindPhysicalParts(_oreNames, _ingotNames, _physicalItemNames, _physicalItems, itemName, out content, out options) && options.Length > 0)
                {
                    MyAPIGateway.Utilities.ShowMessage("Did you mean", String.Join(", ", options) + " ?");
                    return true;
                }
            }

            if (content != null)
            {
                if (amount < 0)
                    amount = 1;

                if (content.TypeId != typeof(MyObjectBuilder_Ore) && content.TypeId != typeof(MyObjectBuilder_Ingot))
                {
                    // must be whole numbers.
                    amount = Math.Round(amount, 0);
                }

                if (!MyAPIGateway.Multiplayer.MultiplayerActive)
                {
                    var definitionId = new MyDefinitionId(content.GetType(), content.SubtypeName);
                    if (!Support.InventoryAdd((MyEntity)entity, (MyFixedPoint)amount, definitionId))
                        MyAPIGateway.Utilities.ShowMessage("Failed", "Invalid container or Full container. Could not add the item.");
                }
                else
                    ConnectionHelper.SendMessageToServer(new MessageSyncCreateObject()
                    {
                        EntityId = entity.EntityId,
                        Type = SyncCreateObjectType.Inventory,
                        TypeId = content.TypeId.ToString(),
                        SubtypeName = content.SubtypeName,
                        Amount = amount
                    });

                return true;
            }

            MyAPIGateway.Utilities.ShowMessage("Unknown Item", "Could not find the specified name.");
            return true;
        }
    }
}
