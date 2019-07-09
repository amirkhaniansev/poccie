using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using PoccieServer.Models;
using PoccieServer.Constants;
using Newtonsoft.Json;

namespace PoccieServer
{
    public class Proxy : IDisposable
    {
        private readonly int port;
        private readonly IPEndPoint localEndpoint;
        private readonly UdpClient server;
        private readonly ConcurrentDictionary<string, User> users;
        private readonly bool logToConsole;
        private bool started;
        private bool stopPending;

        public Proxy(int port, bool logToConsole)
        {
            this.port = port;
            this.logToConsole = logToConsole;
            this.started = false;
            this.stopPending = false;
            this.localEndpoint = new IPEndPoint(IPAddress.Any, port);
            this.server = new UdpClient(this.localEndpoint);
            this.users = new ConcurrentDictionary<string, User>();
        }

        public async Task StartAsync()
        {
            try
            {
                this.started = true;
                this.LogMessage(Messages.Started);

                var recieveResult = default(UdpReceiveResult);
                var message = default(string);
                var request = default(Request);
                var user = default(User);
                var bytes = default(byte[]);
                while (!this.stopPending)
                {
                    this.LogMessage(Messages.WaitingForMessage);
                    recieveResult = await this.server.ReceiveAsync();
                    this.LogMessage($"Received from {recieveResult.RemoteEndPoint}");

                    this.LogMessage(Messages.Decoding);
                    message = Encoding.UTF8.GetString(recieveResult.Buffer);
                    this.LogMessage(Messages.Decoded);

                    this.LogMessage(Messages.Deserializing);
                    request = JsonConvert.DeserializeObject<Request>(message);
                    this.LogMessage(Messages.Deserialized);

                    if (request.RequestType == RequestType.Connect)
                    {
                        this.LogMessage(Messages.Connecting);
                        if (!this.users.TryGetValue(request.Content, out user))
                        {
                            user = new User
                            {
                                Username = request.Content
                            };
                            this.users.TryAdd(user.Username, user);
                        }

                        user.EndPoint = recieveResult.RemoteEndPoint;
                        user.Connected = DateTime.Now;
                        this.LogMessage(Messages.Connected);
                    }
                    else if (request.RequestType == RequestType.Address)
                    {
                        if (this.users.TryGetValue(request.Content, out user))
                        {
                            this.LogMessage(Messages.Addressing);

                            this.LogMessage(Messages.Serializing);
                            message = user.EndPoint.ToString();
                            this.LogMessage(Messages.Serialized);
                            
                            this.LogMessage(Messages.Encoding);
                            bytes = Encoding.UTF8.GetBytes(message);
                            this.LogMessage(Messages.Encoded);

                            await this.server.SendAsync(bytes, bytes.Length, recieveResult.RemoteEndPoint);
                            this.LogMessage(Messages.Addressed);
                        }
                    }
                    else continue;
                }
            }
            catch(Exception ex)
            {
                this.LogError(ex);
                throw;
            }
            finally
            {
                this.started = false;
            }
        }

        public void Stop()
        {
            this.stopPending = true;
        }

        public void Dispose()
        {
            this.server.Dispose();
        }

        private void LogError(Exception ex)
        {
            this.LogMessage(ex.Message);
        }

        private void LogMessage(string message)
        {
            if (this.logToConsole)
                Console.WriteLine("Time : {0} Message : {1}", DateTime.Now, message);
        }
    }
}