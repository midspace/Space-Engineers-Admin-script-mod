using System;
using System.Timers;

namespace midspace.adminscripts.Utils.Timer
{
    public class ThreadsafeTimer
    {
        public Action<object, ElapsedEventArgs> Elapsed;
        public Action<object, EventArgs> Disposed;

        private bool _elapsed;
        private ElapsedEventArgs _elapsedArgs;
        private object _senderElapsed;


        private System.Timers.Timer Timer { get; set; }

        public bool Enabled { get { return Timer.Enabled; } set { Timer.Enabled = value; } }
        public bool AutoReset { get { return Timer.AutoReset; } set { Timer.AutoReset = value; } }
        public double Interval { get { return Timer.Interval; } set { Timer.Interval = value; } }

        public ThreadsafeTimer()
        {
            Timer = new System.Timers.Timer();
            Init();
        }

        public ThreadsafeTimer(double interval)

        {
            Timer = new System.Timers.Timer(interval);
            Init();
        }

        private void Init()
        {
            Timer.Elapsed += Timer_Elapsed;

            TimerRegistry.Add(this);
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _senderElapsed = sender;
            _elapsedArgs = e;
            _elapsed = true;
        }

        public void Update()
        {
            if (!_elapsed) 
                return;

            _elapsed = false;

            Elapsed.Invoke(_senderElapsed, _elapsedArgs);
        }

        public void Close()
        {
            TimerRegistry.Remove(this);
            Timer.Close();
        }

        public void Start()
        {
            Timer.Start();
        }

        public void Stop()
        {
            Timer.Stop();
        }

        public override string ToString()
        {
            return Timer.ToString();
        }

        public override bool Equals(object obj)
        {
            var comp = obj as ThreadsafeTimer;

            if (comp == null)
                return false;

            return Timer.Equals(comp.Timer);
        }

        public override int GetHashCode()
        {
            return Timer.GetHashCode();
        }
    }
}