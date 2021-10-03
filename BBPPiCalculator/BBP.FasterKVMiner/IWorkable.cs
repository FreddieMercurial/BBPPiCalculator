namespace BBP.FasterKVMiner
{
    public interface IWorkable
    {
        WorkBlock Work(PiByteBuffer workingMemory)
        {
            throw new NotImplementedException();
        }
    }
}
