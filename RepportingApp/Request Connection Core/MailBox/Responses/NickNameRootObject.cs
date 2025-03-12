namespace RepportingApp.Request_Connection_Core.Reporting.Responses;

using Newtonsoft.Json.Linq;

public class NickNameRootObject
{
    public NickNameResult result { get; set; }
    public object error { get; set; }
}

public class NickNameResult
{
    public NickNameResponses[] responses { get; set; }
    public NickNameStatus status { get; set; }
}

public class NickNameResponses
{
    public string id { get; set; }
    public NickNameHeaders[] headers { get; set; }
    public NickNameResponse response { get; set; }
    public int httpCode { get; set; }
}

public class NickNameHeaders
{
    public string key { get; set; }
    public string value { get; set; }
}

public class NickNameResponse
{
    public NickNameResult1 result { get; set; }
}

public class NickNameResult1
{
    public string id { get; set; }
    public NickNameLink link { get; set; }
    public JToken value { get; set; } 
}
public class NickNamevalue
{
    public string deaPrefix { get; set; }
    public string deaDomain { get; set; }
}

public class NickNameLink
{
    public string type { get; set; }
    public string href { get; set; }
}

public class NickNameStatus
{
    public object[] failedRequests { get; set; }
    public NickNameSuccessRequests[] successRequests { get; set; }
}

public class NickNameSuccessRequests
{
    public string id { get; set; }
    public string httpCode { get; set; }
    public string latency { get; set; }
    public bool suppressResponse { get; set; }
    public bool deserialized { get; set; }
}

