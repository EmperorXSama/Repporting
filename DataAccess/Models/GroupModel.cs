namespace DataAccess.Models;

public class GroupModel
{
    public int Id { get; set; }
    public string GroupName { get; set; }
    public List<EmailsCoreModel> Emails { get; set; } // One-to-Many relationship
}