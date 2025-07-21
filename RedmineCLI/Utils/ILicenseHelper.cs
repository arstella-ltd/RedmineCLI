using System.Collections.Generic;
using System.Threading.Tasks;

using RedmineCLI.Models;

namespace RedmineCLI.Utils;

public interface ILicenseHelper
{
    Task<Dictionary<string, LicenseData>> GetLicenseInfoAsync();
    Task<LicenseInfo> GetVersionInfoAsync();
    Task GenerateThirdPartyNoticesAsync(string filePath);
    Task<Dictionary<string, LicenseData>> GetEmbeddedLicensesAsync();
}