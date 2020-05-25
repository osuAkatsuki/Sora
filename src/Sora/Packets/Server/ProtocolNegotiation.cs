﻿using Sora.Enums;
using Sora.Utilities;

namespace Sora.Packets.Server
{
    public class ProtocolNegotiation : IPacket
    {
        public uint Protocol = 19; // Latest known protocol version
        public PacketId Id => PacketId.ServerProtocolNegotiation;

        public void ReadFromStream(MStreamReader sr)
        {
            Protocol = sr.ReadUInt32();
        }

        public void WriteToStream(MStreamWriter sw)
        {
            sw.Write(Protocol);
        }
    }
}