using Cassandra.Mapping;
using ms.users.domain.Entities;

namespace ms.users.infraestructure.Mappings
{
    public class CassandraUserMapping : Cassandra.Mapping.Mappings
    {
        public CassandraUserMapping()
        {
            For<User>().TableName("User").PartitionKey(u => u.UserName)
                .Column(u => u.UserName, cc => cc.WithName("user_username"))
                .Column(u => u.Password, cc => cc.WithName("user_pasword"))
                .Column(u => u.Role, cc => cc.WithName("user_role"));
            
            // Se asigna configuración global de mapeado
            MappingConfiguration.Global.Define(this);
        }
    }
}
