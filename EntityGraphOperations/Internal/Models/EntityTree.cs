using System.Collections.Generic;
using System.Linq;

namespace EntityGraphOperations.Internal
{
    /// <summary>
    /// Main model of the logic. 
    /// Holds all dependant and principal objects and owner of the entity
    /// </summary>
    internal class EntityTree
    {
        internal object Entity { get; set; }
        internal HashSet<EntityTree> DirectPrincipals { get; set; }
        internal HashSet<EntityTree> DirectDependants { get; set; }
        internal object Owner { get; set; }

        #region Constructors
        internal EntityTree()
        {
            DirectPrincipals = new HashSet<EntityTree>();
            DirectDependants = new HashSet<EntityTree>();
        }

        internal EntityTree(object entity)
            : this()
        {
            this.Entity = entity;
        }

        internal EntityTree(object entity, object owner)
            : this(entity)
        {
            this.Owner = owner;
        }
        #endregion

        /// <summary>
        /// Get all principles recursively
        /// </summary>
        /// <param name="wholeTree">All entity trees list</param>
        /// <returns>Collection of direct and indirect Principal entity's EntityTree objects</returns>
        internal IEnumerable<EntityTree> GetIndirectPrinciples(List<EntityTree> wholeTree)
        {
            foreach (var item in this.DirectPrincipals.Select(x => x))
            {
                yield return item;
            }

            foreach (var item in this.DirectPrincipals.SelectMany(x => x.GetIndirectPrinciples(wholeTree)))
            {
                yield return item;
            }
        }
    }
}
