namespace RepportingApp.Models.RequestModels;

public class ReturnTypeObject
{
    public string Message { get; set; }
    public object ReturnedValue { get; set; }
}

public class AliasesResult
{
    public List<string> Aliases { get; set; } = new();
    public int MailboxCount { get; set; }
}
