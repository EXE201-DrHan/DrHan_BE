namespace DrHan.Domain.Exceptions
{
    public class NotFoundException : Exception
    {
        // ==================================
        // === Props & Fields
        // ==================================

        public string ResourceType { get; private set; }
        public string ResourceIdentifier { get; private set; }

        // ==================================
        // === Constructor
        // ==================================

        public NotFoundException(string resourceType, string resourceIdentifier)
            : base($"{resourceType} with id: {resourceIdentifier} does not exists")
        {
            ResourceIdentifier = resourceIdentifier;
            ResourceType = resourceType;
        }

        public NotFoundException(string message)
            : base(message)
        {
        }
    }
}
