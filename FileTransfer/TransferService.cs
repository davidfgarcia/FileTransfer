using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;

namespace FileTransferAssignment
{
    public class TransferService // : ITransferrer, etc.
    {
        private int _parentServerId;
        private Server _parentServer;
        private string _filesPath;
        private bool _fileRebuilt;
        private Random _rnd;
        private ConcurrentDictionary<string, Transfer> OutgoingTransfers;
        private ConcurrentDictionary<string, Transfer> IncomingTransfers;

        private TransferableMetadata _fileMetadata;

        public ServiceStatus Status { get; private set; }
        public TransferService(int parentServerId, Server parentServer)
        {
            IncomingTransfers = new ConcurrentDictionary<string, Transfer>();
            OutgoingTransfers = new ConcurrentDictionary<string, Transfer>();

            _parentServerId = parentServerId;
            _parentServer = parentServer;
            _rnd = new Random();
            _fileRebuilt = false;
            _filesPath = "output/server_" + _parentServerId.ToString() + "/files/";

            if (!Directory.Exists(_filesPath))
            {
                Directory.CreateDirectory(_filesPath);
            }
        }

        public TransferRequestResponse ProcessNotificationFromServer(TransferableMetadata metadata, int senderId)
        {
            string key = metadata.Id + metadata.Partition.ToString();
            Console.WriteLine($"Server {_parentServerId}: Received transfer request {key} from server {senderId}.");

            if (File.Exists(_filesPath + metadata.Id))
            {
                Console.WriteLine($"Server {_parentServerId}: Rejected transfer request {key} from server {senderId} - Transfer completed.");
                return TransferRequestResponse.TransferCompleted;
            }

            if (IncomingTransfers.Count < Configuration.MAX_CONCURRENT_INCOMING)
            {
                Console.WriteLine($"Server {_parentServerId}: Accepted transfer request {key} from server {senderId}.");
                return TransferRequestResponse.Accept;
            }
            else
            {
                Console.WriteLine($"Server {_parentServerId}: Rejected transfer request {key} from server {senderId} - Server busy.");
                return TransferRequestResponse.ServerBusy;
            }
        }

        public async Task ReceiveFromSourceAsync(Transferable package, Server source)
        {
            _fileMetadata = package.Metadata;
            string key = package.Id + package.Partition.ToString();

            Transfer newTransfer = new Transfer(package, source, _parentServer);
            IncomingTransfers.TryAdd(key, newTransfer);

            string value = $"Incoming trasfer received - File: {newTransfer.Package.Metadata.Id} - Source: {newTransfer.Source.ServerId} Destination: {newTransfer.Destination.ServerId}";
            Console.WriteLine(value);

            await Task.Delay(_rnd.Next(100, 500)); // simulate task duration
            await File.WriteAllTextAsync(_filesPath + package.Id + package.Partition, package.Contents);

            if (newTransfer.Status == TransferStatus.Ongoing)
            {
                newTransfer.Status = TransferStatus.Completed;
                IncomingTransfers.TryRemove(key, out _);
            }

            if (package.Propagate)
            {
                await SendToTargetServers(package);
            }

            if (IncomingTransfers.Count == 0)
            {
                await RebuildFileAsync();
            }
        }

        public async Task SendToTargetServers(Transferable package)
        {
            foreach (Server srv in Supervisor.Servers.Values)
            {
                if (srv.ServerId == this._parentServerId)
                {
                    continue;
                }

                if (srv.Status != ServerStatus.Online)
                {
                    continue;
                }

                if (srv.TransferSrvc.ProcessNotificationFromServer(package.Metadata, _parentServerId) == TransferRequestResponse.Accept)
                {
                    await srv.TransferSrvc.ReceiveFromSourceAsync(package, _parentServer);
                    string key = package.Id + package.Partition.ToString();
                    OutgoingTransfers.TryAdd(key, new Transfer(package, _parentServer, srv));

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
                    // keep accepting incoming requests to get the missing partition
                    Console.WriteLine($"Missing file partition: {0} - Message: {1}", partition, ex.ToString());
                }
            }

            File.WriteAllText(_filesPath + metadata.Id, sb.ToString());
            _fileRebuilt = true;

            if (Configuration.CLEAN_UP_PARTIAL_FILES)
            {
                CleanPartialFiles(metadata);
            }
        }

        public async Task RebuildFileAsync()
        {
            await Task.Run(() => RebuildFile());
        }

        public void RebuildFile()
        {
            if (_fileMetadata == null)
            {
                return;
            }

            StringBuilder sb = new StringBuilder();

            for (int partition = 0; partition < _fileMetadata.Partitions; partition++)
            {
                try
                {
                    sb.AppendLine(File.ReadAllText(_filesPath + _fileMetadata.Id + partition));
                }
                catch (FileNotFoundException ex)
                {
                    // ToDo: accept incoming requests to get the missing partition
                    Console.WriteLine($"MISSING PARTITION: {0}", ex.ToString());
                }
            }

            File.WriteAllText(_filesPath + _fileMetadata.Id, sb.ToString());
            _fileRebuilt = true;

            if (Configuration.CLEAN_UP_PARTIAL_FILES)
            {
                CleanPartialFiles(_fileMetadata);
            }
        }


        public void CleanPartialFiles(TransferableMetadata metadata)
        {
            // ToDo
        }
        public async Task Refresh()
        {
            if (IncomingTransfers.Count == 0 && !_fileRebuilt)
            {
                await RebuildFileAsync();
            }

            if (_fileRebuilt && OutgoingTransfers.Count == 0)
            {
                Status = ServiceStatus.AllTransfersCompleted;
                return;
            }

            Parallel.ForEach(IncomingTransfers, async kvp =>
            {
                if (kvp.Value.Status == TransferStatus.Completed)
                {
                    string key = kvp.Value.Package.Id + kvp.Value.Package.Partition.ToString();
                    IncomingTransfers.TryRemove(key, out _);
                }
            });

            Parallel.ForEach(OutgoingTransfers, async kvp =>
            {
                if (kvp.Value.Status == TransferStatus.Completed)
                {
                    string key = kvp.Value.Package.Id + kvp.Value.Package.Partition.ToString();
                    OutgoingTransfers.TryRemove(key, out _);
                }

                if (kvp.Value.Status == TransferStatus.Failed || kvp.Value.Status == TransferStatus.TimedOut)
                {
                    if (kvp.Value.Retries < Configuration.DATA_TRANSFER_RETRIES)
                    {
                        await kvp.Value.Destination.TransferSrvc.ReceiveFromSourceAsync(kvp.Value.Package, _parentServer);
                        kvp.Value.ResetStatus();
                        kvp.Value.Retries++;
                    }
                }
            });
        }

        public async Task<TransferRequestResponse> ProcessNotificationAsync(TransferableMetadata metadata, int senderId)
        {
            return await Task.Run(() => ProcessNotificationFromServer(metadata, senderId));
        }
    }

    public enum TransferRequestResponse
    {
        Accept = 0,
        ServerBusy,
        TransferCompleted,
        UnknownTransferState
    }

    public enum ServiceStatus
    {
        ExecutingTransfers = 0,
        AllTransfersCompleted
    }
}