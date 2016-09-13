using System.Reflection;

namespace EntityGraphOperations.Internal
{
    /// <summary>
    /// Helper model which holds navigation property information 
    /// and its role in relationship
    /// </summary>
    internal class EntityRelatedProperties
    {
        internal PropertyInfo PropertyInfo { get; set; }
        internal EntityRelationDirection EntityRelationDirection { get; set; }
    }
}
