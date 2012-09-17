using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeveConnecteuze.Network
{
    public abstract class DevePeer
    {
        protected DeveQueue<DeveIncommingMessage> messages = new DeveQueue<DeveIncommingMessage>(100);

        internal int maxMessageSize = 100000;
        public int MaxMessageSize
        {
            get { return maxMessageSize; }
            set { maxMessageSize = value; }
        }

        public DevePeer()
        {

        }

        public DeveIncommingMessage ReadMessage()
        {
            if (messages.Count == 0)
            {
                return null;
            }
            else
            {
                DeveIncommingMessage retval;
                Boolean didItWork = messages.TryDequeue(out retval);
                if (!didItWork)
                {
                    throw new Exception("Strange error");
                }
                return retval;
            }
        }

        internal void AddDeveIncommingMessage(DeveIncommingMessage devInc)
        {
            messages.Enqueue(devInc);
        }

        public abstract void Start();
        public abstract void Stop();
    }
}
