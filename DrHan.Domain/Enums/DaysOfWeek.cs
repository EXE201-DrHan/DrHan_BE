namespace DrHan.Domain.Enums;

[Flags]
public enum DaysOfWeek
{
    None = 0,
    Monday = 1,
    Tuesday = 2,
    Wednesday = 4,
    Thursday = 8,
    Friday = 16,
    Saturday = 32,
    Sunday = 64,
    Weekdays = Monday | Tuesday | Wednesday | Thursday | Friday,
    Weekends = Saturday | Sunday,
    All = Monday | Tuesday | Wednesday | Thursday | Friday | Saturday | Sunday
}
public class Person
{
    public Person()
    {
    }

    public Person(int id, string name, string description, string firstName)
    {
        Id = id;
        Name = name;
        Description = description;
        FirstName = firstName;
    }

    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string FirstName { get; set; }
}
public record Person1 //immutable Java String nó là immutable String tri = "12312412414"; tri = "2eqadasd"
{
    public Person1()
    {
    }

    public Person1(int id, string name, string description, string firstName)
    {
        Id = id;
        Name = name;
        Description = description;
        FirstName = firstName;
    }

    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string FirstName { get; set; }
}
public class PersonHello
{
    Person tri1 = new Person() {Name = "13124124124" }; //12312412412 [asdasda]
    Person tri2 = new Person() { Name = tri1.Name };//1241212512512
    
}
    