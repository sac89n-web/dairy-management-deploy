using Dapper;
using System.Threading.Tasks;

namespace Dairy.Infrastructure
{
    public interface IAuditLogRepository
    {
        Task<int> LogAsync(int userId, string action, string entity, int entityId, string details);
    }

    public class AuditLogRepository : BaseRepository, IAuditLogRepository
    {
        public AuditLogRepository(SqlConnectionFactory factory) : base(factory) { }

        public async Task<int> LogAsync(int userId, string action, string entity, int entityId, string details)
        {
            var sql = @"INSERT INTO dairy.audit_log (user_id, action, entity, entity_id, details) VALUES (@userId, @action, @entity, @entityId, @details)";
            return await ExecuteAsync(sql, new { userId, action, entity, entityId, details });
        }
    }
}
