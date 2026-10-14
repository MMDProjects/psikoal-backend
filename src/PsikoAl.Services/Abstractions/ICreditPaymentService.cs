using PsikoAl.Common.Dtos.Payment;

namespace PsikoAl.Services.Abstractions;

public interface ICreditPaymentService
{
    Task<PayTrInitResultDto> InitPayTrAsync(
        Guid expertUserId,
        Guid packageId,
        string userIp,
        CancellationToken cancellationToken);

    Task<PayTrCallbackOutcome> HandlePayTrCallbackAsync(PayTrCallbackDto callback, CancellationToken cancellationToken);
}
