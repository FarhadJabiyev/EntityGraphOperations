using EntityGraphOperations.Abstract;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq.Expressions;

namespace EntityGraphOperations.Concrete
{
    /// <summary>
    /// Manually manage entities states after automatically detecting all possible entities states.
    /// </summary>
    /// <typeparam name="TEntity">Type of the entity which is owner of the collection entity.</typeparam>
    public class ManualGraphOperation<TEntity>
        : IManualGraphOperation<TEntity>
        where TEntity : class
    {
        private DbContext context { get; set; }
        private TEntity model { get; set; }
        
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">DbContext instance.</param>
        /// <param name="model">Type of the owner entity.</param>
        public ManualGraphOperation(DbContext context, TEntity model)
        {
            this.context = context;
            this.model = model;
        }

        /// <summary>
        /// Creates an instance of the IManualGraphOperation to configure operation.
        /// </summary>
        /// <param name="manualOperationBuilderAction">Delegate to create an instance of ManualOperationBuilder.</param>
        /// <returns>An instance of IManualGraphOperation.</returns>
        public IManualGraphOperation<TEntity> After(Action<ManualOperationBuilder<TEntity>> manualOperationBuilderAction)
        {
            ManualOperationBuilder<TEntity> manualOperationBuilder = new ManualOperationBuilder<TEntity>(this);
            manualOperationBuilderAction(manualOperationBuilder);
            return this;
        }

        /// <summary>
        /// Registers an lambda expression as collection member and 
        /// returns IGraphCollection for additional configurations over this collection.
        /// </summary>
        /// <typeparam name="TChildEntity">The type of the elements of the collection.</typeparam>
        /// <param name="expression">Lambda expression identifying the collection of the entity.</param>
        /// <returns>An instance of the IGraphCollection</returns>
        internal IGraphCollection RegisterCollection<TChildEntity>(Expression<Func<TEntity, ICollection<TChildEntity>>> expression)
            where TChildEntity : class
        {
            return new GraphCollection<TEntity, TChildEntity>(expression, context, model);
        }

        /// <summary>
        /// Registers an lambda expression as navigational property member and 
        /// returns IGraphNavigationProperty for additional configurations over this property.
        /// </summary>
        /// <typeparam name="TChildEntity">The type of the navigational property.</typeparam>
        /// <param name="expression">Lambda expression identifying the navigational property of the model.</param>
        /// <returns>An instance of the IGraphNavigationProperty</returns>
        internal IGraphNavigationProperty RegisterNavigationalProperty<TChildEntity>(Expression<Func<TEntity, TChildEntity>> expression)
            where TChildEntity : class
        {
            return new GraphNavigationalProperty<TEntity, TChildEntity>(expression, context, model);
        }
    }
}
