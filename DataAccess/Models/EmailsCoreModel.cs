using DataAccess.Enums;

namespace DataAccess.Models;

public class EmailsCoreModel
{
    public int Id { get; set; }
    public string EmailAddress { get; set; }
    public string Password { get; set; }
    public int GroupId { get; set; } // Foreign key to Group
    public GroupModel Group { get; set; } // Navigation property
    public string MailBox { get; set; }
    public string Proxy { get; set; }
    public string Port { get; set; }
    public int NumSpam { get; set; }
    public Status Status { get; set; }
    
}


