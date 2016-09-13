using System.Collections.Generic;
using System.Reflection;

namespace EntityGraphOperations.Internal.Models
{
    public class ForeignKeyRelationship
    {
        public ForeignKeyRelationship(
            IEnumerable<PropertyInfo> principalProperties,
            IEnumerable<PropertyInfo> dependantProperties)
        {
            DependantProperties = dependantProperties;
            PrincipalProperties = principalProperties;
        }

        public IEnumerable<PropertyInfo> DependantProperties { get; set; }
        public IEnumerable<PropertyInfo> PrincipalProperties { get; set; }
    }
}
