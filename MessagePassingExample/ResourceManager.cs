using System;
using System.Collections.Generic;
using System.Text;

namespace MessagePassingExample
{
    public class ResourceManager
    {
        public void UpdateStatus()
        {
            if (true)
            {
                ResourceStatusEventArgs args = new ResourceStatusEventArgs();
                args.SomeValue = 42;
                args.Timestamp = DateTime.Now;
                OnResourceStatusUpdated(args);
            }
        }

        protected virtual void OnResourceStatusUpdated(ResourceStatusEventArgs e)
        {
            EventHandler<ResourceStatusEventArgs> handler = ThresholdReached;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public event EventHandler<ResourceStatusEventArgs> ThresholdReached;

        public string SendData(string data)
        {
            return "Reply received at " + DateTime.Now;
        }
    }

    public class ResourceStatusEventArgs : EventArgs
    {
        public int SomeValue { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
