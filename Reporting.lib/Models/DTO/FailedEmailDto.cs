namespace Reporting.lib.Models.DTO;

public class FailedEmailDto
{
    public int EmailId { get; set; }
    public string FailureReason { get; set; }
}
public class RetrieveFailedEmailDto
{
    public int Id { get; set; }
    public string EmailAddress { get; set; }
    public string Password { get; set; }
    public string ProxyIp { get; set; }
    
    public int Port { get; set; }
    
    public string Username { get; set; }
    
    public string ProxyPassword { get; set; }
}
public class EmailMetadata
{
    public int EmailAccountId { get; set; } // Foreign key to EmailAccount table
    public string MailId { get; set; }
    public string YmreqId { get; set; }
    public string Wssid { get; set; }
    public string Cookie { get; set; }
}

