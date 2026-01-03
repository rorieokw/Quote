namespace Quote.Domain.Enums;

public enum VerificationLevel
{
    None = 0,
    Basic = 1,        // License only
    Verified = 2,     // License + Insurance
    Premium = 3       // License + Insurance + Police Check
}
