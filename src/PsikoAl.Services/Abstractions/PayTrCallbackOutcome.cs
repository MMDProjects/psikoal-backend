namespace PsikoAl.Services.Abstractions;

public enum PayTrCallbackOutcome
{
    Paid,
    Failed,
    AlreadyProcessed,
    InvalidHash,
    UnknownOrder,
}
