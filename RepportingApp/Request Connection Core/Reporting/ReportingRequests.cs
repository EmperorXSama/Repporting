using RepportingApp.Request_Connection_Core.Payload;

namespace RepportingApp.Request_Connection_Core.Reporting;

public class ReportingRequests : IReportingRequests
{
    private readonly IApiConnector _apiConnector;

    // reporting headers 
    private Dictionary<string, string> _headers = new Dictionary<string, string>();
    
    
    public ReportingRequests(IApiConnector apiConnector)
    {
        _apiConnector = apiConnector;
       
    }


    public async Task<bool> SendReportAsync(EmailAccount emailAccount,string messageId)
    {
        PopulateHeaders(emailAccount);
        emailAccount.MetaIds.YmreqId = YmriqId.GetYmreqid(emailAccount.MetaIds.YmreqId, 2).LastOrDefault()!;
        
        string endpoint = GenerateEndpoint(emailAccount);
        
        var batchPayload = new
        {
            requests = new[]
            {
                new
                {
                    id = "UnifiedUpdateMessage_0",
                    uri = $"/ws/v3/mailboxes/@.id=={emailAccount.MetaIds.MailId}/messages/@.select==q?q=id%3A({messageId})",
                    method = "POST",
                    payloadType = "embedded",
                    payload = new
                    {
                        message = new
                        {
                            flags = new { spam = false },
                            folder = new { id = "1" }
                        }
                    }
                }
            },
            responseType = "json"
        };

        // Serialize payload to JSON
        var payloadJson = JsonConvert.SerializeObject(batchPayload);

        // Send the request using the UnifiedApiClient
        try
        {
            var response = await _apiConnector.PostDataAsync<string>(
                endpoint,
                payloadJson,
                _headers,
                emailAccount.Proxy
            );

            return true; // Success if no exception is thrown
        }
        catch (Exception ex)
        {
            // Log or handle the error
            Console.WriteLine($"Error sending report: {ex.Message}");
            return false;
        }
    }
 public async Task<ObservableCollection<InboxMessages>> GetMessagesFromInboxFolder(EmailAccount emailAccount )
    {
        try
        {
           
            string endpoint = GenerateEndpoint(emailAccount);
            PopulateHeaders(emailAccount);
            string payload = PayloadManager.GetCorrectFolderPayload(emailAccount.MetaIds.MailId, Statics.InboxDir);
            string response = await _apiConnector.PostDataAsync<string>(
                endpoint,
                payload,
                _headers,
                emailAccount.Proxy
            );
        
            var jsonResponse = JsonConvert.DeserializeObject<InboxRootObject>(response);
            InboxResult res = jsonResponse.result;
            InboxResponses[] responses = res.responses;
            InboxResponse resp = responses[1].response;
            ObservableCollection<InboxMessages> result = new();
            if (resp.result.messages != null)
            {
                result = resp.result.messages;
            }

            return result;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        
    } 

    
    
    
    private void PopulateHeaders(EmailAccount emailAccount)
    {
        _headers = new Dictionary<string, string>
        {
            { "accept", "application/json" },
            { "accept-language", "en-US,en;q=0.9" },
            { "cache-control", "no-cache" },
            { "pragma", "no-cache" },
            { "priority", "u=1, i" },
            { "referer", "https://mail.yahoo.com/" },
            { "sec-ch-ua", "\"Chromium\";v=\"130\", \"Google Chrome\";v=\"130\", \"Not?A_Brand\";v=\"99\"" },
            { "sec-ch-ua-mobile", "?0" },
            { "sec-ch-ua-platform", "\"Windows\"" },
            { "sec-fetch-dest", "empty" },
            { "sec-fetch-mode", "cors" },
            { "sec-fetch-site", "same-origin" },
            { "user-agent", emailAccount.UserAgent }, // Dynamic user-agent
            { "cookie", emailAccount.MetaIds.Cookie } // Dynamic cookie
        };
    }

    private string GenerateEndpoint(EmailAccount emailAccount)
    {
        emailAccount.MetaIds.YmreqId = YmriqId.GetYmreqid(emailAccount.MetaIds.YmreqId, 2).LastOrDefault()!;
        string hash = RandomGenerator.GenerateRandomHexString(8);
        return $"https://mail.yahoo.com/ws/v3/batch?name=messages.UnifiedUpdate&hash={hash}&appId=YMailNorrin&ymreqid={emailAccount.MetaIds.YmreqId}&wssid={emailAccount.MetaIds.Wssid}";
    }

    
}