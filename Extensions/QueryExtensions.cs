using Azure.Data.Tables;
using System.Linq.Expressions;
namespace Extensions.Query
{
    public static class QueryExtensions
    {
        /// <summary>
        /// Reemplazo de ExecuteQueryAllElementsAsync para obtener todos los registros con un filtro opcional.
        /// </summary>
        public static async Task<List<TElement>> ExecuteQueryAllElementsAsync<TElement>(
            this TableClient table,
            Expression<Func<TElement, bool>> filter = null)
            where TElement : class, ITableEntity, new()
        {
            var results = new List<TElement>();

            await foreach (var entity in table.QueryAsync(filter ?? (_ => true)))
            {
                results.Add(entity);
            }

            return results;
        }
        public static async Task<T?> QueryFirstOrDefaultAsync<T>(
            this TableClient client,
            Expression<Func<T, bool>> filter) where T : class, ITableEntity, new()
        {
            await foreach (var entity in client.QueryAsync(filter))
            {
                return entity;
            }
            return null;
        }

        /// <summary>
        /// Ejecuta QueryAsync (sin filtro) y devuelve todas las entidades en una lista.
        /// </summary>
        public static async Task<List<T>> QueryToListAsync<T>(
            this TableClient client) where T : class, ITableEntity, new()
        {
            var list = new List<T>();
            await foreach (var entity in client.QueryAsync<T>())
            {
                list.Add(entity);
            }
            return list;
        }

        /// <summary>
        /// Ejecuta QueryAsync con un filtro y devuelve todas las entidades en una lista.
        /// </summary>
        public static async Task<List<T>> QueryToListAsync<T>(
            this TableClient client,
            Expression<Func<T, bool>> filter) where T : class, ITableEntity, new()
        {
            var list = new List<T>();
            await foreach (var entity in client.QueryAsync(filter))
            {
                list.Add(entity);
            }
            return list;
        }

        public static async Task<List<T>> QueryToListAsync<T>(
            this TableClient table,
            string filter,
            int? maxPerPage = null,
            IEnumerable<string>? select = null,
            CancellationToken cancellationToken = default
        ) where T : class, ITableEntity, new()
        {
            var list = new List<T>();

            await foreach (var entity in table.QueryAsync<T>(
                filter: filter,
                maxPerPage: maxPerPage,
                select: select,
                cancellationToken: cancellationToken))
            {
                list.Add(entity);
            }

            return list;
        }

        /// <summary>
        /// Devuelve true si existe al menos 1 registro que cumpla el filter,
        /// sin traer toda la tabla (lee como máximo 1).
        /// </summary>
        public static async Task<bool> QueryAnyAsync<T>(
            this TableClient table,
            string filter,
            CancellationToken cancellationToken = default
        ) where T : class, ITableEntity, new()
        {
            await foreach (var _ in table.QueryAsync<T>(
                filter: filter,
                maxPerPage: 1,
                cancellationToken: cancellationToken))
            {
                return true; // Encontró al menos uno
            }
            return false;
        }
    }
}
