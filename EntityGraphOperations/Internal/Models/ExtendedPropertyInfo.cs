using System.Reflection;

namespace EntityGraphOperations.Internal
{
    /// <summary>
    /// Model which holds property value inside it's owner alongside iwth their PropertyInfo's
    /// </summary>
    internal class ExtendedPropertyInfo
    {
        internal PropertyInfo PropertyInfo { get; set; }
        internal object PropertyValue { get; set; }
    }
}
