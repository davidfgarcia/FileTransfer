

using System.Dynamic;
using System.Timers;
using FileTransferAssignment;

public class Transfer
{
    public Transferable Package { get; private set; }
    public Server Destination { get; private set; }
    public TransferStatus Status { get; private set; }
    public int Retries{ get; set; }

    private System.Timers.Timer _timer;

    public Transfer(Transferable item, Server destination)
    {
        Package = item;
        Destination = destination;
        // configure timer to timeout the transfer
        _timer = new System.Timers.Timer(Configuration.TIMEOUT_MILLISECONDS);
        _timer.Elapsed += OnTimeElapsed;
        _timer.Start();
    }

    public void ResetTimer()
    {
        // allows to reset the timer when the transfer is retried
        _timer = new System.Timers.Timer(Configuration.TIMEOUT_MILLISECONDS);
        _timer.Elapsed += OnTimeElapsed;
        _timer.Start();
    }

    private void OnTimeElapsed(object? sender, ElapsedEventArgs e)
    {
        Status = TransferStatus.TimedOut;
        _timer.Stop();
    }
}

public enum TransferStatus
{
    Ongoing = 0,
    Completed,
    TimedOut,
    Failed
}