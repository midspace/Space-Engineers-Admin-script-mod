namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;

    using Sandbox.ModAPI;
    using VRage.ModAPI;

    public class CommandObjectsCount : ChatCommand
    {
        public CommandObjectsCount()
            : base(ChatCommandSecurity.Admin, "countobjects", new[] { "/countobjects" })
        {
        }

        public override void Help(bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/countobjects", "Counts number of in game floating objects.");
        }

        public override bool Invoke(string messageText)
        {
            if (messageText.Equals("/countobjects", StringComparison.InvariantCultureIgnoreCase))
            {
                var floatingList = new HashSet<IMyEntity>();

                // Meteor or FloatingObject??  have to use GetObjectBuilder().TypeId, as there isn't an interface to determine the difference.
                //MyAPIGateway.Entities.GetEntities(floatingList, e => !(e is Sandbox.ModAPI.IMyCubeGrid) && !(e is Sandbox.ModAPI.IMyVoxelBase) && !(e is IMyControllableEntity) && e.GetObjectBuilder().TypeId == typeof(MyObjectBuilder_FloatingObject));
                MyAPIGateway.Entities.GetEntities(floatingList, e => (e is Sandbox.ModAPI.IMyFloatingObject));
                MyAPIGateway.Utilities.ShowMessage("Floating objects", String.Format("{0}/{1}", floatingList.Count, MyAPIGateway.Session.SessionSettings.MaxFloatingObjects));
                return true;
            }

            return false;
        }
    }
}
