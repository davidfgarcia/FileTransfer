using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace FileTransferAssignment
{
    public static class GloballyAccessible_MockUp
    {
        // region globally accessible fields
        public static Dictionary<int, Server> Servers { get; private set; }
        // end region

        // private fields
        private static Random _rnd;
        //

        static GloballyAccessible_MockUp()
        {
            Servers = new Dictionary<int, Server>();
            _rnd = new Random();
        }

        public static async Task<bool> StartServers()
        {
            Server srv;

            // this loop will run the start tasks in order
            for (int counter = 0; counter < Configuration.NUMBER_OF_SERVERS; counter++)
            {
                srv = await StartServerAsync(counter);

                if (srv.Status == ServerStatus.Online)
                {
                    Console.WriteLine($"Successfully started server; server counter: {counter}.");
                    Servers.Add(counter, srv);
                }
                else
                {
                    Console.WriteLine($"Failed to start server; server counter: {counter}.");
                }
            }

            return true;
        }

        private static async Task<Server> StartServerAsync(int serverId)
        {
            return await Task.Run(() => StartServer(serverId));
        }

        private static async Task<Server> StartServer(int serverId)
        {
            int retries = 0;
            Server srv = new Server(serverId);

            while (retries < Configuration.SERVER_START_RETRIES)
            {
                srv.Start();

                Console.WriteLine($"Starting server {serverId}.");

                // simulate startup time
                await Task.Delay(_rnd.Next(100, 200));

                Console.WriteLine($"Server {serverId} started.");

                if (srv.Status == ServerStatus.Online)
                {
                    break;
                }

                retries++;
            }

            return srv;
        }
    }
}