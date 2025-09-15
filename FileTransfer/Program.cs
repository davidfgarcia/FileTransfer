namespace FileTransferAssignment
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting execution...");

            // file mock-up data:
            string fileName = "daily_data_file.dat";
            int totalFilePartitions = 5;

            // start servers
            await Supervisor.StartServers();

            Server source = new Server(-1);
            // send file to an online server and ask server to propagate file
            foreach (Server srv in Supervisor.Servers.Values)
            {
                if (srv.Status == ServerStatus.Online)
                {
                    Parallel.For(0, totalFilePartitions, async partition =>
                    //for (int partition = 0; partition < totalFilePartitions; partition++)
                    {
                        try
                        {
                            Transferable partialFile = new Transferable(fileName, partition, totalFilePartitions, $"LINE {partition}", propagate: true);
                            //TransferRequestResponse response = srv.TransferSrvc.ProcessNotificationFromServer(partialFile.Metadata, srv.ServerId);
                            TransferRequestResponse response = await srv.TransferSrvc.ProcessNotificationAsync(partialFile.Metadata, srv.ServerId);

                            if (response == TransferRequestResponse.Accept)
                            {
                                await srv.TransferSrvc.ReceiveFromSourceAsync(partialFile, source);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to send file partition {partition}; attempting to send to a different server. Error message: {ex.Message}");
                            //continue;
                        }
                    });
                }
            }

            bool transfersInProgress = true;

            while (transfersInProgress)
            {
                transfersInProgress = false;

                foreach (Server srv in Supervisor.Servers.Values)
                {
                    if (srv.TransferSrvc.Status == ServiceStatus.ExecutingTransfers)
                    {
                        transfersInProgress = true;
                    }
                }
            }
            
            Console.WriteLine("Execution completed successfully.");
        }
    }
}
