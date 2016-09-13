using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using EntityGraphOperations.Abstract;

namespace EntityGraphOperations.Concrete
{
    /// <summary>
    /// Creates instances of the GraphCollections.
    /// </summary>
    /// <typeparam name="TEntity">Type of the entity which is owner of the collection entity.</typeparam>
    public class ManualOperationBuilder<TEntity>
        where TEntity : class
    {
        private ManualGraphOperation<TEntity> graphOperation;

        /// <summary>
        /// Constrcutor.
        /// </summary>
        /// <param name="graphOperation">Instance of the ManualGraphOperation.</param>
        public ManualOperationBuilder(ManualGraphOperation<TEntity> graphOperation)
        {
            this.graphOperation = graphOperation;
        }

        /// <summary>
        /// Creates instance of the IGraphCollection for this collection of the current owner entity.
        /// </summary>
        /// <typeparam name="TChildEntity">The type of the elements of the collection.</typeparam>
        /// <param name="expression">Lambda expression identifying the collection of the model</param>
        /// <returns>An instance of the IGraphCollection</returns>
        public IGraphCollection HasCollection<TChildEntity>(Expression<Func<TEntity, ICollection<TChildEntity>>> expression)
            where TChildEntity : class
        {
            return graphOperation.RegisterCollection(expression);
        }

        /// <summary>
        /// Creates instance of the IGraphNavigationProperty for this property of the current owner entity.
        /// </summary>
        /// <typeparam name="TChildEntity">The type of the navigational property.</typeparam>
        /// <param name="expression">Lambda expression identifying the navigational property of the model.</param>
        /// <returns>An instance of the IGraphNavigationProperty</returns>
        public IGraphNavigationProperty HasNavigationalProperty<TChildEntity>(Expression<Func<TEntity, TChildEntity>> expression)
            where TChildEntity : class
        {
            return graphOperation.RegisterNavigationalProperty(expression);
        }
    }
}
