using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Extensions.General;
using System.Data.Entity.ModelConfiguration;

namespace EntityGraphOperations.Concrete
{
    /// <summary>
    /// Extended entity type configurations which allowes to automaticallt define state of entity graph.
    /// </summary>
    public class ExtendedEntityTypeConfiguration<TEntityType> :
        EntityTypeConfiguration<TEntityType>
        where TEntityType : class
    {
        /// <summary>
        /// Configures the unique key property(s) for this entity type.
        /// </summary>
        /// <typeparam name="TKey"> The type of the key. </typeparam>
        /// <param name="keyExpression"> 
        /// A lambda expression representing the property to be used as the unique key.
        /// C#: t => t.Id VB.Net: Function(t) t.Id If the primary key is made up of multiple properties then specify an anonymous type including the properties.
        /// C#: t => new { t.Id1, t.Id2 } 
        /// VB.Net: Function(t) New With { t.Id1, t.Id2 } 
        /// </param>
        /// <returns>The same ModelTypeConfiguration instance so that multiple calls can be chained.</returns>
        public ExtendedEntityTypeConfiguration<TEntityType> HasUniqueKey<TKey>(Expression<Func<TEntityType, TKey>> keyExpression)
        {
            if (keyExpression == null)
                throw new ArgumentNullException("keyExpression");

            // Get singleton instance of the configuration
            var modelTypeConfiguration = ExtendedEntityTypeConfiguration.GetInstance();

            // If the return type is anoymous
            if (!keyExpression.ReturnType.IsAnonymousType())
            {
                // Then get property info from member expression
                modelTypeConfiguration.UniqueKey(keyExpression.GetPropertyInfo());
            }
            else
            {
                // Then get properties info from new expression
                modelTypeConfiguration.UniqueKey(keyExpression.GetPropertiesInfo());
            }

            return this;
        }

        /// <summary>
        /// Returns ExtendedPrimitiveTypeConfiguration instance, which will give opportunity 
        /// to set the property as not modifiable.
        /// </summary>
        /// <typeparam name="TKey">Property ytpe</typeparam>
        /// <param name="keyExpression">
        /// A lambda expression representing the property to be used as the unique key.
        /// C#: t => t.Id VB.Net: Function(t) t.Id If the primary key is made up of multiple properties then specify an anonymous type including the properties.
        /// C#: t => new { t.Id1, t.Id2 } 
        /// VB.Net: Function(t) New With { t.Id1, t.Id2 } 
        /// </param>
        /// <returns>The ExtendedPrimitiveTypeConfiguration instance so that multiple calls can be chained</returns>
        public ExtendedPrimitiveTypeConfiguration ExtendProperty<TKey>(Expression<Func<TEntityType, TKey>> keyExpression)
        {
            if (keyExpression == null)
                throw new ArgumentNullException("keyExpression");

            return new ExtendedPrimitiveTypeConfiguration(keyExpression.GetPropertyInfo());
        }
    }

    /// <summary>
    /// Allows configuration to be performed for a model type.
    /// </summary>
    public class ExtendedEntityTypeConfiguration
    {
        #region Private fields
        private static readonly Lazy<ExtendedEntityTypeConfiguration> instance =
            new Lazy<ExtendedEntityTypeConfiguration>(() => new ExtendedEntityTypeConfiguration());

        private readonly List<PropertyInfo> uniqueKeyProperties = new List<PropertyInfo>();
        private readonly List<PropertyInfo> notCompareProperties = new List<PropertyInfo>();
        #endregion

        #region Constructors
        // Constructor must be private for singleton pattern
        private ExtendedEntityTypeConfiguration() { }
        #endregion

        // Get static instance
        public static ExtendedEntityTypeConfiguration GetInstance()
        {
            return instance.Value;
        }

        // Add properties to the unique keys list
        public void UniqueKey(params PropertyInfo[] properties)
        {
            uniqueKeyProperties.AddRange(properties);
        }

        // Add properties to the list of properties which must not be modified
        public void NotCompare(PropertyInfo property)
        {
            notCompareProperties.Add(property);
        }

        // Get all unique properties
        internal IEnumerable<PropertyInfo> UniqueKeys
        {
            get
            {
                return uniqueKeyProperties;
            }
        }

        // Get all properties which must not be modified
        internal IEnumerable<PropertyInfo> NotCompareProperties
        {
            get
            {
                return notCompareProperties;
            }
        }
    }
}
