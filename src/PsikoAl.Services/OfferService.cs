using Microsoft.EntityFrameworkCore;
using Npgsql;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Offer;
using PsikoAl.Common.Dtos.Offer.Create;
using PsikoAl.Common.Exceptions;
using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories.Abstractions;
using PsikoAl.Services.Abstractions;
using PsikoAl.Services.Mapping;

namespace PsikoAl.Services;

public sealed class OfferService(IUnitOfWork unitOfWork) : IOfferService
{
    public async Task<OfferDto> CreateAsync(Guid expertUserId, CreateOfferDto request, CancellationToken cancellationToken)
    {
        var expert = await unitOfWork.Experts.GetWithProfileAsync(expertUserId, cancellationToken)
            ?? throw new DomainException(ErrorKeys.ExpertNotFound);

        if (expert.Status != ExpertStatuses.Approved)
        {
            throw new DomainException(ErrorKeys.OfferExpertNotApproved);
        }

        var listing = await unitOfWork.Listings.GetByIdAsync(request.ListingId, cancellationToken)
            ?? throw new DomainException(ErrorKeys.ListingNotFound);

        if (listing.Status != ListingStatuses.Open)
        {
            throw new DomainException(ErrorKeys.OfferListingNotOpen);
        }

        if (await unitOfWork.Offers.ExistsForListingAndExpertAsync(request.ListingId, expertUserId, cancellationToken))
        {
            throw new DomainException(ErrorKeys.OfferAlreadyExists);
        }

        var offer = new Offer
        {
            ListingId = request.ListingId,
            ExpertId = expertUserId,
            Title = request.Title,
            Price = request.Price,
            Description = request.Description ?? string.Empty,
            SessionType = request.SessionType,
        };

        await unitOfWork.Offers.AddAsync(offer, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await unitOfWork.Offers.IncrementListingOfferCountAsync(request.ListingId, cancellationToken);

        return await GetByIdAsync(offer.Id, expertUserId, cancellationToken);
    }

    public async Task<OfferListResult> ListMyAsync(Guid expertUserId, string? status, CancellationToken cancellationToken)
    {
        var query = unitOfWork.Offers.QueryWithDetails().Where(offer => offer.ExpertId == expertUserId);
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(offer => offer.Status == status);
        }

        var offers = await query.OrderByDescending(offer => offer.CreatedAt).ToListAsync(cancellationToken);
        var rating = await unitOfWork.Reviews.GetRatingAsync(expertUserId, cancellationToken);
        var dtos = offers.Select(offer => OfferMapper.ToOfferDto(offer, viewerIsListingOwner: false, rating)).ToList();

        var pendingCount = await unitOfWork.Offers.QueryWithDetails()
            .CountAsync(offer => offer.ExpertId == expertUserId && offer.Status == OfferStatuses.Pending, cancellationToken);

        return new OfferListResult(dtos, dtos.Count, pendingCount);
    }

    public async Task<OfferListResult> ListForListingAsync(Guid listingId, Guid viewerUserId, CancellationToken cancellationToken)
    {
        var listing = await unitOfWork.Listings.GetByIdAsync(listingId, cancellationToken)
            ?? throw new DomainException(ErrorKeys.ListingNotFound);

        if (listing.ClientId != viewerUserId)
        {
            throw new DomainException(ErrorKeys.ListingNotFound);
        }

        var offers = await unitOfWork.Offers.QueryWithDetails()
            .Where(offer => offer.ListingId == listingId)
            .OrderByDescending(offer => offer.CreatedAt)
            .ToListAsync(cancellationToken);

        var dtos = new List<OfferDto>(offers.Count);
        foreach (var offer in offers)
        {
            var rating = await unitOfWork.Reviews.GetRatingAsync(offer.ExpertId, cancellationToken);
            dtos.Add(OfferMapper.ToOfferDto(offer, viewerIsListingOwner: true, rating));
        }

        return new OfferListResult(dtos, dtos.Count, PendingCount: null);
    }

    public async Task<OfferDto> GetByIdAsync(Guid offerId, Guid viewerUserId, CancellationToken cancellationToken)
    {
        var offer = await unitOfWork.Offers.GetWithListingAndExpertAsync(offerId, cancellationToken)
            ?? throw new DomainException(ErrorKeys.OfferNotFound);

        var listing = offer.Listing ?? throw new DomainException(ErrorKeys.ListingNotFound);
        var isListingOwner = listing.ClientId == viewerUserId;
        if (!isListingOwner && offer.ExpertId != viewerUserId)
        {
            throw new DomainException(ErrorKeys.OfferNotFound);
        }

        var rating = await unitOfWork.Reviews.GetRatingAsync(offer.ExpertId, cancellationToken);
        return OfferMapper.ToOfferDto(offer, isListingOwner, rating);
    }

    public async Task<OfferDto> AcceptAsync(Guid clientUserId, Guid offerId, CancellationToken cancellationToken)
    {
        try
        {
            var matchId = await unitOfWork.Matches.AcceptOfferAsync(offerId, clientUserId, cancellationToken);
            if (matchId is null)
            {
                throw new DomainException(ErrorKeys.OfferNotFound);
            }
        }
        catch (PostgresException exception) when (exception.MessageText == "OFFER_NOT_FOUND")
        {
            throw new DomainException(ErrorKeys.OfferNotFound);
        }
        catch (PostgresException exception) when (exception.MessageText == "LISTING_NOT_FOUND")
        {
            throw new DomainException(ErrorKeys.ListingNotFound);
        }
        catch (PostgresException exception) when (exception.MessageText == "OFFER_NOT_PENDING")
        {
            throw new DomainException(ErrorKeys.OfferNotPending);
        }
        catch (PostgresException exception) when (exception.MessageText == "LISTING_NOT_OPEN")
        {
            throw new DomainException(ErrorKeys.OfferListingNotOpen);
        }

        return await GetByIdAsync(offerId, clientUserId, cancellationToken);
    }

    public async Task<OfferDto> RejectAsync(Guid clientUserId, Guid offerId, CancellationToken cancellationToken)
    {
        var offer = await unitOfWork.Offers.GetWithListingAndExpertAsync(offerId, cancellationToken)
            ?? throw new DomainException(ErrorKeys.OfferNotFound);

        if (offer.Listing?.ClientId != clientUserId)
        {
            throw new DomainException(ErrorKeys.OfferNotFound);
        }

        if (offer.Status != OfferStatuses.Pending)
        {
            throw new DomainException(ErrorKeys.OfferNotPending);
        }

        offer.Status = OfferStatuses.Rejected;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var rating = await unitOfWork.Reviews.GetRatingAsync(offer.ExpertId, cancellationToken);
        return OfferMapper.ToOfferDto(offer, viewerIsListingOwner: true, rating);
    }

    public async Task<OfferDto> WithdrawAsync(Guid expertUserId, Guid offerId, CancellationToken cancellationToken)
    {
        var offer = await unitOfWork.Offers.GetWithListingAndExpertAsync(offerId, cancellationToken)
            ?? throw new DomainException(ErrorKeys.OfferNotFound);

        if (offer.ExpertId != expertUserId)
        {
            throw new DomainException(ErrorKeys.OfferNotFound);
        }

        if (offer.Status != OfferStatuses.Pending)
        {
            throw new DomainException(ErrorKeys.OfferNotPending);
        }

        offer.Status = OfferStatuses.Withdrawn;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var rating = await unitOfWork.Reviews.GetRatingAsync(offer.ExpertId, cancellationToken);
        return OfferMapper.ToOfferDto(offer, viewerIsListingOwner: false, rating);
    }
}
