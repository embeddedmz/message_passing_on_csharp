using System;
using System.Collections.Generic;
using System.Text;

namespace MessagePassingExample
{
    public class ResourceManager
    {
        public void UpdateStatus(int value)
        {
            ResourceStatusEventArgs args = new ResourceStatusEventArgs();
            args.SomeValue = value;
            args.Timestamp = DateTime.Now;
            OnResourceStatusUpdated(args);
        }

        protected virtual void OnResourceStatusUpdated(ResourceStatusEventArgs e)
        {
            EventHandler<ResourceStatusEventArgs> handler = ResourceStatusUpdated;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public event EventHandler<ResourceStatusEventArgs> ResourceStatusUpdated;

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
