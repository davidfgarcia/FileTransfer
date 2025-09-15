

using System.Dynamic;
using System.Timers;
using FileTransferAssignment;

public class Transfer
{
    public Transferable Package { get; private set; }
    public Server Destination { get; private set; }

    public Server Source { get; private set; }
    public TransferStatus Status { get; set; }
    public int Retries{ get; set; }

    private System.Timers.Timer _timer;

    public Transfer(Transferable item, Server source, Server destination)
    {
        Package = item;
        Source = source;
        Destination = destination;
        // configure timer to timeout the transfer
        _timer = new System.Timers.Timer(Configuration.TIMEOUT_MILLISECONDS);
        _timer.Elapsed += OnTimeElapsed;
        _timer.Start();

        SimulateTransferStatus();
    }

    public void ResetStatus()
    {
        SimulateTransferStatus();
    }

    public void ResetTimer()
    {
        // allows to reset the timer when the transfer is retried
        _timer = new System.Timers.Timer(Configuration.TIMEOUT_MILLISECONDS);
        _timer.Elapsed += OnTimeElapsed;
        _timer.Start();
    }

    private void SimulateTransferStatus()
    {
        Random rnd = new Random();
        double val = rnd.NextDouble();

        if (val < .9)
        {
            Status = TransferStatus.Ongoing;
            return;
        }

        if (val < .95)
        {
            Status = TransferStatus.Failed;
            return;
        }

        Status = TransferStatus.TimedOut;
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