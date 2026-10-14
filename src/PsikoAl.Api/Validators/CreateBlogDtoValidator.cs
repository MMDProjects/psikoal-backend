using FluentValidation;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Blog.Create;
using PsikoAl.Common.Text;

namespace PsikoAl.Api.Validators;

public sealed class CreateBlogDtoValidator : AbstractValidator<CreateBlogDto>
{
    public CreateBlogDtoValidator()
    {
        RuleFor(request => request.Title).NotEmpty().MaximumLength(BlogFieldLimits.Title).WithMessage(ErrorKeys.ValidationFailed);
        RuleFor(request => request.Slug)
            .Must(slug => string.IsNullOrWhiteSpace(slug) || SlugGenerator.IsValid(slug.Trim()))
            .WithMessage(ErrorKeys.BlogSlugInvalid);
        RuleFor(request => request.Excerpt).NotNull().MaximumLength(BlogFieldLimits.Excerpt).WithMessage(ErrorKeys.ValidationFailed);
        RuleFor(request => request.Content).NotNull().MaximumLength(BlogFieldLimits.Content).WithMessage(ErrorKeys.ValidationFailed);
        RuleFor(request => request.Tags)
            .NotNull()
            .Must(tags => tags.Count <= BlogTagNormalizer.MaxTagCount)
            .WithMessage(ErrorKeys.ValidationFailed);
        RuleFor(request => request.AuthorName).NotEmpty().MaximumLength(BlogFieldLimits.AuthorName).WithMessage(ErrorKeys.ValidationFailed);
        RuleFor(request => request.AuthorTitle).NotNull().MaximumLength(BlogFieldLimits.AuthorTitle).WithMessage(ErrorKeys.ValidationFailed);
    }
}
