namespace RepportingApp.Models.RequestModels;

public class SyncRootObject
{
    public Result result { get; set; }
    public object error { get; set; }
}

public class Result
{
    public Responses[] responses { get; set; }
    public SyncStatus status { get; set; }
}

public class Responses
{
    public string id { get; set; }
    public Headers[] headers { get; set; }
    public Response response { get; set; }
    public int httpCode { get; set; }
}

public class Headers
{
    public string key { get; set; }
    public string value { get; set; }
}

public class Response
{
    public Result1 result { get; set; }
}

public class Result1
{
    public Folders[] folders { get; set; }
    public Accounts[] accounts { get; set; }
}

public class Folders
{
    public string id { get; set; }
    public string name { get; set; }
    public string[] types { get; set; }
    public int unread { get; set; }
    public int total { get; set; }
    public int size { get; set; }
    public int uidNext { get; set; }
    public int uidValidity { get; set; }
    public string acctId { get; set; }
    public int highestModSeq { get; set; }
    public Link link { get; set; }
    public object[] bidi { get; set; }
    public string oldV2Fid { get; set; }
}

public class Link
{
    public string type { get; set; }
    public string href { get; set; }
}

public class Accounts
{
    public string id { get; set; }
    public int highestModSeq { get; set; }
}

public class SyncStatus
{
    public object[] failedRequests { get; set; }
    public SuccessRequests[] successRequests { get; set; }
}

public class SuccessRequests
{
    public string id { get; set; }
    public int httpCode { get; set; }
    public double latency { get; set; }
    public bool suppressResponse { get; set; }
    public bool deserialized { get; set; }
}