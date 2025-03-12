namespace RepportingApp.Request_Connection_Core.Reporting.Responses;

public class UsersCreateRootObject
{
    public UsersCreateResult result { get; set; }
    public object error { get; set; }
}

public class UsersCreateResult
{
    public UsersCreateResponses[] responses { get; set; }
    public UsersCreateStatus status { get; set; }
}

public class UsersCreateResponses
{
    public string id { get; set; }
    public UsersCreateHeaders[] headers { get; set; }
    public UsersCreateResponse response { get; set; }
    public int httpCode { get; set; }
}

public class UsersCreateHeaders
{
    public string key { get; set; }
    public string value { get; set; }
}

public class UsersCreateResponse
{
    public UsersCreateResult1 result { get; set; }
}

public class UsersCreateResult1
{
    public string id { get; set; }
    public int priority { get; set; }
    public string email { get; set; }
    public int createTime { get; set; }
    public string authType { get; set; }
    public bool isPrimary { get; set; }
    public string sendingName { get; set; }
    public bool accountVerified { get; set; }
    public string status { get; set; }
    public bool isSending { get; set; }
    public bool isSelected { get; set; }
    public string subscriptionId { get; set; }
    public string highestModSeq { get; set; }
    public string type { get; set; }
    public UsersCreateAccounts[] accounts { get; set; }
}

public class UsersCreateLink
{
    public string type { get; set; }
    public string href { get; set; }
}

public class UsersCreateAccounts
{
    public string id { get; set; }
    public int priority { get; set; }
    public string email { get; set; }
    public int createTime { get; set; }
    public string authType { get; set; }
    public UsersCreateLink1 link { get; set; }
    public bool isPrimary { get; set; }
    public string sendingName { get; set; }
    public bool accountVerified { get; set; }
    public string status { get; set; }
    public bool isSending { get; set; }
    public bool isSelected { get; set; }
    public string checksum { get; set; }
    public string subscriptionId { get; set; }
    public string highestModSeq { get; set; }
    public string type { get; set; }
    public string[] linkedAccounts { get; set; }
}

public class UsersCreateLink1
{
    public string type { get; set; }
    public string href { get; set; }
}

public class UsersCreateStatus
{
    public object[] failedRequests { get; set; }
    public UsersCreateSuccessRequests[] successRequests { get; set; }
    public UsersCreateFailedRequests[] fuccessRequests { get; set; }
}

public class UsersCreateSuccessRequests
{
    public string id { get; set; }
    public int httpCode { get; set; }
    public double latency { get; set; }
    public bool suppressResponse { get; set; }
    public bool deserialized { get; set; }
}
public class UsersCreateFailedRequests
{
    public string id { get; set; }
    public int httpCode { get; set; }
    public double latency { get; set; }
    public bool suppressResponse { get; set; }
    public bool deserialized { get; set; }
}

