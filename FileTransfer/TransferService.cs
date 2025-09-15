using System.Text;

namespace FileTransferAssignment
{
    public class TransferService // : ITransferrer, etc.
    {
        private int _parentServer;
        private string _filesPath;

        public Dictionary<string, int> ExpectedIncoming;

        private List<Transfer> OutgoingTransfers;
        public TransferService(int parentServer)
        {
            ExpectedIncoming = new Dictionary<string, int>();
            OutgoingTransfers = new List<Transfer>();

            _parentServer = parentServer;
            _filesPath = "output/server_" + _parentServer.ToString() + "/files/";

            if (!Directory.Exists(_filesPath))
            {
                Directory.CreateDirectory(_filesPath);
            }
        }

        public TransferRequestResponse RequestSend(TransferableMetadata metadata, int senderId)
        {
            string key = metadata.Id + metadata.Partition.ToString();
            Console.WriteLine($"Server {_parentServer}: Received transfer request {key} from server {senderId}.");

            if (!ExpectedIncoming.ContainsKey(key))
            {
                for (int count = 0; count < metadata.Partitions; count++)
                {
                    ExpectedIncoming.TryAdd(metadata.Id + count.ToString(), 0);
                }

                Console.WriteLine($"Server {_parentServer}: Accepted transfer request {key} from server {senderId}.");
                return TransferRequestResponse.Accept;
            }

            if (ExpectedIncoming[key] < Configuration.MAX_CONCURRENT_INCOMING)
            {
                Console.WriteLine($"Server {_parentServer}: Accepted transfer request {key} from server {senderId}.");
                return TransferRequestResponse.Accept;
            }

            if (ExpectedIncoming[key] >= Configuration.MAX_CONCURRENT_INCOMING)
            {
                Console.WriteLine($"Server {_parentServer}: Rejected transfer request {key} from server {senderId} - Server busy.");
                return TransferRequestResponse.ServerBusy;
            }

            // ToDo: if transfer completed, return completed
            if (File.Exists(_filesPath + metadata.Id))
            {
                Console.WriteLine($"Server {_parentServer}: Rejected transfer request {key} from server {senderId} - Transfer completed.");
                return TransferRequestResponse.TransferCompleted;
            }

            Console.WriteLine($"Server {_parentServer}: Rejected transfer request {key} from server {senderId} - Unknown server state.");
            return TransferRequestResponse.UnknownTransferState;
        }

        public void Send(Transferable sendable)
        {
            string key = sendable.Id + sendable.Partition.ToString();

            if (ExpectedIncoming.ContainsKey(key))
            {
                ExpectedIncoming[key]++;
            }
            else
            {
                ExpectedIncoming[key] = 1;
            }

            Console.WriteLine($"Expected incoming: {key} - Active transfers: {ExpectedIncoming[key]}");

            File.WriteAllText(_filesPath + sendable.Id + sendable.Partition, sendable.Contents);

            if (sendable.Propagate)
            {
                SendToTargetServers(sendable);
            }
        
            if (sendable.Partition == sendable.Partitions - 1)
            {
                RebuildFile(sendable.Metadata);             
            }
        }

        public void SendToTargetServers(Transferable package)
        {
            foreach (Server srv in GloballyAccessible_MockUp.Servers.Values)
            {
                if (srv.ServerId == this._parentServer)
                {
                    continue;
                }

                if (srv.Status != ServerStatus.Online)
                {
                    continue;
                }

                if (srv.TransferSrvc.RequestSend(package.Metadata, _parentServer) == TransferRequestResponse.Accept)
                {
                    srv.TransferSrvc.Send(package);
                    OutgoingTransfers.Add(new Transfer(package, srv));

                    if (OutgoingTransfers.Count >= Configuration.MAX_CONCURRENT_OUTGOING)
                    {
                        break;
                    }
                }
            }
        }

        public void RebuildFile(TransferableMetadata metadata)
        {
            StringBuilder sb = new StringBuilder();

            for (int partition = 0; partition < metadata.Partitions; partition++)
            {
                try
                {
                    sb.AppendLine(File.ReadAllText(_filesPath + metadata.Id + partition));
                }
                catch (FileNotFoundException ex)
                {
                    // ToDo: accept incoming requests to get the missing partition
                    Console.WriteLine($"MISSING PARTITION: {0}", ex.ToString());
                    throw;
                }
            }

            File.WriteAllText(_filesPath + metadata.Id, sb.ToString());

            if (Configuration.CLEAN_UP_PARTIAL_FILES)
            {
                CleanPartialFiles(metadata);
            }
        }

        public void CleanPartialFiles(TransferableMetadata metadata)
        {
            // ToDo
        }
        public void Refresh()
        {
            for (int index = OutgoingTransfers.Count - 1; index >= 0; index--)
            {
                if (OutgoingTransfers[index].Status == TransferStatus.Completed)
                {
                    OutgoingTransfers.RemoveAt(index);
                    continue;
                }

                if (OutgoingTransfers[index].Status == TransferStatus.Failed || OutgoingTransfers[index].Status == TransferStatus.TimedOut)
                {
                    if (OutgoingTransfers[index].Retries < Configuration.DATA_TRANSFER_RETRIES)
                    {
                        OutgoingTransfers[index].Destination.TransferSrvc.Send(OutgoingTransfers[index].Package);
                        OutgoingTransfers[index].ResetTimer();
                        OutgoingTransfers[index].Retries++;
                    }
                }
            }
        }
    }

    public enum TransferRequestResponse
    {
        Accept = 0,
        ServerBusy,
        TransferCompleted,
        UnknownTransferState
    }
}