using System.Threading.Tasks;
using FileTransferAssignment;

public class Server
{
    public ServerStatus Status {get; private set;} = ServerStatus.Offline;
    public Guid SessionId {get; private set;} = Guid.Empty;
    public int ServerId { get; private set; }
    public TransferService TransferSrvc { get; private set; }
    private System.Timers.Timer _timer;
    private Random _rnd;

    public Server(int serverId)
    {
        ServerId = serverId;
        TransferSrvc = new TransferService(serverId, this);

        _rnd = new Random();
        _timer = new System.Timers.Timer(Configuration.HEARTBEAT_FREQUENCY);
        _timer.Elapsed += OnTimeElapsed;
        _timer.Start();
    }

    private async void OnTimeElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        if (_rnd.NextDouble() < .05) // ToDo: 5% chance of the server going offline
        {
            // simulate unintentional shutdown
            ShutDown(false);
            Status = ServerStatus.Offline;
        }

        // refresh transfers status
        await TransferSrvc.Refresh();
    }

    public void Start()
    {
        if (!Initialize())
        {
            Status = ServerStatus.Unknown;
            return;
        }

        Status = ServerStatus.Online;
        SessionId = Guid.NewGuid();
        return;
    }

    public async void StartAsync()
    {
        await Task.Run(() => Start());
    }

    public void ShutDown(bool intentional)
    {
        if (intentional)
        {
            // todo: store/serialize data, clean-up resources, etc.
        }

        Status = ServerStatus.Offline;
    }

    private bool Initialize()
    {
        // ToDo: initialize all tasks here that are private to this server instance/session
        return true;
    }
}

public enum ServerStatus
{
    Online = 0,
    Offline,
    Unknown
}