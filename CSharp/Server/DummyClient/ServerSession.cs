using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using ServerCore;

namespace DummyClient
{
    public abstract class Packet
    {
        public ushort size;
        public ushort packetId;

        public abstract ArraySegment<byte> Write();
        public abstract void Read(ArraySegment<byte> s);
    }
    class PlayerInfoReq : Packet
    {
        public long playerId;
        public PlayerInfoReq()
        {
            this.packetId = (ushort)PacketID.PlayerInfoReq;
        }


        public override void Read(ArraySegment<byte> s)
        {
            ushort count = 0;

            //ushort size = BitConverter.ToUInt16(s.Array, s.Offset);
            count += 2;
            //ushort id = BitConverter.ToUInt16(buffer.Array, buffer.Offset + count);
            count += 2;

            //this.playerId = BitConverter.ToInt64(s.Array, s.Offset + count);
            // 좀더 안전한 버전으로 사용(span)
            this.playerId = BitConverter.ToInt64(new ReadOnlySpan<byte>(s.Array, s.Offset + count, s.Count - count));

            count += 8;
            
        }

        public override ArraySegment<byte> Write()
        {
            ArraySegment<byte> s = SendBufferHelper.Open(4096);

            ushort count = 0;
            bool success = true;

            // [][] [][][][][][][][]
            //success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset, s.Count), packet.size);
            count += 2;
            success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset + count, s.Count - count), this.packetId);
            count += 2;
            success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset + count, s.Count - count), this.playerId);
            count += 8;
                        
            if (1)
            {
                // size는 맨 마지막에 메모리 맨 앞에 넣자.
                success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset, s.Count), count);
            }
            else
            {
                // 강제로 잘못된 값
                //success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset, s.Count), (ushort)4);             // 강제로 거짓만을 해보자.
            }

            if (success == false)
                return null;
            
            return SendBufferHelper.Close(count);
        }
    }
    

    public enum PacketID
    {
        PlayerInfoReq = 1,
        PlayerInfoOk = 2,

    }

    class ServerSession : Session
    {

        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnConnected {endPoint}");

            PlayerInfoReq packet = new PlayerInfoReq 
            { playerId  = 1001};

            // 보낸다.
            //for (int i = 0; i < 5; i++)
            {
                ArraySegment<byte> s = packet.Write();

                if(s != null)
                    Send(s);
            }
        }

        public override void OnDisconnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnDisconnected {endPoint}");
        }

        // 이동 패킷 ((3,2)좌표로 이동하고 싶다!)
        // 15 3 2
        public override int OnRecv(ArraySegment<byte> buffer)
        {
            string recvData = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
            Console.WriteLine($"[From Client]{recvData}");
            return buffer.Count;
        }

        public override void OnSend(int numOfBytes)
        {
            Console.WriteLine($"Tracnsterffed bytes: {numOfBytes}");
        }
    }
}
