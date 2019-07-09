using Newtonsoft.Json;
using PoccieServer;
using PoccieServer.Models;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Poccie
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.Write("Username : ");
            var username = Console.ReadLine();

            try
            {
                using(var client = new UdpClient())
                {
                    var userRequest = new Request
                    {
                        Content = username,
                        RequestType = RequestType.Connect
                    };

                    var userJson = JsonConvert.SerializeObject(userRequest);
                    var userBytes = Encoding.UTF8.GetBytes(userJson);

                    var endpoint = new IPEndPoint(IPAddress.Loopback, 5000);

                    await client.SendAsync(userBytes, userBytes.Length, endpoint);

                    Console.Write("Connect : ");
                    var connectTo = Console.ReadLine();

                    var addressRequest = new Request
                    {
                        Content = connectTo,
                        RequestType = RequestType.Address
                    };

                    var addressJson = JsonConvert.SerializeObject(addressRequest);
                    var addressBytes = Encoding.UTF8.GetBytes(addressJson);

                    await client.SendAsync(addressBytes, addressBytes.Length, endpoint);

                    var addressResult = default(UdpReceiveResult);
                    while (true)
                    {
                        addressResult = await client.ReceiveAsync();
                        if (addressResult.RemoteEndPoint.Equals(endpoint))
                            break;
                    }

                    var addressResultJson = Encoding.UTF8.GetString(addressResult.Buffer);
                    var addressResultSplit = addressResultJson.Split(new[] { ':' });
                    var address = new IPEndPoint(
                        IPAddress.Parse(addressResultSplit[0]),
                        int.Parse(addressResultSplit[1]));

                    Console.WriteLine($"Connected to {connectTo} : {address}");

                    var writer = Task.Run(async () =>
                    {
                        while (true)
                        {
                            var message = Console.ReadLine();
                            var bytes = Encoding.UTF8.GetBytes(message);

                            await client.SendAsync(bytes, bytes.Length, address);
                        }
                    });

                    var reader = Task.Run(async () =>
                    {
                        while(true)
                        {
                            var result = await client.ReceiveAsync();
                            if (!result.RemoteEndPoint.Equals(address))
                                continue;

                            var message = Encoding.UTF8.GetString(result.Buffer);
                            Console.WriteLine(message);
                        }
                    });

                    Task.WaitAll(reader, writer);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Time : {DateTime.Now} Error : {ex.Message}");
            }
        }
    }
}