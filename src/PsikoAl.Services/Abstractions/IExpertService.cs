using PsikoAl.Common.Dtos.Expert;
using PsikoAl.Common.Dtos.Expert.Create;
using PsikoAl.Common.Dtos.Expert.Update;

namespace PsikoAl.Services.Abstractions;

public interface IExpertService
{
    Task<ExpertDto> CreateProfileAsync(Guid userId, CreateExpertProfileDto request, CancellationToken cancellationToken);

    Task<ExpertDto> UpdateProfileAsync(Guid userId, UpdateExpertProfileDto request, CancellationToken cancellationToken);

    Task<ExpertDto> GetByIdAsync(Guid expertId, Guid? viewerUserId, CancellationToken cancellationToken);
}
