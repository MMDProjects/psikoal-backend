using PsikoAl.Services.Payments;

namespace PsikoAl.Services.Abstractions;

public interface IPayTrClient
{
    Task<PayTrTokenResult> GetIframeTokenAsync(PayTrTokenRequest request, CancellationToken cancellationToken);
}
