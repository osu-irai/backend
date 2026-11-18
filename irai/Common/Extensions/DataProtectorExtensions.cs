using System.Security.Cryptography;
using ChiffadicErrors;
using irai.Features.Requests.Services;
using Microsoft.AspNetCore.DataProtection;
using static ChiffadicErrors.Result;

namespace irai.Common.Extensions;

public static class DataProtectorExtensions
{
    /// <summary>
    /// Unprotects a stored OAuth token. Undecryptable (e.g. pre-encryption) rows become
    /// <see cref="NotAuthorized" /> so callers force a re-login instead of throwing.
    /// </summary>
    public static Result<string, RequestServiceError> UnprotectToken(this IDataProtector protector, string ciphertext)
    {
        try
        {
            return Ok(protector.Unprotect(ciphertext));
        }
        catch (CryptographicException)
        {
            return Error<RequestServiceError>(new NotAuthorized());
        }
    }
}
