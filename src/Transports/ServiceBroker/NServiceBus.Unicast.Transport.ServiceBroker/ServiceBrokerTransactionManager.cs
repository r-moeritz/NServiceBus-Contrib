using System;
using System.Data.SqlClient;

namespace NServiceBus.Unicast.Transport.ServiceBroker
{
    public class ServiceBrokerTransactionManager
    {
        private readonly string _connectionString;
        private SqlConnection _connection;
        private SqlTransaction _transaction;

        public ServiceBrokerTransactionManager(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void RunInTransaction(Action<SqlTransaction> callback)
        {
            var closeConnection = _connection == null;

            if (_connection == null)
            {
                _connection = new SqlConnection(_connectionString);
                _connection.Open();
            }

            // Verify we still have a valid connection since we may not have opened it above, cleanup if we've lost our connection
            if ((_connection.State & System.Data.ConnectionState.Open) == 0)
            {
                if (_transaction != null)
                {
                    _transaction.Rollback();
                    _transaction.Dispose();
                    _transaction = null;
                }

                _connection.Dispose();
                _connection = null;

                throw new ApplicationException("Connection to database failed, cleaning up...");
            }

            var disposeTransaction = _transaction == null;

            if (_transaction == null)
            {
                _transaction = _connection.BeginTransaction();
            }

            try
            {
                // The callback might rollback the transaction, we always commit it
                callback(_transaction);

                if (disposeTransaction)
                {
                    // We always commit our transactions, the callback might roll it back though
                    _transaction.Commit();
                }
            }
            catch
            {
                if (disposeTransaction)
                {
                    _transaction.Rollback();
                }
                throw;
            }
            finally
            {
                if (disposeTransaction)
                {
                    if (_transaction != null)
                    {
                        _transaction.Dispose();
                    }
                    _transaction = null;
                }

                if (closeConnection)
                {
                    if (_connection != null)
                    {
                        _connection.Close();
                        _connection.Dispose();
                    }
                    _connection = null;
                }
            }
        }
    }
}