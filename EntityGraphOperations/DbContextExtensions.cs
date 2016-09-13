using EntityGraphOperations.Concrete;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Extensions.General;
using System.Data.Entity.Infrastructure;
using EntityGraphOperations.Internal;

namespace EntityGraphOperations
{
    public static class DbContextExtensions
    {
        /// <summary>
        /// Find mathcing entity in context based on primary or unique properties of the given entity instance
        /// </summary>
        /// <typeparam name="TSource">Entity type</typeparam>
        /// <param name="context">Context instance</param>
        /// <param name="entity">Entity instance</param>
        /// <param name="isLocal">If true, then query will send to the database; otherwise to .net object</param>
        /// <returns>Returns queryable after filtering the source</returns>
        public static IQueryable<TSource> FindMatchingEntity<TSource>(
            this DbContext context,
            TSource entity,
            bool isLocal = false)
            where TSource : class
        {
            if (context == null)
                throw new ArgumentNullException("context");

            if (entity == null)
                throw new ArgumentNullException("entity");

            var source = isLocal ?
                // Search local for duplicate entities
                context.Set<TSource>().Local.AsQueryable() :
                // Send query to the database
                context.Set<TSource>();

            var properties = context.GetPropertiesForFiltering(entity, new List<ExtendedPropertyInfo>());

            if (properties == null || !properties.Any())
                return null;

            return source.Where(properties);
        }

        /// <summary>
        /// Find all entities which their state must be defined after searching for them.
        /// Get these entities alongside with their principle and dependant entities.
        /// Then define state for all of them in ascending order by their direct and indirect principles.
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="context">DbContext instance</param>
        /// <param name="entity">Entity instance</param>
        public static ManualGraphOperation<TEntity> InsertOrUpdateGraph<TEntity>(
            this DbContext context,
            TEntity entity)
            where TEntity : class
        {
            if (entity == null)
                throw new ArgumentNullException("entity");

            if (context == null)
                throw new ArgumentNullException("context");

            context.Entry<TEntity>(entity).State = EntityState.Added;

            var mainEntityTree = new EntityTree { Entity = entity };
            var wholeEntityTree = new List<EntityTree>() { mainEntityTree };

            // Set all entities with their dependant and principle properties
            context.GetEntityTree(entity, null, null, wholeEntityTree);

            // Get all entities direct and indirect principles count
            var entitiesInDefineStateOrder = wholeEntityTree
                .Select(curr => new
                {
                    currentTree = curr,
                    IndirectPrincipleCount = curr.GetIndirectPrinciples(wholeEntityTree).Count()
                })
                .OrderBy(x => x.IndirectPrincipleCount);

            foreach (var item in entitiesInDefineStateOrder)
            {
                // Entity state will be detached if there is some duplicate entities
                if (context.Entry(item.currentTree.Entity).State == EntityState.Detached)
                    continue;

                // Define state of entity if it is not in detached state
                DefineState(
                    context,
                    item.currentTree.Entity as dynamic,
                    wholeEntityTree);
            }

            return new ManualGraphOperation<TEntity>(context, entity);
        }

        /// <summary>
        /// Create properties collection which will be need to define model matches
        /// </summary>
        /// <typeparam name="TSource">Entity type</typeparam>
        /// <param name="context">Context instance</param>
        /// <param name="model">Entity instance</param>
        /// <param name="properties">Information about finded properties will be sent to each function recursively</param>
        /// <returns>ExtendedPropertyInfo collection which holds property information and their values</returns>
        internal static IEnumerable<ExtendedPropertyInfo> GetPropertiesForFiltering<TSource>(
            this DbContext context,
            TSource model,
            IEnumerable<ExtendedPropertyInfo> properties)
            where TSource : class
        {
            if (model == null)
                throw new ArgumentNullException("model");

            if (context == null)
                throw new ArgumentNullException("context");

            // Get primary key properties of the type alongside with the value
            var primaryKeyProperties = context.GetPrimaryKeys<TSource>()
                .MakePropertiesExtended(model);

            // Get unique properterties of the type alongside with the value
            var uniqueKeyProperties = ExtendedEntityTypeConfiguration
                .GetInstance()
                .UniqueKeys
                .OfReflectingType<TSource>()
                .MakePropertiesExtended(model);

            // If all primary key properties has non-default value
            if (primaryKeyProperties.Any() && !primaryKeyProperties.Select(prop => prop.PropertyValue).AnyDefault())
            {
                properties = properties.Concat(primaryKeyProperties);
            }
            else if (uniqueKeyProperties.Any())
            {
                properties = properties
                    .Concat(uniqueKeyProperties.Where(prop => prop.PropertyInfo.PropertyType.IsBuiltInType()));

                // Find complex property
                var complexProperty = uniqueKeyProperties
                    .Where(prop => !prop.PropertyInfo.PropertyType.IsBuiltInType())
                    .FirstOrDefault(prop => !prop.PropertyValue.IsDefault());

                // Recursively get its properties
                if (complexProperty != null)
                {
                    properties = properties.Add(complexProperty)
                        .ToList();

                    properties = DbContextExtensions.GetPropertiesForFiltering(
                        context,
                        complexProperty.PropertyValue as dynamic,
                        properties);
                }
            }

            return properties;
        }

        /// <summary>
        /// Define state of all entities based on their conditions
        /// which will be define automatically with the help of primary and uniquq keys.
        /// Also, detach duplicate entities.
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="context">DbContext instance</param>
        /// <param name="entity">Entity instance</param>
        /// <param name="wholeEntityTree">Full entity tree instance</param>
        internal static void DefineState<TEntity>(
            this DbContext context,
            TEntity entity,
            List<EntityTree> wholeEntityTree)
            where TEntity : class
        {
            if (typeof(TEntity).FullName.Contains("District"))
            {
            }

            if (entity == null)
                throw new ArgumentNullException("entity");

            var currentEntry = context.Entry(entity);

            // Find mathcing entities in local
            var matchedEntitiesInContext = context.FindMatchingEntity(entity, true);

            // If there is any, then keep the last entity.
            // And detach other entities.
            // Change their references to the last one in their owners.
            if (matchedEntitiesInContext != null && matchedEntitiesInContext.Count() > 1)
            {
                matchedEntitiesInContext
                    .Except(entity)
                    .ToList()
                    .ForEach(matchedEntity =>
                        {
                            var matchedEntityTrees = wholeEntityTree
                                .Where(x => x.Entity.Equals(matchedEntity))
                                .ToList();

                            var matchedEntityTree = wholeEntityTree
                               .First(x => x.Entity.Equals(matchedEntity));

                            if (matchedEntityTree.Owner != null)
                            {
                                var property = matchedEntityTree.Owner.GetProperty(matchedEntity);

                                if (property != null)
                                {
                                    var matchedEntityPropertyState = context.Entry(matchedEntityTree.Owner)
                                        .Member(property.Name)
                                        .EntityEntry
                                        .State;

                                    context.Entry(matchedEntityTree.Owner).Member(property.Name).CurrentValue = entity;

                                    // Then detach it
                                    context.DetachEntityWithDependants(matchedEntity);

                                    // And then set new reference's state to unchanged for property fix-up.
                                    context.Entry(matchedEntityTree.Owner).Member(property.Name).EntityEntry.State = EntityState.Unchanged;
                                    context.Entry(matchedEntityTree.Owner).Member(property.Name).EntityEntry.State = EntityState.Added;
                                }
                                else
                                    // Then detach it
                                    context.DetachEntityWithDependants(matchedEntity);
                            }
                            else
                                context.DetachEntityWithDependants(matchedEntity);
                        });
            }

            // Find mathing entities in the database
            var matchedEntitiesInDatabase = context.FindMatchingEntity(entity, false);
            var primaryKeysOfEntity = context.GetPrimaryKeys<TEntity>()
                .MakePropertiesExtended(entity);

            if (matchedEntitiesInDatabase != null)
            {
                var matchedEntityInDatabase = matchedEntitiesInDatabase
                   .AsNoTracking()
                   .SingleOrDefault();

                if (matchedEntityInDatabase != null)
                {
                    // If it is already existed in the database, then set it's PKs
                    foreach (var primaryKey in primaryKeysOfEntity)
                        currentEntry.Property(primaryKey.PropertyInfo.Name).CurrentValue =
                            matchedEntityInDatabase.GetValue(primaryKey.PropertyInfo.Name);

                    // After this, set it's EntityState to Unchanged for allowing EF to fix properties.
                    currentEntry.State = EntityState.Unchanged;

                    // And then check, if it has different values than original entity in the database.
                    currentEntry.CompareWith(matchedEntityInDatabase);
                }
            }

            // If that entity has PK already with non-default values,
            // then change it's state to Unchanged for property fix-up
            // and then back to Added.
            if (currentEntry.State == EntityState.Added && !primaryKeysOfEntity.Select(prop => prop.PropertyValue).AnyDefault())
            {
                currentEntry.State = EntityState.Unchanged;
                currentEntry.State = EntityState.Added;
            }
        }

        /// <summary>
        /// Will compare current entity with original one, and set modified properties as Modified
        /// and will consider InsertOnly and UpdateOnly properties also.
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="currentEntry">Entry of the current entity</param>
        /// <param name="matchedEntityInDatabase">Original values of this entity in the database which will be compared with current entity</param>
        internal static void CompareWith<TEntity>(
           this DbEntityEntry<TEntity> currentEntry,
           TEntity matchedEntityInDatabase)
           where TEntity : class
        {
            // But firstly, get collection of properties which will never be updated.
            var notModifiableProperties = ExtendedEntityTypeConfiguration
                .GetInstance()
                .NotCompareProperties
                .OfReflectingType<TEntity>()
                .Select(prop => prop.Name);

            // Collection of properties which must be compared.
            var comparableProperties = currentEntry.CurrentValues
                .PropertyNames
                .Except(notModifiableProperties);

            foreach (var propertyName in comparableProperties)
            {
                var original = matchedEntityInDatabase.GetValue(propertyName);
                var current = currentEntry.Entity.GetValue(propertyName);

                if (!Compare.IsEqual(original, current))
                {
                    currentEntry.Property(propertyName).IsModified = true;
                }
            }
        }

        /// <summary>
        /// Detach entity alongside with all it's dependants recursively.
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="context">DbContext instance</param>
        /// <param name="entity">Entity instance</param>
        internal static void DetachEntityWithDependants<TEntity>(
            this DbContext context,
            TEntity entity)
            where TEntity : class
        {
            var dependants = context.GetRelatedEntities<TEntity>()
                .Where(prop => prop.EntityRelationDirection == EntityRelationDirection.Dependant);

            foreach (var dependant in dependants)
            {
                var dependantValue = entity.GetValue(dependant.PropertyInfo);

                // If property is collection, then execute process for each elements.
                if (dependant.PropertyInfo.PropertyType.IsGenericCollection())
                {
                    var collection = dependantValue as IEnumerable<object>;

                    if (collection == null)
                        continue;

                    foreach (var collectionElement in collection.ToList())
                    {
                        if (collectionElement == null)
                            continue;

                        DetachEntityWithDependants(
                            context,
                            collectionElement as dynamic);
                    }
                }
                else
                {
                    if (dependantValue == null)
                        continue;

                    DetachEntityWithDependants(context,
                                dependantValue as dynamic);
                }
            }

            context.Entry(entity).State = EntityState.Detached;
        }
    }
}
