using EntityGraphOperations.Concrete;
using System;

namespace EntityGraphOperations.Abstract
{
    /// <summary>
    /// Properties and methods used by the consumer to configure state of entities manually,
    /// after automatically detecting all possible entities states.
    /// </summary>
    public interface IManualGraphOperation<TModel>
        where TModel : class
    {
        IManualGraphOperation<TModel> After(Action<ManualOperationBuilder<TModel>> graphBuilder);
    }
}
