using System.Linq;
using Extensions.General;

namespace EntityGraphOperations.Internal
{
    internal static class Compare
    {
        /// <summary>
        /// Compare all type of objects.
        /// </summary>
        /// <remarks>This method was created for comapring properties of entity objects in runtime.</remarks>
        /// <param name="obj1">First object instance</param>
        /// <param name="obj2">Second object instance</param>
        /// <returns>True, if their value is same; otherwise, false</returns>
        internal static bool IsEqual(object obj1, object obj2)
        {
            if (obj1 == null && obj2 == null)
                return true;

            if (obj1 is string || obj2 is string)
            {
                return Equals(obj1 as string ?? "", obj2 as string ?? "");
            }

            if (obj1 == null && obj2 != null)
                return false;

            if (obj1 != null && obj2 == null)
                return false;

            if (obj1.GetType().IsCollection() && obj1.GetType().IsCollection())
            {
                return Enumerable.SequenceEqual(obj1 as dynamic, obj2 as dynamic);
            }

            return Equals(obj1, obj2);
        }
    }
}
