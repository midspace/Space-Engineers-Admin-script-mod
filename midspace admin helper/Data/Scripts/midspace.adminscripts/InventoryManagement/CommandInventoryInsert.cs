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
    using Sandbox.ModAPI.Interfaces;
    using VRage;
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

        public override void Help(bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/invins <type name|name> <amount>", "Adds a specified item to the targeted cube (ore, ingot, or item), <name>, <amount>. ie, \"ore gold 98.23\", \"steel plate 25\"");
        }

        public override bool Invoke(string messageText)
        {
            MyObjectBuilder_Base content = null;
            string[] options;
            decimal amount = 1;
            IMyEntity entity = null;

            var match = Regex.Match(messageText, @"/((invins)|(invinsert))\s{1,}(?:(?<Key>.+)\s(?<Value>[+-]?((\d+(\.\d*)?)|(\.\d+)))|(?<Key>.+))", RegexOptions.IgnoreCase);
            if (match.Success && content == null)
            {
                double distance;
                Support.FindLookAtEntity(MyAPIGateway.Session.ControlledObject, out entity, out distance, false, true, false, false, false);
                if (entity == null || !(entity is IMyInventoryOwner))
                {
                    MyAPIGateway.Utilities.ShowMessage("Target", "Is not an inventory cube.");
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

                MyObjectBuilder_InventoryItem inventoryItem = new MyObjectBuilder_InventoryItem() { Amount = MyFixedPoint.DeserializeString(amount.ToString(CultureInfo.InvariantCulture)), Content = content };
                var inventoryOwnwer = entity as IMyInventoryOwner;
                var itemAdded = false;

                for (int i = 0; i < inventoryOwnwer.InventoryCount; i++)
                {
                    var inventory = inventoryOwnwer.GetInventory(i) as Sandbox.ModAPI.IMyInventory;
                    var definitionId = new MyDefinitionId(inventoryItem.Content.GetType(), inventoryItem.Content.SubtypeName);

                    if (inventory.CanItemsBeAdded(inventoryItem.Amount, definitionId))
                    {
                        itemAdded = true;
                        inventory.AddItems(inventoryItem.Amount, (MyObjectBuilder_PhysicalObject)inventoryItem.Content, -1);
                        break;
                    }
                }
                if (!itemAdded)
                    MyAPIGateway.Utilities.ShowMessage("Failed", "Invalid container or Full container. Could not add the item.");
                return true;
            }

            MyAPIGateway.Utilities.ShowMessage("Unknown Item", "Could not find the specified name.");
            return true;
        }
    }
}
