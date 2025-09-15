
public class TransferableMetadata
{
    public String Id { get; private set; }
    public int Partition { get; private set; }
    public int Partitions { get; private set; }

    public bool Propagate { get; private set; }

    public TransferableMetadata(string id, int partition, int partitions, bool propagate)
    {
        Id = id;
        Partition = partition;
        Partitions = partitions;
        Propagate = propagate;
    }
}