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
    internal enum PGNValues
    {
        // misc
        PGN_ESTOP = 0x0000,
        PGN_RESET = 0x0001,
        PGN_OG3D_STARTED = 0x0002,
        PGN_PING = 0x0003,

        // blade control
        PGN_FRONT_CUT_VALVE = 0x1000,   // CUTVALVE_MIN -> CUTVALVE_MAX
        PGN_REAR_CUT_VALVE = 0x1001,   // CUTVALVE_MIN -> CUTVALVE_MAX
        PGN_FRONT_ZERO_BLADE_HEIGHT = 0x1002,
        PGN_REAR_ZERO_BLADE_HEIGHT = 0x1003,

        // blade configuration
        PGN_FRONT_PWM_GAIN_UP = 0x2002,
        PGN_FRONT_PWM_GAIN_DOWN = 0x2003,
        PGN_FRONT_PWM_MIN_UP = 0x2004,
        PGN_FRONT_PWM_MIN_DOWN = 0x2005,
        PGN_FRONT_PWM_MAX_UP = 0x2006,
        PGN_FRONT_PWM_MAX_DOWN = 0x2007,
        PGN_FRONT_INTEGRAL_MULTPLIER = 0x2008,
        PGN_FRONT_DEADBAND = 0x2009,
        PGN_REAR_PWM_GAIN_UP = 0x200A,
        PGN_REAR_PWM_GAIN_DOWN = 0x200B,
        PGN_REAR_PWM_MIN_UP = 0x200C,
        PGN_REAR_PWM_MIN_DOWN = 0x200D,
        PGN_REAR_PWM_MAX_UP = 0x200E,
        PGN_REAR_PWM_MAX_DOWN = 0x200F,
        PGN_REAR_INTEGRAL_MULTPLIER = 0x2010,
        PGN_REAR_DEADBAND = 0x2011,

        // autosteer control
        PGN_AUTOSTEER_RELAY = 0x3000,
        PGN_AUTOSTEER_SPEED = 0x3001,
        PGN_AUTOSTEER_DISTANCE = 0x3002,
        PGN_AUTOSTEER_ANGLE = 0x3003,

        // autosteer configuration
        PGN_AUTOSTEER_KP = 0x4000,
        PGN_AUTOSTEER_KI = 0x4001,
        PGN_AUTOSTEER_KD = 0x4002,
        PGN_AUTOSTEER_KO = 0x4003,
        PGN_AUTOSTEER_OFFSET = 0x4004,
        PGN_AUTOSTEER_MIN_PWM = 0x4005,
        PGN_AUTOSTEER_MAX_INTEGRAL = 0x4006,
        PGN_AUTOSTEER_COUNTS_PER_DEG = 0x4007,

        // blade status
        PGN_FRONT_BLADE_OFFSET_SLAVE = 0x5000,
        PGN_FRONT_BLADE_PWMVALUE = 0x5001,
        PGN_FRONT_BLADE_DIRECTION = 0x5002,
        PGN_FRONT_BLADE_AUTO = 0x5003,
        PGN_REAR_BLADE_OFFSET_SLAVE = 0x5004,
        PGN_REAR_BLADE_PWMVALUE = 0x5005,
        PGN_REAR_BLADE_DIRECTION = 0x5006,
        PGN_REAR_BLADE_AUTO = 0x5007,
        PGN_FRONT_BLADE_HEIGHT = 0x5008,
        PGN_REAR_BLADE_HEIGHT = 0x5009,

        // IMU
        PGN_TRACTOR_PITCH = 0x6000,
        PGN_TRACTOR_ROLL = 0x6001,
        PGN_TRACTOR_HEADING = 0x6002,
        PGN_TRACTOR_YAWRATE = 0x6003,
        PGN_TRACTOR_IMUCALIBRATION = 0x6004,
        PGN_FRONT_PITCH = 0x6005,
        PGN_FRONT_ROLL = 0x6006,
        PGN_FRONT_HEADING = 0x6007,
        PGN_FRONT_YAWRATE = 0x6008,
        PGN_FRONT_IMUCALIBRATION = 0x6009,
        PGN_REAR_PITCH = 0x600A,
        PGN_REAR_ROLL = 0x600B,
        PGN_REAR_HEADING = 0x600C,
        PGN_REAR_YAWRATE = 0x600D,
        PGN_REAR_IMUCALIBRATION = 0x600E,
    }

    internal struct AgGradeCommand
    {
        public PGNValues PGN;
        public UInt32 Value;
    }

    internal struct AgGradeStatus
    {
        public PGNValues PGN;
        public UInt32 Value;
    }

    internal class UDPServer
    {
        private const int ListenPort = 5000;
        private const int RemotePort = 5001;

        private IPEndPoint? RemoteEP = null;
        private UdpClient? listener = null;
        private readonly object lockObject = new object();
        
        public Packet Packet { get; } = new Packet();

        public event Action<AgGradeCommand> OnCommandReceived = null;

        public async Task Send
            (
            AgGradeStatus Status
            )
        {
            UdpClient? currentListener;
            IPEndPoint? currentRemoteEP;

            Packet SendPacket = new Packet();
            SendPacket.TxBuff[0] = (byte)((UInt16)Status.PGN & 0xFF);
            SendPacket.TxBuff[1] = (byte)(((UInt16)Status.PGN >> 8) & 0xFF);
            SendPacket.TxBuff[2] = (byte)(Status.Value & 0xFF);
            SendPacket.TxBuff[3] = (byte)((Status.Value >> 8) & 0xFF);
            SendPacket.TxBuff[4] = (byte)((Status.Value >> 16) & 0xFF);
            SendPacket.TxBuff[5] = (byte)((Status.Value >> 24) & 0xFF);

            byte Len = SendPacket.ConstructPacket(6);

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
            listener = new UdpClient(ListenPort);
            IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, ListenPort);

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
                            AgGradeCommand Command = new AgGradeCommand();

                            Command.PGN = (PGNValues)(((UInt16)Packet.RxBuff[1] << 8) | Packet.RxBuff[0]);
                            Command.Value = (UInt32)(((UInt32)Packet.RxBuff[5] << 24) |
                                ((UInt32)Packet.RxBuff[4] << 16) |
                                ((UInt32)Packet.RxBuff[3] << 8) |
                                Packet.RxBuff[2]);

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
                    if (listener != null)
                    {
                        listener.Close();
                        listener = null;
                    }
                }
            }
        }

        public void Stop()
        {
            lock (lockObject)
            {
                if (listener != null)
                {
                    listener.Close();
                    listener = null;
                }
            }
        }
    }
}
