using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Dairy.Infrastructure;
using Dapper;

public class MasterDataModel : BasePageModel
{
    private readonly SqlConnectionFactory _connectionFactory;

    public MasterDataModel(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public List<Farmer> Farmers { get; set; } = new();
    public List<Customer> Customers { get; set; } = new();

    public async Task OnGetAsync()
    {
        using var connection = GetConnection();
        Farmers = (await connection.QueryAsync<Farmer>("SELECT id, name, code, contact, bank_id, branch_id FROM dairy.farmer ORDER BY name")).ToList();
        Customers = (await connection.QueryAsync<Customer>("SELECT id, name, contact, branch_id FROM dairy.customer ORDER BY name")).ToList();
    }

    public async Task<IActionResult> OnPostAddFarmerAsync(string name, string code, string contact)
    {
        using var connection = GetConnection();
        await connection.ExecuteAsync(@"
            INSERT INTO dairy.farmer (name, code, contact, bank_id, branch_id) 
            VALUES (@name, @code, @contact, @bankId, 1)",
            new { name, code, contact, bankId = (int?)null });
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAddCustomerAsync(string name, string contact)
    {
        using var connection = GetConnection();
        await connection.ExecuteAsync(@"
            INSERT INTO dairy.customer (name, contact, branch_id) 
            VALUES (@name, @contact, 1)",
            new { name, contact });
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteFarmerAsync(int id)
    {
        using var connection = GetConnection();
        await connection.ExecuteAsync("DELETE FROM dairy.farmer WHERE id = @id", new { id });
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteCustomerAsync(int id)
    {
        using var connection = GetConnection();
        await connection.ExecuteAsync("DELETE FROM dairy.customer WHERE id = @id", new { id });
        return RedirectToPage();
    }

    public async Task<IActionResult> OnGetGetFarmerAsync(int id)
    {
        using var connection = GetConnection();
        var farmer = await connection.QuerySingleOrDefaultAsync<Farmer>(
            "SELECT * FROM dairy.farmer WHERE id = @id", new { id });
        return new JsonResult(farmer);
    }

    public async Task<IActionResult> OnGetGetCustomerAsync(int id)
    {
        using var connection = GetConnection();
        var customer = await connection.QuerySingleOrDefaultAsync<Customer>(
            "SELECT * FROM dairy.customer WHERE id = @id", new { id });
        return new JsonResult(customer);
    }

    public async Task<IActionResult> OnPostUpdateFarmerAsync(int id, string name, string code, string contact, string email, string address, string village, string taluka, string district, string state, string pincode, string bankName, string accountNumber, string ifscCode, string aadharNumber, string panNumber)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(@"
            UPDATE dairy.farmer SET 
                name = @name, code = @code, contact = @contact, email = @email, address = @address,
                village = @village, taluka = @taluka, district = @district, state = @state, pincode = @pincode,
                bank_name = @bankName, account_number = @accountNumber, ifsc_code = @ifscCode,
                aadhar_number = @aadharNumber, pan_number = @panNumber, updated_at = NOW()
            WHERE id = @id",
            new { id, name, code, contact, email, address, village, taluka, district, state, pincode, bankName, accountNumber, ifscCode, aadharNumber, panNumber });
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateCustomerAsync(int id, string name, string contact, string email, string address, string city, string state, string pincode, string customerType, string gstNumber)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(@"
            UPDATE dairy.customer SET 
                name = @name, contact = @contact, email = @email, address = @address,
                city = @city, state = @state, pincode = @pincode,
                customer_type = @customerType, gst_number = @gstNumber, updated_at = NOW()
            WHERE id = @id",
            new { id, name, contact, email, address, city, state, pincode, customerType, gstNumber });
        return RedirectToPage();
    }
}

