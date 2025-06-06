namespace DrHan.Domain.Exceptions
{
    public class ForbiddenException : Exception
    {
        // ==================================
        // === Constructors
        // ==================================

        public ForbiddenException()
            : base("You do not have permission to access this resource.")
        {
        }
    }
}
