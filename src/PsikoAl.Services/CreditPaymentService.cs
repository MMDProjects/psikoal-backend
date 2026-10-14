using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Payment;
using PsikoAl.Common.Exceptions;
using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories.Abstractions;
using PsikoAl.Services.Abstractions;
using PsikoAl.Services.Options;
using PsikoAl.Services.Payments;

namespace PsikoAl.Services;

// Kredi satın alma YALNIZ web sitesinden (docs/PAYMENTS.md). Akış:
//   init     → PENDING ödeme satırı + PayTR get-token → iframe adresi
//   callback → hash doğrulama → PENDING'den PAID/FAILED'a TEK geçiş → (PAID ise)
//              kredi + PURCHASE defter satırı + uygulama içi bildirim
// PayTR aynı bildirimi birden fazla kez gönderebilir; geçiş koşullu UPDATE olduğu
// için ikinci bildirim krediyi tekrar yüklemez (AlreadyProcessed → yine "OK").
public sealed class CreditPaymentService(
    IUnitOfWork unitOfWork,
    IPayTrClient payTrClient,
    INotificationService notificationService,
    IOptions<PayTrOptions> options,
    TimeProvider timeProvider,
    ILogger<CreditPaymentService> logger) : ICreditPaymentService
{
    public const string IframeUrlPrefix = "https://www.paytr.com/odeme/guvenli/";
    public const string SuccessStatus = "success";
    private const string MerchantOidPrefix = "PA";
    private const string FallbackAddress = "Türkiye";
    private const string FallbackPhone = "0000000000";

    public async Task<PayTrInitResultDto> InitPayTrAsync(
        Guid expertUserId,
        Guid packageId,
        string userIp,
        CancellationToken cancellationToken)
    {
        if (!options.Value.IsConfigured)
        {
            throw new DomainException(ErrorKeys.PaymentProviderUnavailable);
        }

        var expert = await unitOfWork.Experts.GetWithProfileAsync(expertUserId, cancellationToken)
            ?? throw await ExpertAccess.MissingExpertErrorAsync(
                unitOfWork, expertUserId, ErrorKeys.ExpertRoleRequired, cancellationToken);
        var profile = expert.Profile ?? throw new DomainException(ErrorKeys.ProfileNotFound);

        if (expert.Status != ExpertStatuses.Approved)
        {
            throw new DomainException(ErrorKeys.CreditPurchaseExpertNotApproved);
        }

        var package = await unitOfWork.CreditPackages.GetByIdAsync(packageId, cancellationToken);
        if (package is not { IsActive: true })
        {
            throw new DomainException(ErrorKeys.CreditPackageNotFound, "packageId");
        }

        var payment = new CreditPayment
        {
            ExpertId = expert.Id,
            PackageId = package.Id,
            MerchantOid = NewMerchantOid(),
            AmountTry = package.PriceTry,
            Credits = package.Credits,
            Status = CreditPaymentStatuses.Pending,
        };

        await unitOfWork.CreditPayments.AddAsync(payment, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var tokenResult = await payTrClient.GetIframeTokenAsync(
            new PayTrTokenRequest(
                userIp,
                payment.MerchantOid,
                profile.Email,
                ToKurus(package.PriceTry),
                [new PayTrBasketItem($"{package.Name} ({package.Credits} kredi)", package.PriceTry, 1)],
                $"{profile.FirstName} {profile.LastName}".Trim(),
                string.IsNullOrWhiteSpace(profile.City) ? FallbackAddress : profile.City,
                string.IsNullOrWhiteSpace(profile.Phone) ? FallbackPhone : profile.Phone),
            cancellationToken);

        if (tokenResult.Token is not { Length: > 0 } token)
        {
            await unitOfWork.CreditPayments.TryCompleteAsync(
                payment.Id,
                CreditPaymentStatuses.Failed,
                JsonSerializer.Serialize(new { stage = "get-token", reason = tokenResult.FailureReason }),
                timeProvider.GetUtcNow(),
                cancellationToken);
            throw new DomainException(ErrorKeys.PaymentProviderUnavailable);
        }

        return new PayTrInitResultDto(token, payment.MerchantOid, IframeUrlPrefix + token);
    }

    public async Task<PayTrCallbackOutcome> HandlePayTrCallbackAsync(
        PayTrCallbackDto callback,
        CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (!settings.IsConfigured
            || !PayTrHash.IsValidCallback(
                callback.MerchantOid,
                callback.Status,
                callback.TotalAmount,
                callback.Hash,
                settings.MerchantSalt ?? string.Empty,
                settings.MerchantKey ?? string.Empty))
        {
            logger.LogWarning("PayTR bildirimi reddedildi: hash geçersiz (oid: {MerchantOid})", callback.MerchantOid);
            return PayTrCallbackOutcome.InvalidHash;
        }

        var payment = await unitOfWork.CreditPayments.GetByMerchantOidAsync(callback.MerchantOid, cancellationToken);
        if (payment is null)
        {
            logger.LogError("PayTR bildirimi bilinmeyen sipariş için geldi (oid: {MerchantOid})", callback.MerchantOid);
            return PayTrCallbackOutcome.UnknownOrder;
        }

        if (payment.Status != CreditPaymentStatuses.Pending)
        {
            return PayTrCallbackOutcome.AlreadyProcessed;
        }

        var payload = ProviderPayload(callback);
        if (callback.Status == SuccessStatus && IsFullyPaid(callback.TotalAmount, payment.AmountTry))
        {
            return await CompletePaidAsync(payment, payload, cancellationToken);
        }

        if (callback.Status == SuccessStatus)
        {
            logger.LogError(
                "PayTR başarılı bildirdi ama tahsil edilen tutar eksik (oid: {MerchantOid}, total_amount: {TotalAmount})",
                callback.MerchantOid,
                callback.TotalAmount);
        }

        var failed = await unitOfWork.CreditPayments.TryCompleteAsync(
            payment.Id,
            CreditPaymentStatuses.Failed,
            payload,
            timeProvider.GetUtcNow(),
            cancellationToken);
        return failed ? PayTrCallbackOutcome.Failed : PayTrCallbackOutcome.AlreadyProcessed;
    }

    public static int ToKurus(decimal amountTry)
        => (int)decimal.Round(amountTry * 100m, 0, MidpointRounding.AwayFromZero);

    private async Task<PayTrCallbackOutcome> CompletePaidAsync(
        CreditPayment payment,
        string payload,
        CancellationToken cancellationToken)
    {
        int balance;
        await using (var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken))
        {
            var transitioned = await unitOfWork.CreditPayments.TryCompleteAsync(
                payment.Id,
                CreditPaymentStatuses.Paid,
                payload,
                timeProvider.GetUtcNow(),
                cancellationToken);
            if (!transitioned)
            {
                return PayTrCallbackOutcome.AlreadyProcessed;
            }

            balance = await unitOfWork.ExpertCredits.TryApplyDeltaAsync(payment.ExpertId, payment.Credits, cancellationToken)
                ?? throw new InvalidOperationException($"Ödemenin uzmanı bulunamadı (oid: {payment.MerchantOid}).");

            await unitOfWork.CreditTransactions.AddAsync(
                new CreditTransaction
                {
                    ExpertId = payment.ExpertId,
                    Amount = payment.Credits,
                    BalanceAfter = balance,
                    Type = CreditTransactionTypes.Purchase,
                    PaymentId = payment.Id,
                },
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        await notificationService.NotifyAsync(
            payment.ExpertId,
            NotificationTypes.CreditPurchased,
            new Dictionary<string, string>
            {
                ["credits"] = payment.Credits.ToString(CultureInfo.InvariantCulture),
                ["balance"] = balance.ToString(CultureInfo.InvariantCulture),
            },
            JsonSerializer.Serialize(new { paymentId = payment.Id.ToString() }),
            cancellationToken);

        return PayTrCallbackOutcome.Paid;
    }

    // Taksit/vade farkıyla total_amount ödeme tutarını AŞABİLİR; eksik olamaz.
    private static bool IsFullyPaid(string totalAmount, decimal expectedTry)
        => int.TryParse(totalAmount, NumberStyles.None, CultureInfo.InvariantCulture, out var kurus)
            && kurus >= ToKurus(expectedTry);

    private static string NewMerchantOid()
        => MerchantOidPrefix + Guid.NewGuid().ToString("N").ToUpperInvariant();

    // Hash ve kart bilgisi BİLEREK yok: yalnız mutabakat için gereken alanlar.
    private static string ProviderPayload(PayTrCallbackDto callback)
        => JsonSerializer.Serialize(new
        {
            status = callback.Status,
            totalAmount = callback.TotalAmount,
            paymentAmount = callback.PaymentAmount,
            paymentType = callback.PaymentType,
            currency = callback.Currency,
            testMode = callback.TestMode,
            failedReasonCode = callback.FailedReasonCode,
            failedReasonMessage = callback.FailedReasonMessage,
        });
}
