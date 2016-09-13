using System.Reflection;

namespace EntityGraphOperations.Concrete
{
    /// <summary>
    ///  Used to configure a primitive property of an entity.
    /// </summary>
    public class ExtendedPrimitiveTypeConfiguration
    {
        private readonly PropertyInfo propertyInfo;

        public ExtendedPrimitiveTypeConfiguration(PropertyInfo propertyInfo)
        {
            this.propertyInfo = propertyInfo;
        }

        /// <summary>
        /// This property will be set only when inserting new entity and will never be set as modified.
        /// </summary>
        /// <returns>The same ExtendedPrimitiveTypeConfiguration instance so that multiple calls can be chained</returns>
        public ExtendedPrimitiveTypeConfiguration NotCompare()
        {
            ExtendedEntityTypeConfiguration.GetInstance()
                .NotCompare(propertyInfo);

            return this;
        }
    }
}
