using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PoccieServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                using(var proxy = new Proxy(5000, true))
                {
                    await proxy.StartAsync();
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
                Process.Start("dotnet", "PoccieServer.dll");
            }
        }
    }
}