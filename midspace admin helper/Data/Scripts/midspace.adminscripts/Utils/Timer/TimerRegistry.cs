using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace midspace.adminscripts.Utils.Timer
{
    public class TimerRegistry
    {
        private static HashSet<ThreadsafeTimer> _timers = new HashSet<ThreadsafeTimer>();

        public static void Add(ThreadsafeTimer timer)
        {
            _timers.Add(timer);
        }

        public static void Remove(ThreadsafeTimer timer)
        {
            _timers.Remove(timer);
        }

        public static void Update()
        {
            foreach (var timer in _timers)
            {
                timer.Update();
            }
        }

        public static void Close()
        {
            var tmp = new HashSet<ThreadsafeTimer>(_timers);
            // clear the set to avoid exceptions as the timers will try to remove themselves from the _timers set
            // wouldn't be smart to do that while iterating trough the mentioned set...
            _timers.Clear();
            foreach (var timer in tmp)
            {
                timer.Close();
            }
        }
    }
}
