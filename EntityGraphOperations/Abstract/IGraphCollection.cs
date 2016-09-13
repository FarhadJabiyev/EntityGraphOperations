namespace EntityGraphOperations.Abstract
{
    /// <summary>
    /// Properties and methods used by the consumer to configure the Collection properties of the entities.
    /// </summary>
    public interface IGraphCollection
    {
        IGraphCollection DeleteMissingEntities();
    }
}
