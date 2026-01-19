using System.ComponentModel.DataAnnotations;

namespace ClercqIt.Api.Configuration;

public class CloudIAMSettings
{
    public const string SectionName = "CloudIAM";

    [Required(ErrorMessage = "CloudIAM:ApiUrl is required. See docs/ciam.md for configuration instructions.")]
    [Url(ErrorMessage = "CloudIAM:ApiUrl must be a valid URL.")]
    public string ApiUrl { get; set; } = string.Empty;

    [Required(ErrorMessage = "CloudIAM:ApiKey is required. See docs/ciam.md for configuration instructions.")]
    public string ApiKey { get; set; } = string.Empty;

    [Required(ErrorMessage = "CloudIAM:ClientRedirectUrl is required. See docs/ciam.md for configuration instructions.")]
    [Url(ErrorMessage = "CloudIAM:ClientRedirectUrl must be a valid URL.")]
    public string ClientRedirectUrl { get; set; } = string.Empty;
}
