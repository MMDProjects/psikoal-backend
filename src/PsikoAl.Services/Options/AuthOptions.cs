namespace PsikoAl.Services.Options;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    // Supabase Auth > URL Configuration > Redirect URLs listesinde de bulunmalı;
    // yoksa GoTrue redirect_to'yu yok sayıp site_url'e döner.
    public string PasswordResetRedirectUrl { get; set; } = "psikoal://reset-password";
}
