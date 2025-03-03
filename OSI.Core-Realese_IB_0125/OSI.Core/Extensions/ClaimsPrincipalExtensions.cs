using OSI.Core.Models.Db;
using System.Linq;
using System.Security.Claims;

namespace OSI.Core.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static bool IsInRoles(this ClaimsPrincipal principal, params string[] roles)
        {
            foreach (var identity in principal.Identities)
            {
                if (identity != null)
                {
                    if (identity.HasClaim(claim => claim.Type == identity.RoleClaimType && roles.Contains(claim.Value)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
