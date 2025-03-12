namespace RepportingApp.Request_Connection_Core.Reporting.Responses;

public class NickNameValidateRootObject
{
    public NickNameValidateResult result { get; set; }
    public object error { get; set; }
}

public class NickNameValidateResult
{
    public NickNameValidateResponses[] responses { get; set; }
    public NickNameValidateStatus status { get; set; }
}

public class NickNameValidateResponses
{
    public string id { get; set; }
    public NickNameValidateHeaders[] headers { get; set; }
    public NickNameValidateResponse response { get; set; }
    public int httpCode { get; set; }
}

public class NickNameValidateHeaders
{
    public string key { get; set; }
    public string value { get; set; }
}

public class NickNameValidateResponse
{
    public NickNameValidateError error { get; set; }
}

public class NickNameValidateError
{
    public string code { get; set; }
    public string requestId { get; set; }
}

public class NickNameValidateStatus
{
    public NickNameValidateFailedRequests[] failedRequests { get; set; }
    public object[] successRequests { get; set; }
}

public class NickNameValidateFailedRequests
{
    public string id { get; set; }
    public int httpCode { get; set; }
    public double latency { get; set; }
    public bool suppressResponse { get; set; }
    public bool deserialized { get; set; }
}

