namespace DrHan.Domain.Exceptions
{
    public class AuthorizationFailedException : Exception
    {
        // ==================================
        // === Props & Fields
        // ==================================

        public string UserName { get; private set; }

        // ==================================
        // === Constructors
        // ==================================

        public AuthorizationFailedException(string userName)
            : base($"Authorization failed for user: {userName}. You do not have permission to perform this action.")
        {
            UserName = userName;
        }
    }
}
