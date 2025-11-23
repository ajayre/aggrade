using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace HardwareSim
{
    public class UDPServer
    {
        private const int ListenPort = 5000;
        private const int RemotePort = 5001;

        private IPEndPoint? RemoteEP = null;

        public void Send
            (
            )
        {
            if (RemoteEP != null)
            {

            }
        }

        public async Task StartListener()
        {
            using UdpClient listener = new UdpClient(ListenPort);
            IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, ListenPort);

            Console.WriteLine($"UDP Server listening on port {ListenPort}");
            Console.WriteLine("Waiting for incoming messages...");

            try
            {
                while (true)
                {
                    // Receive data
                    UdpReceiveResult result = await listener.ReceiveAsync();
                    byte[] receivedBytes = result.Buffer;
                    if (RemoteEP == null)
                    {
                        RemoteEP = result.RemoteEndPoint;
                    }

                    // fixme - remove
                    //string receivedMessage = Encoding.ASCII.GetString(receivedBytes);

                    //Console.WriteLine($"Received from {RemoteEP}: {receivedMessage}");

                    // Echo back the message
                    //byte[] responseBytes = Encoding.ASCII.GetBytes($"Echo: {receivedMessage}");
                    //await listener.SendAsync(responseBytes, responseBytes.Length, RemoteEP);
                    //Console.WriteLine($"Echoed back to {RemoteEP}");

                    await listener.SendAsync(receivedBytes, receivedBytes.Length, RemoteEP);
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine($"SocketException: {e.Message}");
            }
            finally
            {
                listener.Close();
            }
        }
    }
}
