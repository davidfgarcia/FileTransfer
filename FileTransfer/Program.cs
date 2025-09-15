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
            await GloballyAccessible_MockUp.StartServers();

            // send file to an online server and ask server to propagate file
            foreach (Server srv in GloballyAccessible_MockUp.Servers.Values)
            {
                if (srv.Status == ServerStatus.Online)
                {
                    for (int partition = 0; partition < totalFilePartitions; partition++)
                    {
                        try
                        {
                            Transferable partialFile = new Transferable(fileName, partition, totalFilePartitions, $"LINE {partition}", propagate: true);
                            TransferRequestResponse response = srv.TransferSrvc.RequestSend(partialFile.Metadata, srv.ServerId);

                            if (response == TransferRequestResponse.Accept)
                            {
                                srv.TransferSrvc.Send(partialFile);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to send file partition {partition}; attempting to send to a different server. Error message: {ex.Message}");
                            continue;
                        }
                    }
                }
            }

            Thread.Sleep(10000);
            Console.WriteLine("Execution completed successfully.");
        }
    }
}
