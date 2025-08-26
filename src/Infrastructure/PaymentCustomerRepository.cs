using Dairy.Domain;
using Dapper;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq; // Added

namespace Dairy.Infrastructure
{
    public interface IPaymentCustomerRepository
    {
        Task<IEnumerable<PaymentCustomer>> ListAsync(int? customerId = null, int page = 1, int pageSize = 20);
        Task<PaymentCustomer?> GetByIdAsync(int id);
        Task<int> AddAsync(PaymentCustomer entity);
        Task<int> UpdateAsync(PaymentCustomer entity);
        Task<int> DeleteAsync(int id);
    }

    public class PaymentCustomerRepository : BaseRepository, IPaymentCustomerRepository
    {
        public PaymentCustomerRepository(SqlConnectionFactory factory) : base(factory) { }

        public async Task<IEnumerable<PaymentCustomer>> ListAsync(int? customerId = null, int page = 1, int pageSize = 20)
        {
            var sql = @"SELECT * FROM dairy.payment_customer WHERE (@customerId IS NULL OR customer_id = @customerId) ORDER BY date DESC OFFSET @offset LIMIT @limit";
            return await QueryAsync<PaymentCustomer>(sql, new { customerId, offset = (page - 1) * pageSize, limit = pageSize });
        }

        public async Task<PaymentCustomer?> GetByIdAsync(int id)
        {
            var sql = "SELECT * FROM dairy.payment_customer WHERE id = @id";
            var result = await QueryAsync<PaymentCustomer>(sql, new { id });
            return result.FirstOrDefault();
        }

        public async Task<int> AddAsync(PaymentCustomer entity)
        {
            var sql = @"INSERT INTO dairy.payment_customer (customer_id, sale_id, amount, date, invoice_no, pdf_path) VALUES (@CustomerId, @SaleId, @Amount, @Date, @InvoiceNo, @PdfPath) RETURNING id";
            using var conn = _connectionFactory.CreateConnection();
            return await conn.ExecuteScalarAsync<int>(sql, entity);
        }

        public async Task<int> UpdateAsync(PaymentCustomer entity)
        {
            var sql = @"UPDATE dairy.payment_customer SET amount=@Amount, date=@Date, invoice_no=@InvoiceNo, pdf_path=@PdfPath WHERE id=@Id";
            return await ExecuteAsync(sql, entity);
        }

        public async Task<int> DeleteAsync(int id)
        {
            var sql = "DELETE FROM dairy.payment_customer WHERE id=@id";
            return await ExecuteAsync(sql, new { id });
        }
    }
}
