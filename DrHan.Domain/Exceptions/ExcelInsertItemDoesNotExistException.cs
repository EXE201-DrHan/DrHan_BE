namespace DrHan.Domain.Exceptions
{
    public class ExcelInsertItemDoesNotExistException(string entityName) : Exception($"{entityName} does not exist!")
    {
    }
}