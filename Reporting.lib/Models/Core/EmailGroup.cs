namespace Reporting.lib.Models.Core;

public class EmailGroup
{
    public int? GroupId { get; set; } = null;
    public string GroupName { get; set; } = null;
    //public ICollection<EmailAccount> EmailAccounts { get; set; } = new List<EmailAccount>();
    public override string ToString()
    {
        return GroupName;
    }
}