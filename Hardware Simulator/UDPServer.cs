using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Windows.Forms.AxHost;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace HardwareSim
{
    internal class UDPServer
    {
        private const int ListenPort = 8888;
        private const int RemotePort = 6000;

        private IPEndPoint? RemoteEP = null;
        private UdpClient? listener = null;
        private readonly object lockObject = new object();
        
        public Packet Packet { get; } = new Packet();

        public event Action<PGNPacket> OnCommandReceived = null;
        public event Action OnAgGradeClosed = null;

        public async Task Send
            (
            PGNPacket Status
            )
        {
            UdpClient? currentListener;
            IPEndPoint? currentRemoteEP;

            Packet SendPacket = new Packet();
            SendPacket.TxBuff[0] = (byte)((UInt16)Status.PGN & 0xFF);
            SendPacket.TxBuff[1] = (byte)(((UInt16)Status.PGN >> 8) & 0xFF);
            
            for (int b = 0; b < PGNPacket.MAX_LEN; b++)
            {
                SendPacket.TxBuff[2 + b] = Status.Data[b];
            }

            byte Len = SendPacket.ConstructPacket(PGNPacket.MAX_LEN + 2);

            lock (lockObject)
            {
                currentListener = listener;
                currentRemoteEP = RemoteEP;
            }

            if (currentRemoteEP != null && currentListener != null)
            {
                currentRemoteEP.Port = RemotePort;

                try
                {
                    // Construct the complete packet using SendPacket's buffers
                    int totalPacketSize = SendPacket.Preamble.Length + Len + SendPacket.Postamble.Length;
                    byte[] packet = new byte[totalPacketSize];

                    int offset = 0;
                    Array.Copy(SendPacket.Preamble, 0, packet, offset, SendPacket.Preamble.Length);
                    offset += SendPacket.Preamble.Length;
                    Array.Copy(SendPacket.TxBuff, 0, packet, offset, Len);
                    offset += Len;
                    Array.Copy(SendPacket.Postamble, 0, packet, offset, SendPacket.Postamble.Length);

                    await currentListener.SendAsync(packet, packet.Length, currentRemoteEP);
                }
                catch (SocketException e)
                {
                    Console.WriteLine($"SocketException while sending: {e.Message}");
                }
                catch (ObjectDisposedException)
                {
                    Console.WriteLine("Cannot send: UDP listener has been disposed");
                }
            }
        }

        public async Task StartListener()
        {
            // Ensure any previous listener is fully closed before creating a new one
            Stop();

            listener = new UdpClient();
            listener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            listener.Client.Bind(new IPEndPoint(IPAddress.Any, ListenPort));

            Packet.Begin(false, null, 50);

            Console.WriteLine($"UDP Server listening on port {ListenPort}");
            Console.WriteLine("Waiting for incoming messages...");

            try
            {
                while (true)
                {
                    // Receive data
                    UdpReceiveResult result = await listener.ReceiveAsync();
                    byte[] receivedBytes = result.Buffer;
                    
                    lock (lockObject)
                    {
                        if (RemoteEP == null)
                        {
                            RemoteEP = result.RemoteEndPoint;
                        }
                    }

                    foreach (byte b in receivedBytes)
                    {
                        byte BytesRead = Packet.Parse(b);
                        if (Packet.Status != (int)PacketStatus.Continue)
                        {
                            if (Packet.Status < 0)
                                Packet.Reset();
                        }

                        if (BytesRead > 0)
                        {
                            PGNPacket Command = new PGNPacket();

                            Command.PGN = (PGNValues)(((UInt16)Packet.RxBuff[1] << 8) | Packet.RxBuff[0]);

                            for (int pb = 0; pb < PGNPacket.MAX_LEN; pb++)
                            {
                                Command.Data[pb] = Packet.RxBuff[2 + pb];
                            }

                            OnCommandReceived?.Invoke(Command);
                        }
                    }

                    // fixme - remove
                    //string receivedMessage = Encoding.ASCII.GetString(receivedBytes);

                    //Console.WriteLine($"Received from {RemoteEP}: {receivedMessage}");

                    // Echo back the message
                    //byte[] responseBytes = Encoding.ASCII.GetBytes($"Echo: {receivedMessage}");
                    //await listener.SendAsync(responseBytes, responseBytes.Length, RemoteEP);
                    //Console.WriteLine($"Echoed back to {RemoteEP}");

                    // Note: This echo is optional - can be removed if not needed
                    // The server can now send independently via the Send() method
                    //await listener.SendAsync(receivedBytes, receivedBytes.Length, RemoteEP);
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine($"SocketException: {e.Message}");
            }
            catch (ObjectDisposedException)
            {
                // Listener was disposed, exit gracefully
                Console.WriteLine("UDP Server listener closed");
            }
            finally
            {
                lock (lockObject)
                {
                    RemoteEP = null;
                    if (listener != null)
                    {
                        try
                        {
                            listener.Close();
                        }
                        catch (SocketException)
                        {
                            // Socket may be in error state after remote close; still release reference
                        }
                        listener = null;
                    }
                }

                OnAgGradeClosed?.Invoke();
            }
        }

        public void Stop()
        {
            lock (lockObject)
            {
                RemoteEP = null;
                if (listener != null)
                {
                    try
                    {
                        listener.Close();
                    }
                    catch (SocketException) { /* ignore */ }
                    listener = null;
                }
            }
        }
    }
}
