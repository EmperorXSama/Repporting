namespace Reporting.lib.Models.DTO;

public class NetworkLogDto
{
    public int Id { get; set; }
    public int EmailId { get; set; }
    public string NickName { get; set; }
    public int MailboxesCount { get; set; }
}
public class MailBoxDto
{
    public int Id { get; set; }  // Primary key
    public int EmailId { get; set; }  // Foreign key linking to EmailAccount
    public string MailboxEmail { get; set; }
    public string IdDelete { get; set; }
    public string CostumeName { get; set; }
}
