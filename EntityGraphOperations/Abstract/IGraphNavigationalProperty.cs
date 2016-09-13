namespace EntityGraphOperations.Abstract
{
    /// <summary>
    /// Properties and methods used by the consumer to configure the non-collection 
    /// navigational properties of the entities.
    /// </summary>
    public interface IGraphNavigationProperty
    {
        IGraphNavigationProperty DeleteIfNull();
    }
}
