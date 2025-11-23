using System;
using System.Net;
using System.Net.Sockets;

namespace Controller
{
    /// <summary>
    /// UDP-based packet transfer class with the same functionality as SerialTransfer
    /// using UDP networking instead of serial communication
    /// </summary>
    internal class UDPTransfer
    {
        // Public members
        public Packet Packet { get; } = new Packet();
        public byte BytesRead { get; private set; } = 0;
        public int Status { get; private set; } = 0;

        // Private members
        private UdpClient _udpClient;
        private IPEndPoint _remoteEndPoint;
        private IPEndPoint _localEndPoint;
        private uint _timeout = 50; // Default timeout value
        private bool _isOpen = false;
        
        // Buffer for received UDP data
        private byte[] _receiveBuffer = new byte[65507]; // Max UDP packet size
        private int _receiveBufferIndex = 0;
        private int _receiveBufferLength = 0;

        /// <summary>
        /// Advanced initializer for the UDPTransfer Class
        /// </summary>
        /// <param name="remoteAddress">Remote IP address to send to</param>
        /// <param name="remotePort">Remote port number</param>
        /// <param name="localPort">Local port to bind to (0 for auto-assign)</param>
        /// <param name="configs">Configuration struct</param>
        public void Begin(IPAddress remoteAddress, int remotePort, int localPort, ConfigST configs)
        {
            _remoteEndPoint = new IPEndPoint(remoteAddress, remotePort);
            _localEndPoint = new IPEndPoint(IPAddress.Any, localPort);
            
            _udpClient = new UdpClient(_localEndPoint);
            _udpClient.Client.ReceiveTimeout = (int)configs.Timeout;
            _udpClient.Client.SendTimeout = (int)configs.Timeout;
            
            _timeout = configs.Timeout;
            _isOpen = true;
            Packet.Begin(configs);
        }

        /// <summary>
        /// Simple initializer for the UDPTransfer Class
        /// </summary>
        /// <param name="remoteAddress">Remote IP address to send to</param>
        /// <param name="remotePort">Remote port number</param>
        /// <param name="subnetMask">IP for subnet mask</param>
        /// <param name="localPort">Local port to bind to (0 for auto-assign)</param>
        /// <param name="debug">Whether to print debug messages</param>
        /// <param name="debugPort">Stream for debug output</param>
        /// <param name="timeout">Communication timeout in milliseconds</param>
        public void Begin(
            IPAddress remoteAddress,
            int remotePort,
            IPAddress subnetMask,
            int localPort = 0,
            bool debug = true,
            System.IO.Stream debugPort = null,
            uint? timeout = null)
        {
            _remoteEndPoint = new IPEndPoint(remoteAddress, remotePort);
            _localEndPoint = new IPEndPoint(remoteAddress, localPort);
            
            _udpClient = new UdpClient(_localEndPoint);
            _timeout = timeout ?? 50; // Default 50ms timeout
            
            // Configure timeouts
            _udpClient.Client.ReceiveTimeout = (int)_timeout;
            _udpClient.Client.SendTimeout = (int)_timeout;
            
            _isOpen = true;
            Packet.Begin(debug, debugPort ?? Console.OpenStandardOutput(), _timeout);
        }

        /// <summary>
        /// Send a specified number of bytes in packetized form
        /// </summary>
        /// <param name="messageLen">Number of bytes in the payload</param>
        /// <param name="packetId">The packet identifier</param>
        /// <returns>Number of payload bytes included in packet</returns>
        public byte SendData(ushort messageLen, byte packetId = 0)
        {
            try
            {
                if (!_isOpen || _udpClient == null)
                {
                    Status = (int)PacketStatus.PayloadError;
                    return 0;
                }

                byte numBytesIncl = Packet.ConstructPacket(messageLen, packetId);

                // Construct the complete packet
                int totalPacketSize = Packet.Preamble.Length + numBytesIncl + Packet.Postamble.Length;
                byte[] packet = new byte[totalPacketSize];
                
                int offset = 0;
                Array.Copy(Packet.Preamble, 0, packet, offset, Packet.Preamble.Length);
                offset += Packet.Preamble.Length;
                Array.Copy(Packet.TxBuff, 0, packet, offset, numBytesIncl);
                offset += numBytesIncl;
                Array.Copy(Packet.Postamble, 0, packet, offset, Packet.Postamble.Length);

                // Send the complete packet as a UDP datagram
                _udpClient.Send(packet, packet.Length, _remoteEndPoint);

                return numBytesIncl;
            }
            catch (SocketException)
            {
                Status = (int)PacketStatus.StalePacketError;
                return 0;
            }
            catch (Exception)
            {
                Status = (int)PacketStatus.PayloadError;
                return 0;
            }
        }

        /// <summary>
        /// Parse incoming data and report errors/successful packet reception
        /// </summary>
        /// <returns>Number of bytes in RX buffer</returns>
        public byte Available()
        {
            if (!_isOpen || _udpClient == null)
            {
                Status = (int)PacketStatus.PayloadError;
                return 0;
            }

            bool valid = false;
            byte recChar = 0xFF;

            try
            {
                // Check if we have buffered data to process
                if (_receiveBufferIndex < _receiveBufferLength)
                {
                    valid = true;
                    recChar = _receiveBuffer[_receiveBufferIndex++];
                    BytesRead = Packet.Parse(recChar, valid);
                    Status = Packet.Status;

                    if (Status != (int)PacketStatus.Continue)
                    {
                        if (Status < 0)
                            Reset();
                        return BytesRead;
                    }
                }
                else
                {
                // Try to receive new UDP packet (non-blocking check)
                if (_udpClient.Client.Available > 0)
                {
                    IPEndPoint remoteEP = null;
                    byte[] receivedData = _udpClient.Receive(ref remoteEP);
                    
                    // Update remote endpoint if it changed
                    if (remoteEP != null)
                    {
                        _remoteEndPoint = remoteEP;
                    }
                    
                    // Copy received data to buffer for byte-by-byte processing
                    Array.Copy(receivedData, 0, _receiveBuffer, 0, receivedData.Length);
                    _receiveBufferLength = receivedData.Length;
                    _receiveBufferIndex = 0;
                    
                    // Process first byte
                    if (_receiveBufferLength > 0)
                    {
                        valid = true;
                        recChar = _receiveBuffer[_receiveBufferIndex++];
                        BytesRead = Packet.Parse(recChar, valid);
                        Status = Packet.Status;

                        if (Status != (int)PacketStatus.Continue)
                        {
                            if (Status < 0)
                                Reset();
                            return BytesRead;
                        }
                    }
                }
                    else
                    {
                        // No data available, call parse with invalid flag
                        BytesRead = Packet.Parse(recChar, valid);
                        Status = Packet.Status;

                        if (Status < 0)
                            Reset();
                    }
                }

                // Continue processing remaining buffered bytes
                while (_receiveBufferIndex < _receiveBufferLength)
                {
                    recChar = _receiveBuffer[_receiveBufferIndex++];
                    BytesRead = Packet.Parse(recChar, valid);
                    Status = Packet.Status;

                    if (Status != (int)PacketStatus.Continue)
                    {
                        if (Status < 0)
                            Reset();
                        break;
                    }
                }

                return BytesRead;
            }
            catch (SocketException ex)
            {
                // Handle timeout or socket errors
                if (ex.SocketErrorCode == SocketError.TimedOut)
                {
                    Status = (int)PacketStatus.StalePacketError;
                }
                else
                {
                    Status = (int)PacketStatus.PayloadError;
                }
                Reset();
                return 0;
            }
            catch (Exception)
            {
                // Handle any other errors
                Status = (int)PacketStatus.PayloadError;
                Reset();
                return 0;
            }
        }

        /// <summary>
        /// Checks if any packets have been fully parsed
        /// </summary>
        /// <returns>Whether a full packet has been parsed</returns>
        public bool Tick()
        {
            return Available() > 0;
        }

        /// <summary>
        /// Copy an object into the transmit buffer
        /// </summary>
        /// <typeparam name="T">Type of object to transmit</typeparam>
        /// <param name="val">Object to transmit</param>
        /// <param name="index">Starting index in the transmit buffer</param>
        /// <param name="len">Number of bytes to transmit</param>
        /// <returns>Index after the transmitted object</returns>
        public ushort TxObj<T>(T val, ushort index = 0, ushort? len = null) where T : struct
        {
            return Packet.TxObj(val, index, len);
        }

        /// <summary>
        /// Copy bytes from the receive buffer into an object
        /// </summary>
        /// <typeparam name="T">Type of object to receive into</typeparam>
        /// <param name="val">Object to receive into</param>
        /// <param name="index">Starting index in the receive buffer</param>
        /// <param name="len">Number of bytes to receive</param>
        /// <returns>Index after the received object</returns>
        public ushort RxObj<T>(ref T val, ushort index = 0, ushort? len = null) where T : struct
        {
            return Packet.RxObj(ref val, index, len);
        }

        /// <summary>
        /// Pack an object and send it in a single call
        /// </summary>
        /// <typeparam name="T">Type of object to send</typeparam>
        /// <param name="val">Object to send</param>
        /// <param name="len">Number of bytes to send</param>
        /// <returns>Number of payload bytes included in packet</returns>
        public byte SendDatum<T>(T val, ushort? len = null) where T : struct
        {
            if (len == null)
            {
                len = (ushort)System.Runtime.InteropServices.Marshal.SizeOf(val);
            }

            return SendData(Packet.TxObj(val, 0, len));
        }

        /// <summary>
        /// Returns the ID of the last parsed packet
        /// </summary>
        /// <returns>ID of the last parsed packet</returns>
        public byte CurrentPacketId() => Packet.CurrentPacketId();

        /// <summary>
        /// Clear buffers and reset the parser state
        /// </summary>
        public void Reset()
        {
            try
            {
                // Clear the receive buffer
                _receiveBufferIndex = 0;
                _receiveBufferLength = 0;
                Array.Clear(_receiveBuffer, 0, _receiveBuffer.Length);
            }
            catch (Exception)
            {
                // Ignore errors while resetting
            }

            Packet.Reset();
            Status = Packet.Status;
        }

        /// <summary>
        /// Properly close and dispose the UDP connection
        /// </summary>
        public void Close()
        {
            if (_udpClient != null)
            {
                try
                {
                    _isOpen = false;
                    _udpClient.Close();
                    _udpClient.Dispose();
                }
                catch (Exception)
                {
                    // Ignore errors during close
                }
                finally
                {
                    _udpClient = null;
                }
            }
        }

        /// <summary>
        /// Get the local endpoint (IP and port) that the UDP client is bound to
        /// </summary>
        /// <returns>Local IPEndPoint</returns>
        public IPEndPoint GetLocalEndPoint()
        {
            if (_udpClient != null && _isOpen)
            {
                return (IPEndPoint)_udpClient.Client.LocalEndPoint;
            }
            return _localEndPoint;
        }

        /// <summary>
        /// Get the remote endpoint (IP and port) that packets are being sent to
        /// </summary>
        /// <returns>Remote IPEndPoint</returns>
        public IPEndPoint GetRemoteEndPoint()
        {
            return _remoteEndPoint;
        }
    }
}
