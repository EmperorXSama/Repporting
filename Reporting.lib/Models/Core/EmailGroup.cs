namespace Reporting.lib.Models.Core;

public class EmailGroup
{
    public int? Id { get; set; } = null;
    public string Name { get; set; } = null;
    //public ICollection<EmailAccount> EmailAccounts { get; set; } = new List<EmailAccount>();
    public override string ToString()
    {
        return Name;
    }
}