namespace Reporting.lib.Models.Core;

public class EmailGroup
{
    public int Id { get; set; }
    public string Name { get; set; }
    public ICollection<EmailAccount> EmailAccounts { get; set; } = new List<EmailAccount>();
}