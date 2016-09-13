using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Extensions.General;
using System.Data.Entity.Infrastructure;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Core.Metadata.Edm;
using EntityGraphOperations.Internal.Models;
using System.Data;

namespace EntityGraphOperations.Internal
{
    internal static class HelperExtensions
    {
        /// <summary>
        ///  Filters a sequence of values based on specified properties of a specified model.
        /// </summary>
        /// <typeparam name="TSource">Model type</typeparam>
        /// <param name="source">An System.Linq.IQueryable to filter</param>
        /// <param name="model">Model instance</param>
        /// <param name="properties">Specified properties of a model</param>
        /// <returns>  
        /// An System.Linq.IQueryable that contains elements from the input sequence
        /// that satisfy the condition specified by generated predicate.
        /// </returns>
        internal static IQueryable<TSource> Where<TSource>(this IQueryable<TSource> source,
            IEnumerable<ExtendedPropertyInfo> properties)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            if (properties == null)
                throw new ArgumentNullException("properties");

            if (!properties.Any())
                throw new InvalidOperationException("There is not any properties for building expression.");

            var parameter = Expression.Parameter(typeof(TSource), "model");
            var propertyOwnerParameter = parameter as Expression;
            Expression finalExpression = null;

            foreach (var prop in properties)
            {
                if (!prop.PropertyInfo.PropertyType.IsBuiltInType())
                {
                    propertyOwnerParameter = Expression.Property(propertyOwnerParameter, prop.PropertyInfo);
                    continue;
                }

                if (propertyOwnerParameter is MemberExpression)
                {
                    if (finalExpression == null)
                        finalExpression = Expression.NotEqual(propertyOwnerParameter,
                            Expression.Constant(null));
                    else
                        finalExpression = Expression.AndAlso(finalExpression,
                            Expression.NotEqual(propertyOwnerParameter, Expression.Constant(null)));
                }

                var leftExpression = Expression.Property(propertyOwnerParameter, prop.PropertyInfo.Name);
                var rightExpression = Expression.Constant(prop.PropertyValue);

                // In case of nullable type, it will help us
                var converted = Expression.Convert(rightExpression, leftExpression.Type);

                // model => model.Property == 45 (f.e)
                var comparison = Expression.Equal(leftExpression, converted);

                if (finalExpression == null)
                    finalExpression = comparison;
                else
                    finalExpression = Expression.AndAlso(finalExpression, comparison);
            }


            var resultExpression = Expression.Lambda<Func<TSource, bool>>(finalExpression, parameter);
            return source.Where(resultExpression);
        }

        /// <summary>
        /// Get foreign key properties between two types.
        /// </summary>
        /// <typeparam name="TPrincipalEntity">Principal end of the association</typeparam>
        /// <typeparam name="TDependantEntity">Dependant end of the association</typeparam>
        /// <param name="context">DbContext instance</param>
        /// <returns>Properties of the TDependantEntity type, which associates it with the TPrincipalEntity type.</returns>
        internal static ForeignKeyRelationship GetForeignKeyRelationship<TPrincipalEntity, TDependantEntity>(this DbContext context)
            where TPrincipalEntity : class
            where TDependantEntity : class
        {
            ObjectContext objectContext = ((IObjectContextAdapter)context).ObjectContext;
            ObjectSet<TDependantEntity> set = objectContext.CreateObjectSet<TDependantEntity>();

            foreach (NavigationProperty entityMember in set.EntitySet.ElementType.NavigationProperties)
            {
                if (entityMember.TypeUsage.EdmType.Name == typeof(TPrincipalEntity).Name)
                {
                    var dependantProperties = entityMember
                            .GetDependentProperties()
                            .Select(x => typeof(TDependantEntity).GetProperty(x.Name));

                    var principalProperties = context.GetPrimaryKeys<TPrincipalEntity>();

                    return new ForeignKeyRelationship(principalProperties, dependantProperties);
                }
            }

            return null;
        }

        /// <summary>
        /// Get primary key properties for the specified entity type
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="context">Context instance</param>
        /// <returns>PropertInfo collection of primary key properties</returns>
        internal static IEnumerable<PropertyInfo> GetPrimaryKeys<TEntity>(this DbContext context)
            where TEntity : class
        {
            ObjectContext objectContext = ((IObjectContextAdapter)context).ObjectContext;
            ObjectSet<TEntity> set = objectContext.CreateObjectSet<TEntity>();

            return set.EntitySet
                .ElementType
                .KeyMembers
                .Select(key => typeof(TEntity).GetProperty(key.Name));
        }

        /// <summary>
        /// Get navigational properties for the specified entity type
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="context">Context instance</param>
        /// <returns>PropertInfo collection of navigational properties</returns>
        internal static IEnumerable<PropertyInfo> GetNavigationalProperties<TEntity>(this DbContext context)
            where TEntity : class
        {
            ObjectContext objectContext = ((IObjectContextAdapter)context).ObjectContext;
            ObjectSet<TEntity> set = objectContext.CreateObjectSet<TEntity>();

            foreach (var navigationalProperty in set.EntitySet.ElementType.NavigationProperties)
            {
                yield return typeof(TEntity)
                    .GetProperty(navigationalProperty.Name);
            }
        }

        /// <summary>
        /// Get navigation properties which is dependant or principal of the specified entity
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="context">Context instance</param>
        /// <returns>Collection of EntityRelatedProperties which is dependant or principal of the specified entity</returns>
        internal static IEnumerable<EntityRelatedProperties> GetRelatedEntities<TEntity>(this DbContext context)
            where TEntity : class
        {
            ObjectContext objectContext = ((IObjectContextAdapter)context).ObjectContext;

            // All navigation properties types
            var toManyDependantTypeNames = context.GetNavigationalProperties<TEntity>()
                .Select(x => x.PropertyType.Name);

            // Dependant types
            var dependantTypeNames = objectContext.MetadataWorkspace
                .GetItems<AssociationType>(DataSpace.SSpace)
                .Where(x => x.ReferentialConstraints[0].FromRole.Name == typeof(TEntity).Name)
                .Select(x => x.ReferentialConstraints[0].ToRole.Name);

            // Principal types
            var principalTypeNames = objectContext.MetadataWorkspace
                .GetItems<AssociationType>(DataSpace.SSpace)
                .Where(x => x.ReferentialConstraints[0].ToRole.Name == typeof(TEntity).Name)
                .Select(x => x.ReferentialConstraints[0].FromRole.Name);

            var dependantTypeNamesIncludedCloudTables = dependantTypeNames
                .Union(toManyDependantTypeNames)
                .Except(principalTypeNames);

            var dependantProperties = typeof(TEntity)
                .GetPropertiesWithTypeNames(dependantTypeNamesIncludedCloudTables)
                .Select(prop => new EntityRelatedProperties
                {
                    EntityRelationDirection = EntityRelationDirection.Dependant,
                    PropertyInfo = prop
                });

            var principalProperties = typeof(TEntity)
                 .GetPropertiesWithTypeNames(principalTypeNames)
                 .Select(prop => new EntityRelatedProperties
                 {
                     EntityRelationDirection = EntityRelationDirection.Principal,
                     PropertyInfo = prop
                 });

            return dependantProperties
                .Concat(principalProperties);
        }

        /// <summary>
        /// Returns new ExtendedPropertyInfo collection based on object instance and it's properties
        /// </summary>
        /// <typeparam name="TSource">Type of the model which owns properties</typeparam>
        /// <param name="properties">Properties collection</param>
        /// <param name="model">Model instance</param>
        /// <returns>new ExtendedPropertyInfo collection based on object instance and it's properties</returns>
        internal static IEnumerable<ExtendedPropertyInfo> MakePropertiesExtended<TSource>(this IEnumerable<PropertyInfo> properties,
            TSource model)
            where TSource : class
        {
            if (model == null)
                throw new ArgumentNullException("model");

            if (properties == null)
                throw new ArgumentNullException("properties");

            foreach (var property in properties)
            {
                yield return new ExtendedPropertyInfo
                {
                    PropertyInfo = property,
                    PropertyValue = model.GetValue(property)
                };
            }
        }

        /// <summary>
        /// Construct entity tree list for the specified entity
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="context">Context instance</param>
        /// <param name="entity">Entity instance</param>
        /// <param name="grandOwner">Entity's grand owner instance</param>
        /// <param name="owner">Entity's owner instance</param>
        /// <param name="lastRecursionReturn">Will help us to get all entity trees recursively</param>
        /// <returns>Holds all dependant and principal objects and owner of the entity and it's all child entities recursively</returns>
        internal static List<EntityTree> GetEntityTree<TEntity>(this DbContext context,
            TEntity entity,
            object grandOwner,
            object owner,
            List<EntityTree> lastRecursionReturn)
            where TEntity : class
        {
            // Iterate over dependant and principal navigational properties
            foreach (var relatedProperty in context.GetRelatedEntities<TEntity>())
            {
                var relatedPropertyValue = entity.GetValue(relatedProperty.PropertyInfo);

                if (relatedPropertyValue == null)
                    continue;

                // Use loop to check each element of the collection
                if (relatedProperty.PropertyInfo.PropertyType.IsGenericCollection())
                {
                    (relatedPropertyValue as IEnumerable<object>)
                        .ToList()
                        .ForEach(element =>
                        {
                            if (element == null)
                                return;

                            var isResursionNeeded = CheckAndInsertIntoTreeList(element,
                                entity,
                                owner,
                                relatedProperty.EntityRelationDirection,
                                lastRecursionReturn);

                            if (isResursionNeeded)
                                GetEntityTree(context, element as dynamic, owner, entity, lastRecursionReturn);
                        });
                }
                else
                {
                    var isResursionNeeded = CheckAndInsertIntoTreeList(relatedPropertyValue,
                                entity,
                                owner,
                                relatedProperty.EntityRelationDirection,
                                lastRecursionReturn);

                    if (isResursionNeeded)
                        GetEntityTree(context, relatedPropertyValue as dynamic, owner, entity, lastRecursionReturn);
                }

            }

            return lastRecursionReturn;
        }

        /// <summary>
        /// Check and insert new EntityTree to the list.
        /// Set it's dependant and principal entities.
        /// </summary>
        /// <param name="firstEntity">First side entity instance</param>
        /// <param name="secondEntity">Second side entity instance</param>
        /// <param name="secondEntityOwner">Second side entity's owner instance</param>
        /// <param name="entityRelationDirection">How does first entity depends on second entity? Eitehr Principal or Dependant</param>
        /// <param name="lastRecursionReturn">Holds all dependant and principal objects and owner of the entity and it's all child entities recursively</param>
        /// <returns>
        /// If the first entity was not in the list, true; otherwise, false.
        /// The first entities properties will be checked in case of True.
        /// </returns>
        private static bool CheckAndInsertIntoTreeList(
            object firstEntity,
            object secondEntity,
            object secondEntityOwner,
            EntityRelationDirection entityRelationDirection,
            List<EntityTree> lastRecursionReturn)
        {
            var firstEntityTree = lastRecursionReturn.FirstOrDefault(x => x.Entity.Equals(firstEntity));
            var secondEntityTree = lastRecursionReturn.FirstOrDefault(x => x.Entity.Equals(secondEntity));
            var isResursionNeeded = firstEntityTree == null;

            if (firstEntityTree == null)
            {
                firstEntityTree = new EntityTree(firstEntity, secondEntity);
                lastRecursionReturn.Add(firstEntityTree);
            }

            if (secondEntityTree == null)
            {
                secondEntityTree = new EntityTree(secondEntity, secondEntityOwner);
                lastRecursionReturn.Add(secondEntityTree);
            }

            if (entityRelationDirection == EntityRelationDirection.Dependant)
            {
                firstEntityTree.DirectPrincipals.Add(secondEntityTree);
                secondEntityTree.DirectDependants.Add(firstEntityTree);
            }
            else
            {
                firstEntityTree.DirectDependants.Add(secondEntityTree);
                secondEntityTree.DirectPrincipals.Add(firstEntityTree);
            }

            return isResursionNeeded;
        }
    }
}
