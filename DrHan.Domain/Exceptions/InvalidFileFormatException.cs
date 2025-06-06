namespace DrHan.Domain.Exceptions
{
    public class InvalidFileFormatException : Exception
    {
        public InvalidFileFormatException(string message) : base(message)
        {
        }
    }
}
