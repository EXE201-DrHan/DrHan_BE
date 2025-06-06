namespace DrHan.Domain.Exceptions
{
    public class UnsupportedMediaFormatException : Exception
    {
        // ==================================
        // === Props & Fields
        // ==================================

        public string FileExtension { get; private set; }

        // ==================================
        // === Constructors
        // ==================================

        public UnsupportedMediaFormatException(string fileExtension)
            : base($"File format {fileExtension} is not supported.")
        {
            FileExtension = fileExtension;
        }
    }
}
