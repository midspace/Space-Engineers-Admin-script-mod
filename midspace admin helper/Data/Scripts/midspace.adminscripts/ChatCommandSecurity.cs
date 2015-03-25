namespace midspace.adminscripts
{
    using System;

    public class ChatCommandSecurity
    {
        /// <summary>
        /// The normal average player can access these command
        /// </summary>
        public const uint User = 0;

        /// <summary>
        /// Player is Admin of game.
        /// </summary>
        public const uint Admin = 100;
    }
}
