namespace RepportingApp.Request_Connection_Core.Responses;

public class InboxRootObject
{
    public InboxResult result { get; set; }
    public object error { get; set; }
}

public class InboxResult
{
    public InboxResponses[] responses { get; set; }
    public InboxStatus status { get; set; }
}

public class InboxResponses
{
    public string id { get; set; }
    public InboxHeaders[] headers { get; set; }
    public InboxResponse response { get; set; }
    public int httpCode { get; set; }
}

public class InboxHeaders
{
    public string key { get; set; }
    public string value { get; set; }
}

public class InboxResponse
{
    public InboxResult1 result { get; set; }
}

public class InboxResult1
{
    public string id { get; set; }
    public InboxCounts[] counts { get; set; }
    public ObservableCollection<InboxMessages> messages { get; set; }
    public InboxConversations[] conversations { get; set; }
    public InboxQuery query { get; set; }
}

public class InboxCounts
{
    public string accountId { get; set; }
    public int unseen { get; set; }
}

public class InboxMessages
{
    public InboxLink link { get; set; }
    public InboxFolder folder { get; set; }
    public InboxFlags flags { get; set; }
    public InboxHeaders1 headers { get; set; }
    public string id { get; set; }
    public string conversationId { get; set; }
    public string immutableid { get; set; }
    public string uid { get; set; }
    public string auid { get; set; }
    public string uidl { get; set; }
    public string size { get; set; }
    public int rfc822Size { get; set; }
    public string snippet { get; set; }
    public InboxFilterResult filterResult { get; set; }
    public InboxDecos[] decos { get; set; }
    public string deliveryId { get; set; }
    public object[] attachments { get; set; }
    public long dedupId { get; set; }
    public int attachmentCount { get; set; }
    public long modSeq { get; set; }
    public InboxEncryptionKey encryptionKey { get; set; }
    public InboxVerifiedDomains[] verifiedDomains { get; set; }
}

public class InboxLink
{
    public string type { get; set; }
    public string href { get; set; }
}

public class InboxFolder
{
    public string id { get; set; }
    public string name { get; set; }
    public string[] types { get; set; }
    public int unread { get; set; }
    public int total { get; set; }
    public string size { get; set; }
    public string uidNext { get; set; }
    public string uidValidity { get; set; }
    public string acctId { get; set; }
    public int highestModSeq { get; set; }
    public InboxLink1 link { get; set; }
    public object[] bidi { get; set; }
    public string oldV2Fid { get; set; }
}

public class InboxLink1
{
    public string type { get; set; }
    public string href { get; set; }
}

public class InboxFlags
{
    public bool ham { get; set; }
    public bool recent { get; set; }
    public bool read { get; set; }
}

public class InboxHeaders1
{
    public string subject { get; set; }
    public InboxFrom[] from { get; set; }
    public InboxTo[] to { get; set; }
    public InboxReplyTo[] replyTo { get; set; }
    public string date { get; set; }
    public string mimeDate { get; set; }
    public string internalDate { get; set; }
    public string messageIdRfc822 { get; set; }
    public string mimeType { get; set; }
}

public class InboxFrom
{
    public string name { get; set; }
    public string email { get; set; }
}

public class InboxTo
{
    public string email { get; set; }
}

public class InboxReplyTo
{
    public string email { get; set; }
    public string name { get; set; }
}

public class InboxFilterResult
{
    public int folderOfDelivery { get; set; }
}

public class InboxDecos
{
    public string id { get; set; }
    public string type { get; set; }
}

public class InboxEncryptionKey
{
    public string encryptionKey { get; set; }
    public string ckmsVersion { get; set; }
}

public class InboxSchemaOrg
{
    public InboxSchema schema { get; set; }
}

public class InboxSchema
{
    public string _type { get; set; }
    public string[] antispamStatus { get; set; }
    public InboxJediSchemaInfoNode jediSchemaInfoNode { get; set; }
    public string _id { get; set; }
    public string _context { get; set; }
    public InboxCategory[] category { get; set; }
}

public class InboxJediSchemaInfoNode
{
    public string _type { get; set; }
    public int v { get; set; }
    public string ownerId { get; set; }
    public string about { get; set; }
    public int id { get; set; }
}

public class InboxCategory
{
    public string version { get; set; }
    public string _id { get; set; }
    public string _type { get; set; }
    public InboxIdentifier[] identifier { get; set; }
    public InboxTypes[] types { get; set; }
    public InboxObjectives[] objectives { get; set; }
    public InboxMethods[] methods { get; set; }
}

public class InboxIdentifier
{
    public string _id { get; set; }
    public string _type { get; set; }
    public string propertyID { get; set; }
    public string value { get; set; }
}

public class InboxTypes
{
    public string _id { get; set; }
    public string _type { get; set; }
    public string name { get; set; }
}

public class InboxObjectives
{
    public string _id { get; set; }
    public string _type { get; set; }
    public string name { get; set; }
}

public class InboxMethods
{
    public string _id { get; set; }
    public string _type { get; set; }
    public string name { get; set; }
}

public class InboxVerifiedDomains
{
    public string domain { get; set; }
}

public class InboxConversations
{
    public string id { get; set; }
    public string[] messageIds { get; set; }
    public string[] folderIds { get; set; }
}

public class InboxQuery
{
    public string _base { get; set; }
    public string options { get; set; }
    public string type { get; set; }
    public string token { get; set; }
    public long nextModSeq { get; set; }
}

public class InboxStatus
{
    public object[] failedRequests { get; set; }
    public InboxSuccessRequests[] successRequests { get; set; }
}

public class InboxSuccessRequests
{
    public string id { get; set; }
    public int httpCode { get; set; }
    public double latency { get; set; }
    public bool suppressResponse { get; set; }
    public bool deserialized { get; set; }
}

