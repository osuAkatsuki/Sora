using Sora.Enums;
using Sora.Packets.Client;
using Sora.Utilities;

namespace Sora.Packets.Server
{
    public class MatchScoreUpdate : IPacket
    {
        public ScoreFrame Frame;

        public MatchScoreUpdate(int slotId, ScoreFrame frame)
        {
            Frame = frame;
            Frame.Id = (byte) slotId;
        }

        public PacketId Id => PacketId.ServerMatchScoreUpdate;

        public void ReadFromStream(MStreamReader sr)
        {
            Frame = sr.ReadData<ScoreFrame>();
        }

        public void WriteToStream(MStreamWriter sw)
        {
            sw.Write(Frame);
        }
    }
}