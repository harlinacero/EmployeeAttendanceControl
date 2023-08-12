using System.Data;

namespace ms.employees.infraestucture.Data
{
    public interface IDapperContext
    {
        IDbConnection Connection { get; }
        T Transaction<T>(Func<IDbTransaction, T> query);
        IDbTransaction BeginTransaction();
        void Commit();
        void Rollback();
        void Dispose();
    }
}
