using Dairy.Domain;
using Dapper;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dairy.Infrastructure
{
    public interface ICollectionRepository
    {
        Task<IEnumerable<MilkCollection>> ListAsync(int? farmerId = null, int? shiftId = null, string? date = null, int page = 1, int pageSize = 20);
        Task<MilkCollection?> GetByIdAsync(int id);
        Task<int> AddAsync(MilkCollection entity);
        Task<int> UpdateAsync(MilkCollection entity);
        Task<int> DeleteAsync(int id);
        Task<bool> ExistsAsync(int farmerId, int shiftId, string date);
    }

    public class CollectionRepository : BaseRepository, ICollectionRepository
    {
        public CollectionRepository(SqlConnectionFactory factory) : base(factory) { }

        public async Task<IEnumerable<MilkCollection>> ListAsync(int? farmerId = null, int? shiftId = null, string? date = null, int page = 1, int pageSize = 20)
        {
            var sql = @"SELECT * FROM dairy.milk_collection WHERE (@farmerId IS NULL OR farmer_id = @farmerId) AND (@shiftId IS NULL OR shift_id = @shiftId) AND (@date IS NULL OR date::text = @date) ORDER BY date DESC OFFSET @offset LIMIT @limit";
            return await QueryAsync<MilkCollection>(sql, new { farmerId, shiftId, date, offset = (page - 1) * pageSize, limit = pageSize });
        }

        public async Task<MilkCollection?> GetByIdAsync(int id)
        {
            var sql = "SELECT * FROM dairy.milk_collection WHERE id = @id";
            var result = await QueryAsync<MilkCollection>(sql, new { id });
            return result.FirstOrDefault();
        }

        public async Task<int> AddAsync(MilkCollection entity)
        {
            var sql = @"INSERT INTO dairy.milk_collection (farmer_id, shift_id, date, qty_ltr, fat_pct, price_per_ltr, due_amt, notes, created_by) VALUES (@FarmerId, @ShiftId, @Date::date, @QtyLtr, @FatPct, @PricePerLtr, @DueAmt, @Notes, @CreatedBy) RETURNING id";
            using var conn = _connectionFactory.CreateConnection();
            return await conn.ExecuteScalarAsync<int>(sql, entity);
        }

        public async Task<int> UpdateAsync(MilkCollection entity)
        {
            var sql = @"UPDATE dairy.milk_collection SET qty_ltr=@QtyLtr, fat_pct=@FatPct, price_per_ltr=@PricePerLtr, due_amt=@DueAmt, notes=@Notes WHERE id=@Id";
            return await ExecuteAsync(sql, entity);
        }

        public async Task<int> DeleteAsync(int id)
        {
            var sql = "DELETE FROM dairy.milk_collection WHERE id=@id";
            return await ExecuteAsync(sql, new { id });
        }

        public async Task<bool> ExistsAsync(int farmerId, int shiftId, string date)
        {
            var sql = "SELECT COUNT(1) FROM dairy.milk_collection WHERE farmer_id=@farmerId AND shift_id=@shiftId AND date::text=@date";
            using var conn = _connectionFactory.CreateConnection();
            var count = await conn.ExecuteScalarAsync<int>(sql, new { farmerId, shiftId, date });
            return count > 0;
        }
    }
}
