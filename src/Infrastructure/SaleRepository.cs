using Dairy.Domain;
using Dapper;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dairy.Infrastructure
{
    public interface ISaleRepository
    {
        Task<IEnumerable<Sale>> ListAsync(int? customerId = null, int? shiftId = null, string? date = null, int page = 1, int pageSize = 20);
        Task<Sale?> GetByIdAsync(int id);
        Task<int> AddAsync(Sale entity);
        Task<int> UpdateAsync(Sale entity);
        Task<int> DeleteAsync(int id);
    }

    public class SaleRepository : BaseRepository, ISaleRepository
    {
        public SaleRepository(SqlConnectionFactory factory) : base(factory) { }

        public async Task<IEnumerable<Sale>> ListAsync(int? customerId = null, int? shiftId = null, string? date = null, int page = 1, int pageSize = 20)
        {
            var sql = @"SELECT * FROM dairy.sales WHERE (@customerId IS NULL OR customer_id = @customerId) AND (@shiftId IS NULL OR shift_id = @shiftId) AND (@date IS NULL OR date::text = @date) ORDER BY date DESC OFFSET @offset LIMIT @limit";
            return await QueryAsync<Sale>(sql, new { customerId, shiftId, date, offset = (page - 1) * pageSize, limit = pageSize });
        }

        public async Task<Sale?> GetByIdAsync(int id)
        {
            var sql = "SELECT * FROM dairy.sales WHERE id = @id";
            var result = await QueryAsync<Sale>(sql, new { id });
            return result.FirstOrDefault();
        }

        public async Task<int> AddAsync(Sale entity)
        {
            var sql = @"INSERT INTO dairy.sales (customer_id, shift_id, date, qty_ltr, unit_price, discount, paid_amt, due_amt, created_by) VALUES (@CustomerId, @ShiftId, @Date::date, @QtyLtr, @UnitPrice, @Discount, @PaidAmt, @DueAmt, @CreatedBy) RETURNING id";
            using var conn = _connectionFactory.CreateConnection();
            return await conn.ExecuteScalarAsync<int>(sql, entity);
        }

        public async Task<int> UpdateAsync(Sale entity)
        {
            var sql = @"UPDATE dairy.sales SET qty_ltr=@QtyLtr, unit_price=@UnitPrice, discount=@Discount, paid_amt=@PaidAmt, due_amt=@DueAmt WHERE id=@Id";
            return await ExecuteAsync(sql, entity);
        }

        public async Task<int> DeleteAsync(int id)
        {
            var sql = "DELETE FROM dairy.sales WHERE id=@id";
            return await ExecuteAsync(sql, new { id });
        }
    }
}
