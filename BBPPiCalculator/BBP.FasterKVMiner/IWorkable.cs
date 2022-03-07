namespace BBP.FasterKVMiner;

public interface IWorkable
{
    WorkBlock Work(PiBuffer workingMemory)
    {
        throw new NotImplementedException();
    }
}
