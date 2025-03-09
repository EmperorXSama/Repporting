namespace Reporting.lib.Models.Core;

public class EmailGroup
{
    public int? GroupId { get; set; }
    public string GroupName { get; set; }
    public string RdpIp { get; set; }
    public int EmailCount { get; set; }  

    public override string ToString()
    {
        return $"{GroupName} - {RdpIp} - {EmailCount}";
    }
}
