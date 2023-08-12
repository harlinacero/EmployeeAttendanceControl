﻿using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace ms.employees.infraestucture.Data
{
    public class EmployeesDapperContext : IDapperContext
    {
        private IDbConnection _connection;
        private IDbTransaction _transaction;
        private readonly string _connectionString;

        public EmployeesDapperContext(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("EmployeeDB");
        }

        public IDbConnection Connection
        {
            get
            {
                _connection ??= new SqlConnection(_connectionString);
                if (IsConnectionClosed()) _connection.Open();
                return _connection;
            }
        }

        public bool IsConnectionClosed() => _connection != null && _connection.State.Equals(ConnectionState.Closed);

        public bool HasOpenConnection() => !IsConnectionClosed();
        public bool HasOpenTransaction() => _transaction != null && _transaction.Connection != null;

        public IDbTransaction BeginTransaction()
        {
            if (!HasOpenTransaction()) _transaction = Connection.BeginTransaction();
            return _transaction;
        }

        public void ClearTransaction()
        {
            if (_transaction != null)
            {
                _transaction.Dispose();
                _transaction = null;
            }
        }

        public void Commit()
        {
            try
            {
                _transaction.Commit();
                ClearTransaction();
            }
            catch
            {
                if (HasOpenConnection())
                {
                    Rollback();
                }
                throw;
            }
        }

        public T Transaction<T>(Func<IDbTransaction, T> query)
        {
            using var connection = Connection;
            using var transaction = BeginTransaction();
            try
            {
                return ExecuteQueryTransaction(query, transaction);
            }
            catch
            {
                Rollback();
                throw;
            }
        }

        private T ExecuteQueryTransaction<T>(Func<IDbTransaction, T> query, IDbTransaction transaction)
        {
            var transactionResult = query(transaction);
            Commit();
            return transactionResult;
        }
        public void Rollback()
        {
            try
            {
                _transaction.Rollback();
                ClearTransaction();
            }
            catch
            {
                throw;
            }
        }


        public void Dispose()
        {
            if(!HasOpenConnection())
            {
                _connection.Close();
                _connection = null;
            }
        }


    }
}
