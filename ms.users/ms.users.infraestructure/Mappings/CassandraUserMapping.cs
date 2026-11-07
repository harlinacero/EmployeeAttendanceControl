using Cassandra.Mapping;
using ms.users.domain.Entities;

namespace ms.users.infraestructure.Mappings
{
    public class CassandraUserMapping : Cassandra.Mapping.Mappings
    {
        public CassandraUserMapping()
        {
            // The column keeps its historic name; it now holds the protected form of the password
            For<User>().TableName("user").PartitionKey(u => u.UserName)
                .Column(u => u.UserName, cc => cc.WithName("user_username"))
                .Column(u => u.PasswordHash, cc => cc.WithName("user_pasword"))
                .Column(u => u.Role, cc => cc.WithName("user_role"));
        }
    }
}
