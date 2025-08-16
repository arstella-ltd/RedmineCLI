// This file has been moved to RedmineCLI.Common.Authentication.AuthenticationHelper
// Keeping this for backward compatibility
using Microsoft.Extensions.Logging;

using RedmineCLI.Common.Authentication;
using RedmineCLI.Common.Models;

namespace RedmineCLI.Extension.Board
{
    [Obsolete("Use RedmineCLI.Common.Authentication.AuthenticationHelper instead")]
    public static class AuthenticationHelper
    {
        public static async Task<string?> CreateSessionFromCredentialsAsync(
            string redmineUrl,
            StoredCredential credential,
            ILogger? logger)
        {
            return await Common.Authentication.AuthenticationHelper.CreateSessionFromCredentialsAsync(
                redmineUrl, credential, logger);
        }
    }
}
