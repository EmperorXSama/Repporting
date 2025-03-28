namespace RepportingApp.Request_Connection_Core.Reporting.Responses;

public class UsersDeleteRootObject
{
    public UsersDeleteResult result { get; set; }
    public object error { get; set; }
}

public class UsersDeleteResult
{
    public UsersDeleteResponses[] responses { get; set; }
    public UsersDeleteStatus status { get; set; }
}

public class UsersDeleteResponses
{
    public string id { get; set; }
    public UsersDeleteHeaders[] headers { get; set; }
    public UsersDeleteResponse response { get; set; }
    public int httpCode { get; set; }
}

public class UsersDeleteHeaders
{
    public string key { get; set; }
    public string value { get; set; }
}

public class UsersDeleteResponse
{
    public UsersDeleteResult1 result { get; set; }
}

public class UsersDeleteResult1
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
    public UsersDeleteAccounts[] accounts { get; set; }
}

public class UsersDeleteAccounts
{
    public string id { get; set; }
    public int priority { get; set; }
    public string email { get; set; }
    public int createTime { get; set; }
    public string authType { get; set; }
    public UsersDeleteLink link { get; set; }
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

public class UsersDeleteLink
{
    public string type { get; set; }
    public string href { get; set; }
}

public class UsersDeleteStatus
{
    public object[] failedRequests { get; set; }
    public UsersDeleteSuccessRequests[] successRequests { get; set; }
    public UsersDeleteFailRequests[] failRequests { get; set; }
}

public class UsersDeleteSuccessRequests
{
    public string id { get; set; }
    public int httpCode { get; set; }
    public double latency { get; set; }
    public bool suppressResponse { get; set; }
    public bool deserialized { get; set; }
}
public class UsersDeleteFailRequests
{
    public string id { get; set; }
    public int httpCode { get; set; }
    public double latency { get; set; }
    public bool suppressResponse { get; set; }
    public bool deserialized { get; set; }
}

