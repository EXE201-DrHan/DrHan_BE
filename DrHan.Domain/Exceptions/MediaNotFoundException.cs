namespace DrHan.Domain.Exceptions
{
    public class MediaNotFoundException : Exception
    {
        // ==================================
        // === Constructors
        // ==================================

        public MediaNotFoundException() : base("The selected media format is not supported.")
        {
        }
    }
}
