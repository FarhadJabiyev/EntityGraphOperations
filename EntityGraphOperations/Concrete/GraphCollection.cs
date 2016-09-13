using EntityGraphOperations.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using Extensions.General;
using EntityGraphOperations.Internal;
using System.Data.Entity;
using System.Linq.Expressions;

namespace EntityGraphOperations.Concrete
{
    /// <summary>
    /// Represents a collection entity in an entity.
    /// </summary>
    /// <typeparam name="TEntity">Type of the entity which is owner of the collection entity</typeparam>
    /// <typeparam name="TChildEntity">The type of the elements of the collection entity.</typeparam>
    public class GraphCollection<TEntity, TChildEntity> : IGraphCollection
        where TEntity : class
        where TChildEntity : class
    {
        private DbContext context;
        private TEntity owner;
        private ICollection<TChildEntity> collection;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="expression">Lambda expression identifying the collection of the entity.</param>
        /// <param name="context">DbContext instance</param>
        /// <param name="owner">Owner entity.</param>
        public GraphCollection(
            Expression<Func<TEntity, ICollection<TChildEntity>>> expression,
            DbContext context,
            TEntity owner)
        {
            this.context = context;
            this.owner = owner;
            this.collection = expression.Compile()(owner);
        }

        /// <summary>
        /// Retrieve list of the owner's previous collection of TChildEntity type.
        /// Then, compares it with the current collection. Missing entities state will be set to Deleted.
        /// </summary>
        /// <returns>An IGraphCollection instance.</returns>
        public IGraphCollection DeleteMissingEntities()
        {
            var relationship = context.GetForeignKeyRelationship<TEntity, TChildEntity>();

            // Foreign key Properties in collection entities and their values from the owner entity
            var properties = relationship
                .DependantProperties
                .Select((x, index) => new ExtendedPropertyInfo()
                {
                    PropertyInfo = x,
                    PropertyValue = owner.GetValue(relationship.PrincipalProperties.ElementAt(index))
                });

            // Get previous collection elements from the database
            var missedEntities = context.Set<TChildEntity>()
                .Where(properties)
                .AsNoTracking()
                .ToList();

            // If current collection is null or empty, then remove all elements from the table 
            // which are belongs to the owner.
            if (collection == null || !collection.Any())
                missedEntities
                    .ForEach(x => context.Entry(x).State = EntityState.Deleted);
            else
            {
                // Get Primary key of child entities
                var primaryKeyOfCollectionElements = context.GetPrimaryKeys<TChildEntity>();

                // And internally search for all of them in current collection.
                // Set their state to deleted, if they are missing from the current collection.
                missedEntities.ForEach(item =>
                {
                    var exactElementPk = primaryKeyOfCollectionElements
                         .Select(x => new ExtendedPropertyInfo()
                         {
                             PropertyInfo = x,
                             PropertyValue = item.GetValue(x)
                         });

                    if (!collection.AsQueryable().Where(exactElementPk).Any())
                        context.Entry(item).State = EntityState.Deleted; 
                });
            }

            return this;
        }
    }
}
