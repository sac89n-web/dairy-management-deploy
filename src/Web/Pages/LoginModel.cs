using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Dairy.Web.Services;

public class UserLoginModel : PageModel
{
    private readonly OtpService _otpService;

    public UserLoginModel(OtpService otpService)
    {
        _otpService = otpService;
    }

    public string Mobile { get; set; } = "";
    public bool ShowOtpField { get; set; } = false;
    public string ErrorMessage { get; set; } = "";
    public string SuccessMessage { get; set; } = "";

    public void OnGet()
    {
        // Check if user is already logged in
        if (HttpContext.Session.GetString("UserId") != null)
        {
            Response.Redirect("/Dashboard");
        }
    }

    public async Task<IActionResult> OnPostAsync(string mobile, string? otp, string action)
    {
        Mobile = mobile;

        if (action == "sendotp")
        {
            try
            {
                // For demo, allow any mobile number
                // In production, check if user exists in database

                // Generate and send OTP
                var generatedOtp = await _otpService.GenerateOtpAsync(mobile);
                
                // In production, send SMS here
                // For demo, show OTP in success message
                SuccessMessage = "OTP sent successfully! Demo OTP: 2025";
                ShowOtpField = true;
                
                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to send OTP: {ex.Message}";
                return Page();
            }
        }
        else if (action == "verify")
        {
            try
            {
                if (string.IsNullOrEmpty(otp))
                {
                    ErrorMessage = "Please enter OTP";
                    ShowOtpField = true;
                    return Page();
                }

                // Validate OTP
                var isValidOtp = await _otpService.ValidateOtpAsync(mobile, otp);
                if (!isValidOtp)
                {
                    ErrorMessage = "Invalid or expired OTP";
                    ShowOtpField = true;
                    return Page();
                }

                // Get user details
                var user = await _otpService.GetUserByMobileAsync(mobile);
                if (user == null)
                {
                    ErrorMessage = "User not found";
                    return Page();
                }

                // Set session
                HttpContext.Session.SetString("UserId", ((int)user.id).ToString());
                HttpContext.Session.SetString("UserName", (string)user.name);
                HttpContext.Session.SetString("UserRole", (string)user.role);
                HttpContext.Session.SetString("UserMobile", (string)user.mobile);

                return RedirectToPage("/Dashboard");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Login failed: {ex.Message}";
                ShowOtpField = true;
                return Page();
            }
        }

        return Page();
    }
}