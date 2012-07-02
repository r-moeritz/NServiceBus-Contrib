using System.Data.SqlClient;

namespace NServiceBus.Unicast.Transport.ServiceBroker.Util
{
    internal static class ConnectionStringExtensions
    {
        public static void TestConnection(this string connectionString)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
            }
        }
    }
}
