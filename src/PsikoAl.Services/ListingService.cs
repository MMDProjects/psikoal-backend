using Microsoft.EntityFrameworkCore;
using Npgsql;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Listing;
using PsikoAl.Common.Dtos.Listing.Create;
using PsikoAl.Common.Exceptions;
using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories.Abstractions;
using PsikoAl.Services.Abstractions;
using PsikoAl.Services.Mapping;

namespace PsikoAl.Services;

public sealed class ListingService(
    IUnitOfWork unitOfWork,
    ICategoryService categoryService) : IListingService
{
    public async Task<ListingDto> CreateAsync(
        Guid clientUserId,
        CreateListingDto request,
        CancellationToken cancellationToken)
    {
        var client = await unitOfWork.Profiles.GetByIdAsync(clientUserId, cancellationToken)
            ?? throw new DomainException(ErrorKeys.ProfileNotFound);

        if (client.Role != ProfileRoles.Client)
        {
            throw new DomainException(ErrorKeys.ListingClientRoleRequired);
        }

        if (!await categoryService.AllActiveNamesExistAsync(request.Specialization, cancellationToken))
        {
            throw new DomainException(ErrorKeys.ValidationFailed, "specialization");
        }

        var autoApprove = await unitOfWork.SystemSettings.GetBoolAsync(SystemSettingKeys.ListingAutoApprove, cancellationToken);

        var listing = new Listing
        {
            ClientId = clientUserId,
            Title = request.Title,
            Description = request.Description ?? string.Empty,
            Specialization = [.. request.Specialization],
            BudgetMin = request.BudgetMin,
            BudgetMax = request.BudgetMax,
            PreferredSessionType = request.PreferredSessionType,
            City = request.City,
            AssessmentResultId = request.AssessmentResultId,
        };

        if (autoApprove)
        {
            var expireDays = await unitOfWork.SystemSettings.GetIntAsync(SystemSettingKeys.ListingExpireDays, cancellationToken);
            listing.Status = ListingStatuses.Open;
            listing.ApprovedAt = DateTimeOffset.UtcNow;
            listing.ExpiresAt = DateTimeOffset.UtcNow.AddDays(expireDays);
        }

        await unitOfWork.Listings.AddAsync(listing, cancellationToken);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { MessageText: "LISTING_MAX_ACTIVE_EXCEEDED" })
        {
            throw new DomainException(ErrorKeys.ListingMaxActiveExceeded);
        }

        var assessmentResult = request.AssessmentResultId.HasValue
            ? await unitOfWork.AssessmentResults.QueryWithAssessment()
                .FirstOrDefaultAsync(result => result.Id == request.AssessmentResultId.Value, cancellationToken)
            : null;

        return ListingMapper.ToListingDto(listing, client, viewerIsOwner: true, viewerHasOffered: false, viewerOfferId: null, assessmentResult);
    }

    public async Task<ListingListResult> ListFeedAsync(ListingFeedFilters filters, CancellationToken cancellationToken)
    {
        var query = unitOfWork.Listings.Query()
            .Include(listing => listing.Client)
            .Where(listing => listing.Status == ListingStatuses.Open);

        if (filters.Specialization is { Count: > 0 })
        {
            query = query.Where(listing => listing.Specialization.Any(spec => filters.Specialization.Contains(spec)));
        }

        if (filters.SessionType is { Count: > 0 })
        {
            query = query.Where(listing => filters.SessionType.Contains(listing.PreferredSessionType));
        }

        if (filters.BudgetMin.HasValue)
        {
            query = query.Where(listing => listing.BudgetMax >= filters.BudgetMin.Value);
        }

        if (filters.BudgetMax.HasValue)
        {
            query = query.Where(listing => listing.BudgetMin <= filters.BudgetMax.Value);
        }

        query = filters.Sort switch
        {
            "budget_desc" => query.OrderByDescending(listing => listing.BudgetMax),
            "budget_asc" => query.OrderBy(listing => listing.BudgetMin),
            "offer_asc" => query.OrderBy(listing => listing.OfferCount),
            _ => query.OrderByDescending(listing => listing.CreatedAt),
        };

        var listings = await query.ToListAsync(cancellationToken);
        var dtos = listings.Select(listing => ListingMapper.ToListingDto(
            listing,
            listing.Client ?? throw new DomainException(ErrorKeys.ProfileNotFound),
            viewerIsOwner: false,
            viewerHasOffered: false,
            viewerOfferId: null)).ToList();

        return new ListingListResult(dtos, dtos.Count, ActiveCount: null);
    }

    public async Task<ListingListResult> ListMyAsync(Guid clientUserId, string? status, CancellationToken cancellationToken)
    {
        var client = await unitOfWork.Profiles.GetByIdAsync(clientUserId, cancellationToken)
            ?? throw new DomainException(ErrorKeys.ProfileNotFound);

        var query = unitOfWork.Listings.Query().Where(listing => listing.ClientId == clientUserId);
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(listing => listing.Status == status);
        }

        var listings = await query.OrderByDescending(listing => listing.CreatedAt).ToListAsync(cancellationToken);
        var dtos = listings.Select(listing => ListingMapper.ToListingDto(
            listing,
            client,
            viewerIsOwner: true,
            viewerHasOffered: false,
            viewerOfferId: null)).ToList();

        var activeCount = await unitOfWork.Listings.CountActiveByClientAsync(clientUserId, cancellationToken);
        return new ListingListResult(dtos, dtos.Count, activeCount);
    }

    public async Task<ListingDto> GetByIdAsync(Guid listingId, Guid viewerUserId, CancellationToken cancellationToken)
    {
        var listing = await unitOfWork.Listings.GetWithClientAsync(listingId, cancellationToken)
            ?? throw new DomainException(ErrorKeys.ListingNotFound);
        var client = listing.Client ?? throw new DomainException(ErrorKeys.ProfileNotFound);

        var isOwner = listing.ClientId == viewerUserId;
        if (listing.Status != ListingStatuses.Open && !isOwner)
        {
            throw new DomainException(ErrorKeys.ListingNotFound);
        }

        var viewerOffer = isOwner
            ? null
            : await unitOfWork.Offers.Query()
                .FirstOrDefaultAsync(offer => offer.ListingId == listingId && offer.ExpertId == viewerUserId, cancellationToken);

        var assessmentResult = listing.AssessmentResultId.HasValue
            ? await unitOfWork.AssessmentResults.QueryWithAssessment()
                .FirstOrDefaultAsync(result => result.Id == listing.AssessmentResultId.Value, cancellationToken)
            : null;

        return ListingMapper.ToListingDto(
            listing, client, isOwner,
            viewerHasOffered: viewerOffer is not null,
            viewerOfferId: viewerOffer?.Id,
            assessmentResult);
    }

    public async Task<ListingDto> CloseAsync(Guid clientUserId, Guid listingId, CancellationToken cancellationToken)
    {
        var listing = await unitOfWork.Listings.GetWithClientAsync(listingId, cancellationToken)
            ?? throw new DomainException(ErrorKeys.ListingNotFound);
        var client = listing.Client ?? throw new DomainException(ErrorKeys.ProfileNotFound);

        if (listing.ClientId != clientUserId)
        {
            throw new DomainException(ErrorKeys.ListingNotFound);
        }

        if (listing.Status != ListingStatuses.Open)
        {
            throw new DomainException(ErrorKeys.ListingNotOpen);
        }

        listing.Status = ListingStatuses.Closed;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ListingMapper.ToListingDto(listing, client, viewerIsOwner: true, viewerHasOffered: false, viewerOfferId: null);
    }
}
