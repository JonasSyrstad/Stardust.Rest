using System;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;

namespace Stardust.Interstellar.Rest.Annotations
{
    /// <summary>
    /// Default implementation for extracting user and client identifiers from claims or HTTP context
    /// </summary>
    public class DefaultCircuitBreakerIdentifierProvider : ICircuitBreakerIdentifierProvider
    {
        private static readonly string[] DefaultUserClaimTypes = {
            ClaimTypes.NameIdentifier,  // "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
            "sub",                      // JWT subject claim
            "user_id",                  // Common user ID claim
            "userId",                   // Alternative user ID claim
            "preferred_username",       // OIDC preferred username
            "upn"                      // User Principal Name
        };

        private static readonly string[] DefaultClientClaimTypes = {
            "client_id",               // OAuth2/OIDC client identifier
            "azp",                     // Authorized party (client_id in some contexts)
            "appid",                   // Application ID (Azure AD)
            "application_id"           // Generic application identifier
        };

        public virtual string GetUserIdentifier(IServiceProvider serviceProvider, string userIdentifierProperty = null)
        {
            try
            {
                // Try to get ClaimsPrincipal from service provider or current context
                var principal = GetClaimsPrincipal(serviceProvider);
                if (principal?.Identity?.IsAuthenticated != true)
                    return null;

                var claimsIdentity = principal.Identity as ClaimsIdentity;
                if (claimsIdentity == null)
                    return principal.Identity.Name; // Fallback to basic identity name

                // If a specific property is requested, look for that first
                if (!string.IsNullOrEmpty(userIdentifierProperty))
                {
                    var specificClaim = claimsIdentity.FindFirst(userIdentifierProperty);
                    if (specificClaim != null && !string.IsNullOrEmpty(specificClaim.Value))
                        return specificClaim.Value;
                }

                // Try default claim types in order of preference
                foreach (var claimType in DefaultUserClaimTypes)
                {
                    var claim = claimsIdentity.FindFirst(claimType);
                    if (claim != null && !string.IsNullOrEmpty(claim.Value))
                        return claim.Value;
                }

                // Final fallback to identity name
                return principal.Identity.Name;
            }
            catch
            {
                // If anything fails, return null to fall back to common scope
                return null;
            }
        }

        public virtual string GetClientIdentifier(IServiceProvider serviceProvider, string clientIdentifierProperty = null)
        {
            try
            {
                var principal = GetClaimsPrincipal(serviceProvider);
                if (principal?.Identity?.IsAuthenticated != true)
                    return null;

                var claimsIdentity = principal.Identity as ClaimsIdentity;
                if (claimsIdentity == null)
                    return null;

                // If a specific property is requested, look for that first
                if (!string.IsNullOrEmpty(clientIdentifierProperty))
                {
                    var specificClaim = claimsIdentity.FindFirst(clientIdentifierProperty);
                    if (specificClaim != null && !string.IsNullOrEmpty(specificClaim.Value))
                        return specificClaim.Value;
                }

                // Try default client claim types
                foreach (var claimType in DefaultClientClaimTypes)
                {
                    var claim = claimsIdentity.FindFirst(claimType);
                    if (claim != null && !string.IsNullOrEmpty(claim.Value))
                        return claim.Value;
                }

                return null;
            }
            catch
            {
                // If anything fails, return null to fall back to common scope
                return null;
            }
        }

        protected virtual ClaimsPrincipal GetClaimsPrincipal(IServiceProvider serviceProvider)
        {
            // Try to get from service provider first (dependency injection)
            if (serviceProvider != null)
            {
                // Try various common ways to access the current principal
                try
                {
                    // For ASP.NET Core - try to get IHttpContextAccessor
                    var httpContextAccessor = serviceProvider.GetService(Type.GetType("Microsoft.AspNetCore.Http.IHttpContextAccessor, Microsoft.AspNetCore.Http.Abstractions"));
                    if (httpContextAccessor != null)
                    {
                        var httpContext = httpContextAccessor.GetType().GetProperty("HttpContext")?.GetValue(httpContextAccessor);
                        if (httpContext != null)
                        {
                            var user = httpContext.GetType().GetProperty("User")?.GetValue(httpContext) as ClaimsPrincipal;
                            if (user != null)
                                return user;
                        }
                    }
                }
                catch
                {
                    // Ignore errors and try next approach
                }

                try
                {
                    // For ASP.NET Framework - try to get HttpContext
                    var httpContext = serviceProvider.GetService(Type.GetType("System.Web.HttpContext, System.Web"));
                    if (httpContext != null)
                    {
                        var user = httpContext.GetType().GetProperty("User")?.GetValue(httpContext) as IPrincipal;
                        if (user is ClaimsPrincipal claimsPrincipal)
                            return claimsPrincipal;
                    }
                }
                catch
                {
                    // Ignore errors and try next approach
                }
            }

            // Fallback to Thread.CurrentPrincipal (works in many scenarios)
            try
            {
                if (System.Threading.Thread.CurrentPrincipal is ClaimsPrincipal threadPrincipal)
                    return threadPrincipal;
            }
            catch
            {
                // Ignore and try next approach
            }

            // Final fallback - try ClaimsPrincipal.Current (available in .NET Framework)
            try
            {
                var currentPrincipalProperty = typeof(ClaimsPrincipal).GetProperty("Current");
                if (currentPrincipalProperty != null)
                {
                    return currentPrincipalProperty.GetValue(null) as ClaimsPrincipal;
                }
            }
            catch
            {
                // Ignore
            }

            return null;
        }
    }
}