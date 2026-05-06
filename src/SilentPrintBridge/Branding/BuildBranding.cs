namespace SilentPrintBridge.Branding;

public static class BuildBranding
{
#if CUSTOMER_BRANDING
    public static bool IsCustomerBuild => true;
#else
    public static bool IsCustomerBuild => false;
#endif

    public const string AppName = "SilentPrintBridge";
#if CUSTOMER_BRANDING
    public const string DeveloperName = "";
    public const string DeveloperProfileUrl = "";
    public const string DeveloperProfileLabel = "";
#else
    public const string DeveloperName = "Khalil Hasanzade";
    public const string DeveloperProfileUrl = "https://github.com/hasanzadekhalil";
    public const string DeveloperProfileLabel = "github.com/hasanzadekhalil";
#endif

    public static string TestReceiptFooter =>
        IsCustomerBuild
            ? string.Empty
            : @"
========================================
     Developed by Khalil Hasanzade
     github.com/hasanzadekhalil
========================================
";
}
