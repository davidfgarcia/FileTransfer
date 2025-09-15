using System.Dynamic;
using System.Runtime.CompilerServices;

public class Transferable // : ITransferable, etc.
{
    public String Id { get; private set; }
    public int Partition { get; private set; }
    public int Partitions { get; private set; }

    public bool Propagate { get; private set; }
    public String Contents { get; private set; }

    public TransferableMetadata Metadata { get; private set; }

    public Transferable(string id, int partition, int partitions, string contents, bool propagate)
    {
        Metadata = new TransferableMetadata(id, partition, partitions, propagate);
        Id = id;
        Partition = partition;
        Partitions = partitions;
        Contents = contents;
        Propagate = propagate;
    }
}