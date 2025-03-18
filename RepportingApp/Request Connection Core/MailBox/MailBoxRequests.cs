using System.Net.Sockets;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Polly;
using Polly.Retry;
using Reporting.lib.Models.DTO;
using Reporting.lib.Models.Secondary;
using RepportingApp.CoreSystem.ProxyService;
using RepportingApp.Request_Connection_Core.Reporting.Responses;

namespace RepportingApp.Request_Connection_Core.MailBox;

public interface IMailBoxRequests
{
    Task<ReturnTypeObject> PrepareMailBoxNickName(EmailAccount emailAccount);
    Task<ReturnTypeObject> ProcessCollectAlias(EmailAccount emailAccount);

    Task<ReturnTypeObject> CreateAliaseManagerInitializer(EmailAccount emailAccount, int count, string nickname,
        string customName);
}

public class MailBoxRequests : IMailBoxRequests
{
      private readonly IApiConnector _apiConnector;
      private readonly ProxyListManager proxyListManager;
    
    
    
        public MailBoxRequests(IApiConnector apiConnector)
        {
            _apiConnector = apiConnector;
           proxyListManager = new ProxyListManager();
        }
    
    public async Task<ReturnTypeObject> PrepareMailBoxNickName(EmailAccount emailAccount)
    {
        CheckEmailMetaData(emailAccount);
        var result = await ProcessPrepareMailBoxNickName(emailAccount);
       return result;
    }
 

    public async Task<ReturnTypeObject> ProcessCollectAlias(EmailAccount emailAccount)
    {
        CheckEmailMetaData(emailAccount);
        var result = await ProcessCollectMailboxAliases(emailAccount);
        return result;
    }

public async  Task<ReturnTypeObject> ProcessPrepareMailBoxNickName(EmailAccount emailAccount)
{
       
    var nickName = await GetMailBoxNickName(emailAccount);
    

    return new ReturnTypeObject
    {
        ReturnedValue = nickName,
        Message =
            $"the nickname '{nickName}' has been created/retrieved in/from {emailAccount.EmailAddress} "
    };
}

public async  Task<ReturnTypeObject> ProcessCollectMailboxAliases(EmailAccount emailAccount)
{
       
    var aliases = await GetAliases(emailAccount);
    // split aliases and return list of it 
    var result = ParseAliases(aliases,emailAccount.Id);
    return new ReturnTypeObject
    {
        ReturnedValue = result,
        Message =
            $"we have retrieved aliases from {emailAccount.EmailAddress} "
    };
}

#region Create Aliases


public async Task<ReturnTypeObject> CreateAliaseManagerInitializer(EmailAccount emailAccount,int count,string nickname,string customName)
{
    CheckEmailMetaData(emailAccount);
    var result = await CreateAliaseManager(emailAccount , count, nickname, customName);
    return result;
}
public async  Task<ReturnTypeObject> CreateAliaseManager(EmailAccount emailAccount,int count,string nickname,string customName)
{
    var aliases = string.Empty;
    for (int i = 0; i < count; i++)
    {
        await Task.Delay(1200);
         aliases =  await CreateAliases(emailAccount,nickname,customName);
    }
   
    return new ReturnTypeObject
    {
        ReturnedValue = aliases,
        Message =
            $"we have retrieved aliases from {emailAccount.EmailAddress} "
    };
}

private async Task<string> CreateAliases(EmailAccount emailAccount,string nickname,string customName)
{
    var headers = PopulateHeaders(emailAccount);

    // Define a retry policy for specific exceptions.
    var retryPolicy = GetRetryPolicy(emailAccount);
    
    return await retryPolicy.ExecuteAsync(async () =>
    {
        var alias = StringExtension.GetRandomString(13);
        // Generate endpoint and payload
        string getEndpoint = GenerateEndpoint(emailAccount, MailBoxEndpointType.Create);
        string getPayload = PayloadManager.CreateAliasePayload(emailAccount.MetaIds.MailId,nickname,alias,customName);

        // Call the API
        string response = await _apiConnector.PostDataAsync<string>(
            emailAccount,
            getEndpoint,
            getPayload,
            headers,
            emailAccount.Proxy
        );
        UsersCreateRootObject result = JsonConvert.DeserializeObject<UsersCreateRootObject>(response);
        return "done";
    });
}

#endregion



   
private async Task<string> GetMailBoxNickName(EmailAccount emailAccount)
{
    var headers = PopulateHeaders(emailAccount);

    // Define a retry policy for specific exceptions.
    var retryPolicy = Policy
        .Handle<SocketException>() // Catch socket errors
        .Or<HttpRequestException>() // Catch network-related errors
        .Or<Exception>(ex => ex.Message.Contains("Proxy error")) // Catch proxy-related errors
        .WaitAndRetryAsync(
            retryCount: 1, // Retry only once
            retryAttempt => TimeSpan.FromSeconds(1), // Wait 2 seconds before retry
            (exception, timeSpan, retryCount, context) =>
            {
                var reservedProxyInUse = proxyListManager.GetRandomDifferentSubnetProxyDb(emailAccount.Proxy);
                emailAccount.Proxy = reservedProxyInUse;
            }
        );


    return await retryPolicy.ExecuteAsync(async () =>
    {
        // Generate endpoint and payload
        string getEndpoint = GenerateEndpoint(emailAccount, MailBoxEndpointType.Prepare);
        string getPayload = PayloadManager.PrepareMailBoxPayload(emailAccount.MetaIds.MailId);

        // Call the API
        string response = await _apiConnector.PostDataAsync<object>(
            emailAccount,
            getEndpoint,
            getPayload,
            headers,
            emailAccount.Proxy
        );

        // Deserialize response and process messages
        NickNameRootObject? nicknameRoot = JsonConvert.DeserializeObject<NickNameRootObject>(response);
        string deaPrefix = null;
        if (nicknameRoot?.result.responses != null)
            foreach (var responses in nicknameRoot.result.responses)
            {
                var responseValue = responses.response.result.value;
                if (responses.id == "GetMailboxAttribute_disposableAddressesPrefix" &&
                    responseValue.Type == JTokenType.Object)
                {
                    var responseValueObj = (JObject)responseValue;
                    string deaDomain = null;
                    if (responseValueObj.ContainsKey("deaPrefix") && responseValueObj.ContainsKey("deaDomain"))
                    {
                        deaPrefix = responseValueObj.GetValue("deaPrefix").ToString();
                        break;
                    }
                }
            }

        if (string.IsNullOrWhiteSpace(deaPrefix))
        {
            var nickname = await MailBoxPrepareSetBaseName(emailAccount);
            deaPrefix = nickname;
        }
        

        return deaPrefix;
    });
}
private async Task<string> MailBoxPrepareSetBaseName(EmailAccount emailAccount)
{
    var headers = PopulateHeaders(emailAccount);

    // Define a retry policy for specific exceptions.
    var retryPolicy = Policy
        .Handle<SocketException>() // Catch socket errors
        .Or<HttpRequestException>() // Catch network-related errors
        .Or<Exception>(ex => ex.Message.Contains("Proxy error")) // Catch proxy-related errors
        .WaitAndRetryAsync(
            retryCount: 1, // Retry only once
            retryAttempt => TimeSpan.FromSeconds(1), // Wait 2 seconds before retry
            (exception, timeSpan, retryCount, context) =>
            {
                var reservedProxyInUse = proxyListManager.GetRandomDifferentSubnetProxyDb(emailAccount.Proxy);
                emailAccount.Proxy = reservedProxyInUse;
            }
        );


    return await retryPolicy.ExecuteAsync(async () =>
    {
        string endpoint = GenerateEndpoint(emailAccount, MailBoxEndpointType.Prepare);
        var final_result = string.Empty;
        bool finished = true;
        do
        {
            string randomName = StringExtension.GetRandomString(13);
            string payload = PayloadManager.PrepareMailBoxPayloadSetName(emailAccount.MetaIds.MailId,randomName);
            
            string response = await _apiConnector.PostDataAsync<string>(
                emailAccount,
                endpoint,
                payload,
                headers,
                emailAccount.Proxy
            );

            // Deserialize response and process messages
            NickNameValidateRootObject rootValue =
                JsonConvert.DeserializeObject<NickNameValidateRootObject>(response);
                
            if (rootValue.result.status.failedRequests.Length <= 0)
            {
                // nickName validated and we can progress
                finished = false;
                final_result = randomName;
            }
            
        } while (finished);
        
       
        return final_result;
    });
}



#region Aliases Collect

private async Task<string> GetAliases(EmailAccount emailAccount)
{
    var headers = PopulateHeaders(emailAccount);

    // Define a retry policy for specific exceptions.
    var retryPolicy = Policy
        .Handle<SocketException>() // Catch socket errors
        .Or<HttpRequestException>() // Catch network-related errors
        .Or<Exception>(ex => ex.Message.Contains("Proxy error")) // Catch proxy-related errors
        .WaitAndRetryAsync(
            retryCount: 1, // Retry only once
            retryAttempt => TimeSpan.FromSeconds(1), // Wait 2 seconds before retry
            (exception, timeSpan, retryCount, context) =>
            {
                var reservedProxyInUse = proxyListManager.GetRandomDifferentSubnetProxyDb(emailAccount.Proxy);
                emailAccount.Proxy = reservedProxyInUse;
            }
        );


    return await retryPolicy.ExecuteAsync(async () =>
    {
        // Generate endpoint and payload
        string getEndpoint = GenerateEndpoint(emailAccount, MailBoxEndpointType.Collect);
        string getPayload = PayloadManager.CollectMailBoxPayload(emailAccount.MetaIds.MailId);

        // Call the API
        string response = await _apiConnector.PostDataAsync<string>(
            emailAccount,
            getEndpoint,
            getPayload,
            headers,
            emailAccount.Proxy
        );
        string accountMailboxesInfo = String.Empty;
        SkiletonRootObject root = JsonConvert.DeserializeObject<SkiletonRootObject>(response);
        if (root?.result?.responses != null)
        {
            // Find the response with the "GetAccounts" id
            var getAccountsResponse = root.result.responses
                .FirstOrDefault(r => r.id == "GetAccounts");

            if (getAccountsResponse?.response?.result?.accounts != null)
            {
                var accounts = getAccountsResponse.response.result.accounts;

                // Iterate through all accounts to build the concatenated string
                foreach (var account in accounts)
                {
                    accountMailboxesInfo += $"{account.id}:{account.email}:{account.sendingName};";
                }

                // Example of using the first account's data for a custom string (if needed)
                if (accounts.Length > 1)
                {
                    SkiletonAccounts accountHolder = accounts[1]; 
                    accountMailboxesInfo += $"{accountHolder.email.Split('-')[0]};";
                }
            }
        }
        // Remove the trailing semicolon if present
        if (accountMailboxesInfo.EndsWith(";"))
        {
            accountMailboxesInfo = accountMailboxesInfo.TrimEnd(';');
        }
        return accountMailboxesInfo;
    });
}
private List<MailBoxDto> ParseAliases(string aliases,int id)
{
    var mailboxes = new List<MailBoxDto>();

    var parts = aliases.Split(';');
    foreach (var part in parts)
    {
        var match = Regex.Match(part, @"^(\d+):([^:]+):(.+)$");
        if (match.Success && match.Groups[1].Value != "1")
        {
            mailboxes.Add(new MailBoxDto
            {
                EmailId = id,
                IdDelete = match.Groups[1].Value,
                MailboxEmail = match.Groups[2].Value,
                CostumeName = match.Groups[3].Value
            });
        }
    }

    return mailboxes;
}

#endregion
    
    
    private Dictionary<string, string> PopulateHeaders(EmailAccount emailAccount)
    {
        return new Dictionary<string, string>
        {
            { "accept", "application/json" },
            { "cache-control", "no-cache" },
            { "pragma", "no-cache" },
            { "priority", "u=1, i" },
            { "referer", "https://mail.yahoo.com/" },
            { "sec-ch-ua-mobile", "?0" },
            { "sec-ch-ua-platform", "\"Windows\"" },
            { "sec-fetch-dest", "empty" },
            { "sec-fetch-mode", "cors" },
            { "sec-fetch-site", "same-origin" },
            { "user-agent", emailAccount.UserAgent },
            { "cookie", emailAccount.MetaIds.Cookie.Trim() }
        };
    }


    private string GenerateEndpoint(EmailAccount emailAccount, MailBoxEndpointType endpointType)
    {
        emailAccount.MetaIds.YmreqId = YmriqId.GetYmreqid(emailAccount.MetaIds.YmreqId, 2).LastOrDefault()!;
        string hash = RandomGenerator.GenerateRandomHexString(8);
        
        return endpointType switch
        {
            MailBoxEndpointType.Prepare =>
                $"https://mail.yahoo.com/ws/v3/batch?name=settings.get&hash={hash}&appId=YMailNorrin&ymreqid={emailAccount.MetaIds.YmreqId}&wssid={emailAccount.MetaIds.Wssid}",
            MailBoxEndpointType.SetBaseName =>
                $"https://mail.yahoo.com/ws/v3/batch?name=settings.setBaseNameJWS&hash={hash}&appId=YMailNorrin&ymreqid={emailAccount.MetaIds.YmreqId}&wssid={emailAccount.MetaIds.Wssid}",
            MailBoxEndpointType.Collect => 
                $"https://mail.yahoo.com/ws/v3/batch?name=launch.skeleton&hash={hash}&appId=YMailNorrin&ymreqid={emailAccount.MetaIds.YmreqId}&wssid={emailAccount.MetaIds.Wssid}",
            MailBoxEndpointType.Create =>
                $"https://mail.yahoo.com/ws/v3/batch?name=settings.addAccount&hash={hash}&appId=YMailNorrin&ymreqid={emailAccount.MetaIds.YmreqId}&wssid={emailAccount.MetaIds.Wssid}",
           
        };
    }

    private void  CheckEmailMetaData(EmailAccount emailAccount)
    {
        if (emailAccount.MetaIds == null)
        {
            throw new Exception($"[MetaDataError] Invalid email metadata (MetaIds is null). Please check your email metadata. (email ma3andoxi ids )");
        }
        var metadataProperties = new Dictionary<string, object>
        {
            { "YmreqId", emailAccount.MetaIds.YmreqId },
            { "Wssid", emailAccount.MetaIds.Wssid },
            { "MailId", emailAccount.MetaIds.MailId },
            { "Cookie", emailAccount.MetaIds.Cookie }
        };

        foreach (var property in metadataProperties)
        {
            if (property.Value == null)
            {
                throw new Exception($"[{property.Key} Error] Invalid email metadata ({property.Key} is null). Please check your email metadata.");
            }
        }
    }

    private AsyncRetryPolicy GetRetryPolicy(EmailAccount emailAccount)
    {
        return Policy
            .Handle<SocketException>() // Catch socket errors
            .Or<HttpRequestException>() // Catch network-related errors
            .Or<Exception>(ex => ex.Message.Contains("Proxy error")) // Catch proxy-related errors
            .WaitAndRetryAsync(
                retryCount: 1, // Retry only once
                retryAttempt => TimeSpan.FromSeconds(1), // Wait 2 seconds before retry
                (exception, timeSpan, retryCount, context) =>
                {
                    var reservedProxyInUse = proxyListManager.GetRandomDifferentSubnetProxyDb(emailAccount.Proxy);
                    emailAccount.Proxy = reservedProxyInUse;
                }
            );
    }
}
public enum MailBoxEndpointType
{
    Create,
    Prepare,
    SetBaseName,
    Collect
}