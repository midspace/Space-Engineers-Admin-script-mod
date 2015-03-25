namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Sandbox.Definitions;
    using Sandbox.ModAPI;
    using System.Collections;

    /// <summary>
    /// An attempt to fetch Ship Blueprints, except there does not appear to be any classes to expose them.
    /// </summary>
    public class CommandListBlueprints : ChatCommand
    {
        /// <summary>
        /// Temporary hotlist cache created when player requests a list of blueprints, populated only by search results.
        /// </summary>
        public readonly static List<MyDefinitionBase> BlueprintCache = new List<MyDefinitionBase>();

        public CommandListBlueprints()
            : base(ChatCommandSecurity.Admin, ChatCommandFlag.Experimental, "listblueprints", new[] { "/listblueprints" })
        {
        }

        public override void Help(bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/listblueprints <filter>", "List ships in the Blueprints. Optional <filter> to refine your search by name.");
        }

        public override bool Invoke(string messageText)
        {
            if (messageText.StartsWith("/listblueprints", StringComparison.InvariantCultureIgnoreCase))
            {
                string blueprintName = null;
                var match = Regex.Match(messageText, @"/listblueprints\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    blueprintName = match.Groups["Key"].Value;
                }

                //MyDefinitionManager.Static.TryGetDefinition<ShipBlueprint
                //MyDefinitionManager.Static.GetObjectBuilder(MyDefinitionBase.GetObjectFactory<MyDefinitionTypeAttribute.GetCustomAttributes

                var list = MyDefinitionManager.Static.GetAllDefinitions().ToArray(); // this only fetches item, component, and cube blueprints. NOT ships.
                //var list = Enumerable.OfType<MyPrefabDefinition>(MyDefinitionManager.Static.GetAllDefinitions()).ToArray();
                if (blueprintName != null)
                    list = list.Where(item => item.DisplayNameText != null && item.DisplayNameText.IndexOf(blueprintName, StringComparison.InvariantCultureIgnoreCase) >= 0).ToArray();

                BlueprintCache.Clear();

                MyAPIGateway.Utilities.ShowMessage("Count", list.Count().ToString());
                var index = 1;
                foreach (var item in list)
                {
                    //BlueprintCache.Add(item);
                    MyAPIGateway.Utilities.ShowMessage(string.Format("#{0}", index++), item.DisplayNameText);
                }

                return true;
            }

            return false;
        }
    }
}
