namespace DoanPhi.Infrastructure.Persistence;

public class SupabaseOptions
{
    public const string SectionName = "Supabase";

    public string Url { get; set; } = string.Empty;
    public string AnonKey { get; set; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Url) && 
                                !string.IsNullOrWhiteSpace(AnonKey) &&
                                !Url.Contains("YOUR_PROJECT_REF") &&
                                !AnonKey.Contains("YOUR_ANON_KEY");
}
