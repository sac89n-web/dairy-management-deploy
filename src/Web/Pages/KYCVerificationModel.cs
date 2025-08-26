using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Dairy.Infrastructure;
using Dairy.Web.Services;
using Dapper;

public class KYCVerificationModel : BasePageModel
{
    private readonly SqlConnectionFactory _connectionFactory;
    private readonly DigiLockerService _digiLockerService;

    public KYCVerificationModel(SqlConnectionFactory connectionFactory, DigiLockerService digiLockerService)
    {
        _connectionFactory = connectionFactory;
        _digiLockerService = digiLockerService;
    }

    public int PendingKYC { get; set; }
    public int VerifiedKYC { get; set; }
    public int RejectedKYC { get; set; }
    public int BankVerified { get; set; }
    public List<KYCFarmer> Farmers { get; set; } = new();

    public async Task OnGetAsync()
    {
        using var connection = GetConnection();
        
        // Mock KYC statistics since columns don't exist yet
        PendingKYC = 15;
        VerifiedKYC = 8;
        RejectedKYC = 2;
        BankVerified = 5;

        Farmers = (await connection.QueryAsync<KYCFarmer>(@"
            SELECT id, name, code, contact,
                   'Pending' as kyc_status, false as bank_verified,
                   '' as pan_number, '' as aadhar_number, '' as account_number,
                   false as digilocker_verified
            FROM dairy.farmer 
            ORDER BY id DESC")).ToList();
    }

    public async Task<IActionResult> OnPostVerifyAsync([FromBody] KYCRequest request)
    {
        using var connection = GetConnection();
        
        // Mock verification since KYC columns don't exist yet
        var isVerified = new Random().Next(1, 10) > 3; // 70% success rate
        
        return new JsonResult(new { success = true, verified = isVerified, aadharValid = isVerified, panValid = isVerified });
    }

    public async Task<IActionResult> OnPostRejectAsync([FromBody] KYCRequest request)
    {
        using var connection = GetConnection();
        // Mock rejection since KYC columns don't exist yet
        await Task.Delay(100);
        return new JsonResult(new { success = true });
    }

    public async Task<IActionResult> OnPostBankVerifyAsync([FromBody] KYCRequest request)
    {
        using var connection = GetConnection();
        
        // Simulate bank verification (penny drop)
        var verified = new Random().Next(1, 10) > 2; // 80% success rate
        
        // Mock update since bank columns don't exist yet
        await Task.Delay(100);
        
        return new JsonResult(new { success = true, verified });
    }
}

public class KYCFarmer
{
    public int id { get; set; }
    public string name { get; set; } = "";
    public string pan_number { get; set; } = "";
    public string aadhar_number { get; set; } = "";
    public string account_number { get; set; } = "";
    public string kyc_status { get; set; } = "Pending";
    public bool bank_verified { get; set; }
    public bool digilocker_verified { get; set; }
}

public class KYCRequest
{
    public int FarmerId { get; set; }
}