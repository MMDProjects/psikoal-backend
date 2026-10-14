using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using PsikoAl.Api.Middleware;
using PsikoAl.Common.Constants;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Api.ContractTests;

// Sözleşme §4: dondurulmuş/silinmiş hesabın elindeki geçerli token 401 ACCOUNT_INACTIVE
// almalı; anonim uçlar (login/refresh) eski token başlıkta olsa da etkilenmemeli.
public sealed class AccountStatusMiddlewareTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public async Task Inactive_account_on_protected_endpoint_gets_401_account_inactive()
    {
        var context = NewContext(new AuthorizeAttribute());
        var nextCalled = false;

        await new AccountStatusMiddleware(_ => { nextCalled = true; return Task.CompletedTask; })
            .InvokeAsync(context, new StubAccountStatus(isActive: false));

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        using var body = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal(ErrorKeys.AccountInactive, body.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Active_account_passes_through()
    {
        var context = NewContext(new AuthorizeAttribute());
        var nextCalled = false;

        await new AccountStatusMiddleware(_ => { nextCalled = true; return Task.CompletedTask; })
            .InvokeAsync(context, new StubAccountStatus(isActive: true));

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task Anonymous_endpoint_is_not_checked_even_with_a_stale_token()
    {
        var context = NewContext();
        var status = new StubAccountStatus(isActive: false);
        var nextCalled = false;

        await new AccountStatusMiddleware(_ => { nextCalled = true; return Task.CompletedTask; })
            .InvokeAsync(context, status);

        Assert.True(nextCalled);
        Assert.Equal(0, status.Calls);
    }

    [Fact]
    public async Task AllowAnonymous_overrides_class_level_authorize()
    {
        var context = NewContext(new AuthorizeAttribute(), new AllowAnonymousAttribute());
        var nextCalled = false;

        await new AccountStatusMiddleware(_ => { nextCalled = true; return Task.CompletedTask; })
            .InvokeAsync(context, new StubAccountStatus(isActive: false));

        Assert.True(nextCalled);
    }

    private static DefaultHttpContext NewContext(params object[] metadata)
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, UserId.ToString())],
                authenticationType: "Bearer")),
        };
        context.Response.Body = new MemoryStream();
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(metadata), "test"));
        return context;
    }

    private sealed class StubAccountStatus(bool isActive) : IAccountStatusService
    {
        public int Calls { get; private set; }

        public Task<bool> IsActiveAsync(Guid userId, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(isActive);
        }

        public void Invalidate(Guid userId)
        {
        }
    }
}
