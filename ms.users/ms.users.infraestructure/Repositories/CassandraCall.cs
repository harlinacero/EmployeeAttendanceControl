using Cassandra;
using Cassandra.Mapping;
using ms.users.domain.Exceptions;
using ms.users.infraestructure.Data;

namespace ms.users.infraestructure.Repositories
{
    /// <summary>
    /// Runs a query and turns any driver failure, including the connection itself, into DataUnavailableException.
    /// </summary>
    internal static class CassandraCall
    {
        public static async Task<T> Run<T>(Func<IMapper, Task<T>> query, IUsersContext usersContext)
        {
            try
            {
                return await query(usersContext.GetMapper());
            }
            catch (DriverException exception)
            {
                throw new DataUnavailableException(exception);
            }
        }

        public static Task Run(Func<IMapper, Task> command, IUsersContext usersContext) => Run(async mapper =>
        {
            await command(mapper);
            return true;
        }, usersContext);
    }
}
