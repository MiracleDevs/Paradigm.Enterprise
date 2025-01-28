using Paradigm.Enterprise.Data.StoredProcedures.Mappers;
using System.Collections;
using System.Data.Common;

namespace Microsoft.DemoManagementSystem.Data.Core.Extensions
{
    public static class DbDataReaderExtensions
    {
        #region Public Methods

        /// <summary>
        /// Translates the reader to a list of the specified type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader">The reader.</param>
        /// <returns></returns>
        public static async Task<T?> TranslateAsync<T>(this DbDataReader reader)
        {
            var resultType = typeof(T);

            if (resultType.IsPrimitive)
                return await TranslatePrimitiveAsync<T>(reader);

            if (typeof(IList).IsAssignableFrom(resultType))
                return await TranslateListAsync<T>(reader, resultType);

            return await TranslateSingleAsync<T>(reader, resultType);
        }

        /// <summary>
        /// Translates the and move.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader">The reader.</param>
        /// <returns></returns>
        public static async Task<T?> TranslateAndMoveAsync<T>(this DbDataReader reader)
        {
            var result = await TranslateAsync<T>(reader);
            await reader.NextResultAsync();
            return result;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Translates a single entity.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader">The reader.</param>
        /// <param name="resultType">Type of the result.</param>
        /// <returns></returns>
        private static async Task<T?> TranslateSingleAsync<T>(DbDataReader reader, Type resultType)
        {
            var objectMapper = DataReaderMapperFactory.GetMapper<T>();
            if (await reader.ReadAsync())
                return (T)objectMapper.Map(reader);

            return default;
        }

        /// <summary>
        /// Translates a list of entities.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader">The reader.</param>
        /// <param name="resultType">Type of the result.</param>
        /// <returns></returns>
        /// <exception cref="Exception">Couldn't instantiate the type {resultType.Name}</exception>
        private static async Task<T> TranslateListAsync<T>(DbDataReader reader, Type resultType)
        {
            var listItemType = resultType.GetGenericArguments().First();
            var objectMapper = DataReaderMapperFactory.GetMapper(listItemType);

            if (Activator.CreateInstance(resultType) is not IList results)
                throw new Exception($"Couldn't instantiate the type {resultType.Name}");

            while (await reader.ReadAsync())
            {
                results.Add(objectMapper.Map(reader));
            }

            return (T)results;
        }

        /// <summary>
        /// Translates a primitive.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader">The reader.</param>
        private static async Task<T?> TranslatePrimitiveAsync<T>(DbDataReader reader)
        {
            if (await reader.ReadAsync())
                return (T)reader.GetValue(0);

            return default;
        }

        #endregion
    }
}