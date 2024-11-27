using System.Diagnostics;
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
    public async Task<ReturnTypeObject> ProcessGetMessagesFromDir(EmailAccount emailAccount,string directoryId)
    {
        var folderNames = new Dictionary<string, string>
        {
         
               { Statics.InboxDir,"Inbox"},
                { Statics.SpamDir,"Spam"},
                { Statics.DraftDir,"Draft"},
                { Statics.SentDir,"Sent"},
                { Statics.TrashDir,"Trash"},
                { Statics.ArchiveDir,"Archive"},
        };
        folderNames.TryGetValue(directoryId, out var folderName);
        folderName ??= "Unknown";
        CheckEmailMetaData(emailAccount);
        var messasges = await GetMessagesFromInboxFolder(emailAccount,directoryId);


        return new ReturnTypeObject() { ReturnedValue = messasges,Message = $" number of messages in {folderName} : {messasges.Count}" };
    }

    public async Task<ReturnTypeObject> ProcessMarkMessagesAsReadFromDir(EmailAccount emailAccount,int   bulkThreshold= 60,int bulkChunkSize= 30,int singleThreshold=  20 ,string directoryId =Statics.InboxDir, IEnumerable<InboxMessages>? messages= null)
    {
        try
        {
            CheckEmailMetaData(emailAccount);
            if (messages is null) messages = await GetMessagesFromInboxFolder(emailAccount , directoryId);

            var result = await MarkMessagesAsRead(emailAccount, messages,bulkThreshold, bulkChunkSize, singleThreshold);
            
            return new ReturnTypeObject(){Message = result};
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
            
        }
    }
    
    public async Task<bool> SendReportAsync(EmailAccount emailAccount,string messageId)
    {
        PopulateHeaders(emailAccount);
        emailAccount.MetaIds.YmreqId = YmriqId.GetYmreqid(emailAccount.MetaIds.YmreqId, 2).LastOrDefault()!;
        
        string endpoint = GenerateEndpoint(emailAccount, EndpointType.OtherEndpoint);
        
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
   


    #region Pre reporting calls

    private async Task<ObservableCollection<InboxMessages>> GetMessagesFromInboxFolder(EmailAccount emailAccount , string dirId )
    {
        try
        {
           
            string endpoint = GenerateEndpoint(emailAccount, EndpointType.ReadSync);
            PopulateHeaders(emailAccount);
            string payload = PayloadManager.GetCorrectFolderPayload(emailAccount.MetaIds.MailId, dirId);
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
            throw new Exception($"[Request Error] {e.Message}");
        }
        
        
    }

    private async Task<string> MarkMessagesAsRead(EmailAccount emailAccount, 
        IEnumerable<InboxMessages> messages,
        int   bulkThreshold,int bulkChunkSize,int singleThreshold)
    {
        int marked = 0;
        int numberOfMessagesToMarkAsRead = RandomGenerator.GetRandomBetween2Numbers(6, 11);
        ObservableCollection<InboxMessages> messagesToMarkAsRead = new ObservableCollection<InboxMessages>();
        foreach (var ms in messages)
        {
            if (!ms.flags.read)
            {
                messagesToMarkAsRead.Add(ms);
            }

            if (messagesToMarkAsRead.Count >= numberOfMessagesToMarkAsRead)
            {
                break;
            }
        }
        await BulkProcessor<InboxMessages>.ProcessItemsAsync(messagesToMarkAsRead,

            async (bulkMessages) =>
            {
                var messagesEnumerable = bulkMessages as InboxMessages[] ?? bulkMessages.ToArray();
                try
                {
                    var inboxMessagesEnumerable = bulkMessages as InboxMessages[] ?? messagesEnumerable.ToArray();
                    var messageIds = inboxMessagesEnumerable.Select(m => m.id).ToList();

                    // Create the payload
                    string payload = PayloadManager.GetMarkMessagesAsReadBulkPayload(emailAccount.MetaIds.MailId, messageIds);
                    string endpoint = GenerateEndpoint(emailAccount, EndpointType.ReadFlagUpdate);
                    PopulateHeaders(emailAccount);

                    // Send the request
                    string response = await _apiConnector.PostDataAsync<string>(endpoint, payload, _headers, emailAccount.Proxy);
                    var responseMessage = JsonConvert.DeserializeObject<HttpResponseMessage>(response);

                    // Ensure the request was successful
                    responseMessage?.EnsureSuccessStatusCode();

                    // Increment the marked count and add a success message
                    marked += inboxMessagesEnumerable.Count();
                    string firstId = messageIds.First();
                    string lastId = messageIds.Last();
                    emailAccount.ApiResponses.Add(new KeyValuePair<string, object>(
                        $"[BulkReadMessages] {DateTime.UtcNow.ToString("g")}",
                        $"Successfully marked {inboxMessagesEnumerable.Count()} messages. IDs: {firstId} to {lastId} \n"
                    ));

                    Debug.WriteLine($"Marked {marked} of {messages.Count()} messages || IDs: {firstId} to {lastId}");
                }
                catch (Exception ex)
                {
                    // Handle errors and add an error message to the responses
                    string firstId = messagesEnumerable.FirstOrDefault()?.id ?? "N/A";
                    string lastId = messagesEnumerable.LastOrDefault()?.id ?? "N/A";
                    emailAccount.ApiResponses.Add(new KeyValuePair<string, object>(
                        $"[BulkReadMessages] {DateTime.UtcNow.ToString("g")}",
                        $"Failed to mark messages. IDs: {firstId} to {lastId}. Error: {ex.Message} \n"
                    ));

                    Debug.WriteLine($"Error marking messages. Exception: {ex}");
                }
                await Task.Delay(TimeSpan.FromSeconds(2));
            },
            async (singleMessages) =>
            {
                try
                {
                    string payload = PayloadManager.GetMarkMessageAsReadSSinglePayload(emailAccount.MetaIds.MailId, singleMessages.id);
                    string endpoint = GenerateEndpoint(emailAccount, EndpointType.ReadFlagUpdate);
                    PopulateHeaders(emailAccount);

                    // Send the request
                    string response = await _apiConnector.PostDataAsync<string>(endpoint, payload, _headers, emailAccount.Proxy);
                    var responseMessage = JsonConvert.DeserializeObject<HttpResponseMessage>(response);

                    // Ensure the request was successful
                    responseMessage?.EnsureSuccessStatusCode();
                    if (marked >=2)
                    {
                        throw new Exception($"Marked {marked} of {messages.Count()} messages");
                    }
                    // Increment the marked count and add a success message
                    marked++;
                    emailAccount.ApiResponses.Add(new KeyValuePair<string, object>(
                        $"[ReadMessage] {DateTime.UtcNow.ToString("g")}", 
                        $"{marked} out of {numberOfMessagesToMarkAsRead} id: {singleMessages.id} \n"
                    ));
                    
                }
                catch (Exception ex)
                {
                    // Handle errors and add an error message to the responses
                    emailAccount.ApiResponses.Add(new KeyValuePair<string, object>(
                        $"[ReadMessage] {DateTime.UtcNow.ToString("g")}",
                        $"Failed to mark message id: {singleMessages.id}. Error: {ex.Message} \n"
                    ));
                    
                }
                
                await Task.Delay(TimeSpan.FromSeconds(2));
            },
            bulkThreshold: bulkThreshold,
            bulkChunkSize: bulkChunkSize,
            singleThreshold: singleThreshold
            
            
            );
        
     
        
        return $"{marked} of {messages.Count()} messages has been marked as read ";
    }

    #endregion
    
    
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
            { "user-agent", emailAccount.UserAgent },
            { "cookie", emailAccount.MetaIds.Cookie } 
        };
    }

    private string GenerateEndpoint(EmailAccount emailAccount, EndpointType endpointType)
    {
        emailAccount.MetaIds.YmreqId = YmriqId.GetYmreqid(emailAccount.MetaIds.YmreqId, 2).LastOrDefault()!;
        string hash = RandomGenerator.GenerateRandomHexString(8);
        
        return endpointType switch
        {
            EndpointType.ReadFlagUpdate =>
                $"https://mail.yahoo.com/ws/v3/batch?name=messages.readFlagUpdate&hash={hash}&appId=YMailNorrin&ymreqid={emailAccount.MetaIds.YmreqId}&wssid={emailAccount.MetaIds.Wssid}",
            EndpointType.UnifiedUpdate =>
                $"https://mail.yahoo.com/ws/v3/batch?name=messages.UnifiedUpdate&hash={hash}&appId=YMailNorrin&ymreqid={emailAccount.MetaIds.YmreqId}&wssid={emailAccount.MetaIds.Wssid}", 
            EndpointType.ReadSync =>
                $"https://mail.yahoo.com/ws/v3/batch?name=folderChange.getList&hash={hash}&appId=YMailNorrin&ymreqid={emailAccount.MetaIds.YmreqId}&wssid={emailAccount.MetaIds.Wssid}",
            EndpointType.OtherEndpoint =>
                $"https://mail.yahoo.com/ws/v3/batch?name=messages.OtherEndpoint&hash={hash}&appId=YMailNorrin&ymreqid={emailAccount.MetaIds.YmreqId}&wssid={emailAccount.MetaIds.Wssid}",
            _ => throw new ArgumentException("Invalid endpoint type", nameof(endpointType))
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


}

public enum EndpointType
{
    ReadFlagUpdate,
    UnifiedUpdate,
    ReadSync,
    OtherEndpoint
}
