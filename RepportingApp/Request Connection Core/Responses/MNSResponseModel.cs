namespace RepportingApp.Request_Connection_Core.Responses;

public class MNSRootObject
{
    public MNSResult result { get; set; }
    public object error { get; set; }
}

public class MNSResult
{
    public MNSResponses[] responses { get; set; }
    public MNSStatus status { get; set; }
}

public class MNSResponses
{
    public string id { get; set; }
    public MNSHeaders[] headers { get; set; }
    public int httpCode { get; set; }
    public string responseString { get; set; }
}

public class MNSHeaders
{
    public string key { get; set; }
    public string value { get; set; }
}

public class MNSStatus
{
    public object[] failedRequests { get; set; }
    public MNSSuccessRequests[] successRequests { get; set; }
}

public class MNSSuccessRequests
{
    public string id { get; set; }
    public int httpCode { get; set; }
    public double latency { get; set; }
    public bool suppressResponse { get; set; }
    public bool deserialized { get; set; }
}

