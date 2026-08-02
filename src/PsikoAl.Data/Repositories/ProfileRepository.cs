using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories.Abstractions;

namespace PsikoAl.Data.Repositories;

public sealed class ProfileRepository(AppDbContext dbContext)
    : Repository<Profile, Guid>(dbContext), IProfileRepository;
