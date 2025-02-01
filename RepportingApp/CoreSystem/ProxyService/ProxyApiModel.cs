public class PeroxyApiRootObject
{
    public int count { get; set; }
    public string next { get; set; }
    public string previous { get; set; }
    public ProxyApiModelResults[] results { get; set; }
    public string? detail { get; set; }
}

public class ProxyApiModelResults
{
    public int id { get; set; }
    public string reason { get; set; }
    public string proxy { get; set; }
    public int proxy_port { get; set; }
    public string proxy_country_code { get; set; }
    public string replaced_with { get; set; }
    public int replaced_with_port { get; set; }
    public string replaced_with_country_code { get; set; }
    public string created_at { get; set; }
}
