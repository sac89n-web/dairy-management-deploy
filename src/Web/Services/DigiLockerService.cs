using System.Text.Json;

namespace Dairy.Web.Services;

public class DigiLockerService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DigiLockerService> _logger;

    public DigiLockerService(HttpClient httpClient, IConfiguration configuration, ILogger<DigiLockerService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        
        _httpClient.BaseAddress = new Uri(_configuration["DigiLocker:BaseUrl"] ?? "https://api.digitallocker.gov.in/");
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_configuration["DigiLocker:ApiKey"]}");
    }

    public async Task<DigiLockerResponse> VerifyAadharAsync(string aadharNumber)
    {
        try
        {
            var request = new { aadhaar = aadharNumber };
            var response = await _httpClient.PostAsJsonAsync("v1/verify/aadhaar", request);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<DigiLockerApiResponse>(content);
                
                return new DigiLockerResponse
                {
                    IsValid = result?.status == "success",
                    Name = result?.data?.name ?? "",
                    DocumentNumber = aadharNumber,
                    DocumentType = "Aadhar",
                    VerificationDate = DateTime.UtcNow
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DigiLocker Aadhar verification failed for {AadharNumber}", aadharNumber);
        }

        return new DigiLockerResponse { IsValid = false, DocumentType = "Aadhar" };
    }

    public async Task<DigiLockerResponse> VerifyPanAsync(string panNumber)
    {
        try
        {
            var request = new { pan = panNumber };
            var response = await _httpClient.PostAsJsonAsync("v1/verify/pan", request);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<DigiLockerApiResponse>(content);
                
                return new DigiLockerResponse
                {
                    IsValid = result?.status == "success",
                    Name = result?.data?.name ?? "",
                    DocumentNumber = panNumber,
                    DocumentType = "PAN",
                    VerificationDate = DateTime.UtcNow
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DigiLocker PAN verification failed for {PanNumber}", panNumber);
        }

        return new DigiLockerResponse { IsValid = false, DocumentType = "PAN" };
    }
}

public class DigiLockerResponse
{
    public bool IsValid { get; set; }
    public string Name { get; set; } = "";
    public string DocumentNumber { get; set; } = "";
    public string DocumentType { get; set; } = "";
    public DateTime VerificationDate { get; set; }
    public string ErrorMessage { get; set; } = "";
}

public class DigiLockerApiResponse
{
    public string status { get; set; } = "";
    public DigiLockerData? data { get; set; }
}

public class DigiLockerData
{
    public string name { get; set; } = "";
    public string dob { get; set; } = "";
    public string gender { get; set; } = "";
    public string address { get; set; } = "";
}