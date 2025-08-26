using Dairy.Domain;
using Dapper;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq; // Added

namespace Dairy.Infrastructure
{
    public interface IPaymentFarmerRepository
    {
        Task<IEnumerable<PaymentFarmer>> ListAsync(int? farmerId = null, int page = 1, int pageSize = 20);
        Task<PaymentFarmer?> GetByIdAsync(int id);
        Task<int> AddAsync(PaymentFarmer entity);
        Task<int> UpdateAsync(PaymentFarmer entity);
        Task<int> DeleteAsync(int id);
    }

    public class PaymentFarmerRepository : BaseRepository, IPaymentFarmerRepository
    {
        public PaymentFarmerRepository(SqlConnectionFactory factory) : base(factory) { }

        public async Task<IEnumerable<PaymentFarmer>> ListAsync(int? farmerId = null, int page = 1, int pageSize = 20)
        {
            var sql = @"SELECT * FROM dairy.payment_farmer WHERE (@farmerId IS NULL OR farmer_id = @farmerId) ORDER BY date DESC OFFSET @offset LIMIT @limit";
            return await QueryAsync<PaymentFarmer>(sql, new { farmerId, offset = (page - 1) * pageSize, limit = pageSize });
        }

        public async Task<PaymentFarmer?> GetByIdAsync(int id)
        {
            var sql = "SELECT * FROM dairy.payment_farmer WHERE id = @id";
            var result = await QueryAsync<PaymentFarmer>(sql, new { id });
            return result.FirstOrDefault();
        }

        public async Task<int> AddAsync(PaymentFarmer entity)
        {
            var sql = @"INSERT INTO dairy.payment_farmer (farmer_id, milk_collection_id, amount, date, invoice_no, pdf_path) VALUES (@FarmerId, @MilkCollectionId, @Amount, @Date, @InvoiceNo, @PdfPath) RETURNING id";
            using var conn = _connectionFactory.CreateConnection();
            return await conn.ExecuteScalarAsync<int>(sql, entity);
        }

        public async Task<int> UpdateAsync(PaymentFarmer entity)
        {
            var sql = @"UPDATE dairy.payment_farmer SET amount=@Amount, date=@Date, invoice_no=@InvoiceNo, pdf_path=@PdfPath WHERE id=@Id";
            return await ExecuteAsync(sql, entity);
        }

        public async Task<int> DeleteAsync(int id)
        {
            var sql = "DELETE FROM dairy.payment_farmer WHERE id=@id";
            return await ExecuteAsync(sql, new { id });
        }
    }
}
