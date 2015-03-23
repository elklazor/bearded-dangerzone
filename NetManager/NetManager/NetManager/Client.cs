using Lidgren.Network;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetManager
{
    class Client:ITrackable
    {

        public Vector2 Position { get; set; }
        public byte AnimationState { get; set; }
        public string Name { get; private set; }
        public ushort ID { get; private set; }
        public bool Disconnected { get; set; }
        public byte Health { get; set; }
        public NetConnection Connection { get; private set; }
        public Client(string clientName, ushort clientID, NetConnection connection)
        {
            Disconnected = false;
            ID = clientID;
            Name = clientName;
            AnimationState = 0;
            Connection = connection;
        }

    }
}
