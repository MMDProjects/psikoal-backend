namespace PsikoAl.Data.Entities;

/// Keyless entity: public.expert_ratings view'ına eşlenir (yalnızca approved yorumlardan).
public sealed class ExpertRating
{
    public Guid ExpertId { get; set; }

    public double Rating { get; set; }

    public int ReviewCount { get; set; }
}
