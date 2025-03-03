using Blazored.LocalStorage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.JSInterop;
using OSI.Core.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace OSI.Core.Auth
{
    public class TokenServerAuthenticationStateProvider : ServerAuthenticationStateProvider, IDisposable
    {
        private const string tokenKey = "authToken";
        private readonly ILocalStorageService localStorageService;
        private readonly IServiceProvider serviceProvider;

        private CancellationTokenSource cancellationTokenSource;

        public TokenServerAuthenticationStateProvider(ILocalStorageService localStorageService, IServiceProvider serviceProvider)
        {
            this.localStorageService = localStorageService;
            this.serviceProvider = serviceProvider;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var token = await GetToken();
            return new AuthenticationState(serviceProvider.GetRequiredService<IAuthSvc>().ValidateToken(token, out _));
        }

        private async Task RefreshTokenOnExpire(TimeSpan delay, IEnumerable<Claim> claims, CancellationToken cancellationToken)
        {
            try
            {
                Debug.WriteLine($"{GetHashCode()} refresh token delay {delay}");
                await Task.Delay(delay, cancellationToken);
                Debug.WriteLine($"{GetHashCode()} refresh token set");
                await SetToken(serviceProvider.GetRequiredService<IAuthSvc>().GenerateJwtToken(claims), false);
            }
            catch (TaskCanceledException ex)
            {
                if (ex.CancellationToken != cancellationToken)
                    throw;
            }
            finally
            {
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = null;
            }
        }

        public async Task<string> GetToken() => await localStorageService.GetItemAsStringAsync(tokenKey);

        public async Task SetToken(string token, bool notify = true)
        {
            Debug.WriteLine($"{GetHashCode()} set token {notify} {token}");
            if (cancellationTokenSource?.IsCancellationRequested == false)
            {
                cancellationTokenSource?.Cancel();
            }
            if (token == null)
            {
                await localStorageService.RemoveItemAsync(tokenKey);
            }
            else
            {
                await localStorageService.SetItemAsStringAsync(tokenKey, token);
                ClaimsPrincipal user = serviceProvider.GetRequiredService<IAuthSvc>().ValidateToken(token, out SecurityToken validatedToken);
                if (validatedToken != null)
                {
                    cancellationTokenSource = new CancellationTokenSource();
                    TimeSpan delay = validatedToken.ValidTo - DateTime.UtcNow;
                    if (delay < TimeSpan.Zero)
                        delay = TimeSpan.Zero;
                    _ = RefreshTokenOnExpire(delay, user.Claims, cancellationTokenSource.Token);
                }
            }
            if (notify)
            {
                NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
            }
        }

        public void Dispose()
        {
            if (cancellationTokenSource?.IsCancellationRequested == false)
            {
                cancellationTokenSource?.Cancel();
            }
            Debug.WriteLine($"{GetHashCode()} refresh token dispose");
        }
    }
}
