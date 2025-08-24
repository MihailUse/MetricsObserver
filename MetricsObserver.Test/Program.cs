using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MetricsObserver.Test
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            if (!IPAddress.TryParse("127.0.0.1", out var address))
                throw new ArgumentException("Invalid host address");

            var ip = new IPEndPoint(address, 8888);
            var client = new UdpClient();
            var rnd = new Random();
            
            while (true)
            {
                var line = Console.ReadLine();
                if (line == "exit")
                    break;

                if (line == "huge")
                {
                    var randomBytes = new byte[260];
                    rnd.NextBytes(randomBytes);
                    client.Send(randomBytes, randomBytes.Length, ip);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var dgram = Encoding.UTF8.GetBytes(line);
                client.Send(dgram, dgram.Length, ip);
            }

            client.Close();
        }
    }
}
