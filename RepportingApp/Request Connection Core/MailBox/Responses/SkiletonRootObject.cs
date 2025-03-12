namespace RepportingApp.Request_Connection_Core.Reporting.Responses;

public class SkiletonRootObject
{
    public SkiletonResult result { get; set; }
    public object error { get; set; }
}

public class SkiletonResult
{
    public SkiletonResponses[] responses { get; set; }
    public SkiletonStatus status { get; set; }
}

public class SkiletonResponses
{
    public string id { get; set; }
    public SkiletonHeaders[] headers { get; set; }
    public SkiletonResponse response { get; set; }
    public int httpCode { get; set; }
}

public class SkiletonHeaders
{
    public string key { get; set; }
    public string value { get; set; }
}

public class SkiletonResponse
{
    public SkiletonResult1 result { get; set; }
}

public class SkiletonResult1
{
    public string id { get; set; }
    public SkiletonLink link { get; set; }
    public SkiletonValue value { get; set; }
    public SkiletonAccounts[] accounts { get; set; }
    public SkiletonFolders[] folders { get; set; }
}

public class SkiletonLink
{
    public string type { get; set; }
    public string href { get; set; }
}

public class SkiletonValue
{
    public int web_markAsReadInterval { get; set; }
    public long testAndSet { get; set; }
    public bool mutable { get; set; }
    public bool web_useRichText { get; set; }
    public string web_composeFontSize { get; set; }
    public bool web_breakingNews { get; set; }
    public int web_hideComposeToolbar_gdpr { get; set; }
    public int web_msgsListDensity { get; set; }
    public bool web_hideAd { get; set; }
    public bool web_mailTabs { get; set; }
    public bool web_showCustomFolders { get; set; }
    public object[] web_emojiRecentlyUsed { get; set; }
    public bool web_hulk_readabilityMode { get; set; }
    public bool web_showSmartViews { get; set; }
    public object[] web_systemTabs { get; set; }
    public bool web_enableEnhancerLinkPreview { get; set; }
    public bool web_generalSnippet { get; set; }
    public int web_desktopNotification { get; set; }
    public int web_actionAfterMsgMove { get; set; }
    public bool isShipmentTrackingEnabled { get; set; }
    public int web_mailPreviewPane { get; set; }
    public string web_stationeryTheme { get; set; }
    public string web_composeFontFamily { get; set; }
    public int web_adBlockFeatureCueId { get; set; }
    public object[] web_expandedFolders { get; set; }
    public bool ampEmail { get; set; }
    public int dealsStories { get; set; }
    public int web_composeMode { get; set; }
}

public class SkiletonAccounts
{
    public string id { get; set; }
    public int priority { get; set; }
    public string email { get; set; }
    public int createTime { get; set; }
    public string authType { get; set; }
    public string partnerCode { get; set; }
    public SkiletonLink1 link { get; set; }
    public bool isPrimary { get; set; }
    public string sendingName { get; set; }
    public bool accountVerified { get; set; }
    public string status { get; set; }
    public bool isSending { get; set; }
    public bool isSelected { get; set; }
    public string checksum { get; set; }
    public string subscriptionId { get; set; }
    public long  highestModSeq { get; set; }
    public string type { get; set; }
    public string[] linkedAccounts { get; set; }
}

public class SkiletonLink1
{
    public string type { get; set; }
    public string href { get; set; }
}

public class SkiletonFolders
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
    public SkiletonLink2 link { get; set; }
    public object[] bidi { get; set; }
    public string oldV2Fid { get; set; }
}

public class SkiletonLink2
{
    public string type { get; set; }
    public string href { get; set; }
}

public class SkiletonStatus
{
    public object[] failedRequests { get; set; }
    public SkiletonSuccessRequests[] successRequests { get; set; }
}

public class SkiletonSuccessRequests
{
    public string id { get; set; }
    public int httpCode { get; set; }
    public double latency { get; set; }
    public bool suppressResponse { get; set; }
    public bool deserialized { get; set; }
}
