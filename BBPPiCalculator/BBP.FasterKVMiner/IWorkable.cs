namespace BBP.FasterKVMiner
{
    public interface IWorkable
    {
        async Task<WorkBlock> Work()
        {
            throw new NotImplementedException();
        }
    }
}
