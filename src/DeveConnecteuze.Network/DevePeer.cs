using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeveConnecteuze.Network
{
    public abstract class DevePeer
    {
        protected DeveQueue<DeveIncomingMessage> messages = new DeveQueue<DeveIncomingMessage>(100);

        internal int maxMessageSize = 100000;
        public int MaxMessageSize
        {
            get { return maxMessageSize; }
            set { maxMessageSize = value; }
        }

        public DevePeer()
        {

        }

        public DeveIncomingMessage ReadMessage()
        {
            if (messages.Count == 0)
            {
                return null;
            }
            else
            {
                DeveIncomingMessage retval;
                Boolean didItWork = messages.TryDequeue(out retval);
                if (!didItWork)
                {
                    throw new Exception("Strange error");
                }
                return retval;
            }
        }

        internal void AddDeveIncomingMessage(DeveIncomingMessage devInc)
        {
            messages.Enqueue(devInc);
        }

        public abstract void Start();
        public abstract void Stop();
    }
}
