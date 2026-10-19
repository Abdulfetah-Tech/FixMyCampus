namespace FixMyCampus.Application.Common;

public class AuthCookieSettings
{
    public const string SectionName = "AuthCookies";

    public string AccessCookieName { get; set; } = "fmc_access";
    public string RefreshCookieName { get; set; } = "fmc_refresh";
    public string RefreshCookiePath { get; set; } = "/api/auth";
}