﻿using Lidgren.Network;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bearded_Dangerzone.GamePart
{
    class Client:IFocusable
    {
        private Vector2 position;
        public Vector2 Position { get {return position;} set{ position = value;} }
        public byte AnimationState { get; set; }
        public string Name { get; set; }
        public ushort ID { get; set; }
        public bool Disconnected { get; set; }
        public byte Health { get; set; }
        public NetConnection Connection { get; set; }
        public byte Type { get; set; }
        public Client(string clientName, ushort clientID, NetConnection connection)
        {
            Disconnected = false;
            ID = clientID;
            Name = clientName;
            AnimationState = 0;
            Connection = connection;
        }
        public Client()
        {

        }
        public void SetAll(string name, ushort id, Vector2 pos,byte type)
        {
            Name = name;
            ID = id;
            Position = pos;
            Type = type;
        }



        public bool Flip { get; set; }
    }
}
