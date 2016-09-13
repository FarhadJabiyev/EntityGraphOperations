using EntityGraphOperations.Abstract;
using System;
using System.Linq;
using Extensions.General;
using EntityGraphOperations.Internal;
using System.Data.Entity;
using System.Linq.Expressions;

namespace EntityGraphOperations.Concrete
{
    /// <summary>
    /// Represents a navigational property in an entity.
    /// </summary>
    /// <typeparam name="TEntity">Type of the entity which is owner of the navigational property.</typeparam>
    /// <typeparam name="TChildEntity">The type of the navigational property.</typeparam>
    public class GraphNavigationalProperty<TEntity, TChildEntity> : IGraphNavigationProperty
        where TEntity : class
        where TChildEntity : class
    {
        private DbContext context;
        private TEntity owner;
        private TChildEntity navigationalProperty;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="expression">Lambda expression identifying the navigational property of the entity.</param>
        /// <param name="context">DbContext instance</param>
        /// <param name="owner">Owner entity.</param>
        public GraphNavigationalProperty(
            Expression<Func<TEntity, TChildEntity>> expression,
            DbContext context,
            TEntity owner)
        {
            this.context = context;
            this.owner = owner;
            this.navigationalProperty = expression.Compile()(owner);
        }

        /// <summary>
        /// Delete entity if was existed for the owner entity and has null value now.
        /// </summary>
        /// <returns>An IGraphNavigationProperty instance</returns>
        public IGraphNavigationProperty DeleteIfNull()
        {
            var relationship = context.GetForeignKeyRelationship<TEntity, TChildEntity>();

            // Foreign key Properties in navigational property and it's value from the owner entity
            var properties = relationship
                .DependantProperties
                .Select((x, index) => new ExtendedPropertyInfo()
                {
                    PropertyInfo = x,
                    PropertyValue = owner.GetValue(relationship.PrincipalProperties.ElementAt(index))
                });

            // Get previous element from the database
            var previousEntity = context.Set<TChildEntity>()
                .Where(properties)
                .AsNoTracking()
                .SingleOrDefault();

            // If current collection is null or empty, then remove all elements from the table 
            // which are belongs to the owner.
            if (navigationalProperty == null &&
                previousEntity != null)
                context.Entry(previousEntity).State = EntityState.Deleted;

            return this;
        }
    }
}
