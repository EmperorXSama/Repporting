namespace Reporting.lib.Models.DTO;

public class ActivateMailboxPackRequest
{
    public int PackNumber { get; set; }
    public List<string> EmailAddresses { get; set; } = new();
}