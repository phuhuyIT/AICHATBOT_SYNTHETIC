using System.Reflection;
using WebApplication1.Models;

namespace WebApplication1.Service.MappingService
{
    public static class GenericMappingService
    {
        /// <summary>
        /// Maps properties from source to destination object with same property names
        /// </summary>
        public static void MapProperties<TSource, TDestination>(TSource source, TDestination destination, params string[] excludeProperties)
            where TSource : class
            where TDestination : class
        {
            if (source == null || destination == null) return;

            var sourceType = typeof(TSource);
            var destinationType = typeof(TDestination);
            var excludeSet = new HashSet<string>(excludeProperties ?? Array.Empty<string>());

            var sourceProperties = sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead)
                .ToList();

            var destinationProperties = destinationType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite)
                .ToDictionary(p => p.Name, p => p);

            foreach (var sourceProp in sourceProperties)
            {
                if (excludeSet.Contains(sourceProp.Name)) continue;

                if (destinationProperties.TryGetValue(sourceProp.Name, out var destProp))
                {
                    // Check if types are compatible
                    if (IsTypeCompatible(sourceProp.PropertyType, destProp.PropertyType))
                    {
                        var value = sourceProp.GetValue(source);
                        destProp.SetValue(destination, value);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a new instance of TDestination and maps properties from source
        /// </summary>
        public static TDestination MapTo<TSource, TDestination>(TSource source, params string[] excludeProperties)
            where TSource : class
            where TDestination : class, new()
        {
            if (source == null) return null!;

            var destination = new TDestination();
            MapProperties(source, destination, excludeProperties);
            return destination;
        }

        /// <summary>
        /// Maps properties from DTO to Entity for creation, automatically sets CreatedAt and CreatedBy
        /// </summary>
        public static TEntity MapToEntity<TDTO, TEntity>(TDTO dto, string? createdBy = null)
            where TDTO : class
            where TEntity : AuditableEntity, new()
        {
            if (dto == null) return null!;

            var entity = new TEntity();
            
            // Map common properties
            MapProperties(dto, entity, "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy");
            
            // Set audit properties for creation
            entity.CreatedAt = DateTime.UtcNow;
            entity.CreatedBy = createdBy ?? AuditableEntity.SYSTEM_USER_ID;
            
            return entity;
        }

        /// <summary>
        /// Updates entity from DTO, automatically sets UpdatedAt and UpdatedBy
        /// </summary>
        public static void UpdateEntityFromDTO<TDTO, TEntity>(TDTO dto, TEntity entity, string? updatedBy = null)
            where TDTO : class
            where TEntity : AuditableEntity
        {
            if (dto == null || entity == null) return;

            // Map common properties (exclude audit fields and ID fields)
            MapProperties(dto, entity, "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy", "ApiKeyId", "Id");
            
            // Set audit properties for update
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = updatedBy ?? AuditableEntity.SYSTEM_USER_ID;
        }

        /// <summary>
        /// Maps Entity to Response DTO
        /// </summary>
        public static TDTO MapToResponseDTO<TEntity, TDTO>(TEntity entity)
            where TEntity : class
            where TDTO : class, new()
        {
            if (entity == null) return null!;

            return MapTo<TEntity, TDTO>(entity);
        }

        /// <summary>
        /// Maps collection of entities to collection of DTOs
        /// </summary>
        public static List<TDTO> MapToList<TEntity, TDTO>(IEnumerable<TEntity>? entities)
            where TEntity : class
            where TDTO : class, new()
        {
            if (entities == null) return new List<TDTO>();
            
            return entities
                .Where(e => e != null)
                .Select(e => MapToResponseDTO<TEntity, TDTO>(e))
                .Where(d => d != null)
                .ToList();
        }

        /// <summary>
        /// Checks if source type can be assigned to destination type
        /// </summary>
        private static bool IsTypeCompatible(Type sourceType, Type destinationType)
        {
            // Handle nullable types
            var sourceUnderlyingType = Nullable.GetUnderlyingType(sourceType) ?? sourceType;
            var destUnderlyingType = Nullable.GetUnderlyingType(destinationType) ?? destinationType;

            // Check direct assignment
            if (destinationType.IsAssignableFrom(sourceType))
                return true;

            // Check underlying types for nullable compatibility
            if (destUnderlyingType.IsAssignableFrom(sourceUnderlyingType))
                return true;

            // Check for same underlying type
            if (sourceUnderlyingType == destUnderlyingType)
                return true;

            return false;
        }
    }
}
