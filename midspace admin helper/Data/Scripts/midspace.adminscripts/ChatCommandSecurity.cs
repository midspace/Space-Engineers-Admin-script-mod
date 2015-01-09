namespace midspace.adminscripts
{
    using System;

    [Flags]
    public enum ChatCommandSecurity
    {
        /// <summary>
        /// Default state, uninitilized.
        /// </summary>
        None = 0x0,

        /// <summary>
        /// The normal average player can access these command
        /// </summary>
        User = 0x1,

        /// <summary>
        /// Player is Admin of game.
        /// </summary>
        Admin = 0x2,

        /// <summary>
        /// Player is designer or creator. Used for testing commands only.
        /// </summary>
        Experimental = 0x4
    };
}
