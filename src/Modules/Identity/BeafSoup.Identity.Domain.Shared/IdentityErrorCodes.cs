namespace BeafSoup.Identity;

public static class IdentityErrorCodes
{
    public const string InvalidEmail = "Identity:InvalidEmail";
    public const string WeakPassword = "Identity:WeakPassword";
    public const string DuplicateIdentityLink = "Identity:DuplicateIdentityLink";
    public const string AccountDisabled = "Identity:AccountDisabled";
    public const string InvalidCredentials = "Identity:InvalidCredentials";
    public const string SessionNotFound = "Identity:SessionNotFound";
    public const string SessionEnded = "Identity:SessionEnded";
    public const string InvalidProvider = "Identity:InvalidProvider";
}
