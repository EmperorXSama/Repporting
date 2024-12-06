using System.Collections;
using System.Diagnostics;


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
        var messasges = await GetMessagesFromFolder(emailAccount,directoryId);


        return new ReturnTypeObject() { ReturnedValue = messasges,Message = $" number of retrieved messages in {folderName} : {messasges.Count} \n total number of messages : {(messasges.Any()? messasges[0].folder.total:0 )}" };
    }

    public async Task<ReturnTypeObject> ProcessMarkMessagesAsReadFromDir(EmailAccount emailAccount,
        MarkMessagesAsReadConfig config,string directoryId = "1")
    {
            CheckEmailMetaData(emailAccount);
            var messages = await GetMessagesFromFolder(emailAccount ,directoryId);

            var result = await MarkMessagesAsRead(emailAccount, messages,config.BulkThreshold,config.BulkChunkSize,config.SingleThreshold,config.MinMessagesValue ,config.MaxMessagesValue);
            
            return new ReturnTypeObject(){Message = result};
    }  
    public async Task<ReturnTypeObject> ProcessMarkMessagesAsNotSpam(EmailAccount emailAccount, 
        MarkMessagesAsReadConfig config)
    {
        CheckEmailMetaData(emailAccount);
    
        int totalMessages = 0;
        int processedMessages = 0;

        while (true)
        {
            // Fetch messages from the spam folder with pagination
            var messages = await GetMessagesFromFolder(emailAccount, Statics.SpamDir);
        
            if (!messages.Any())
            {
                break; // No more messages to process
            }

            // Set totalMessages once based on folder metadata
            if (totalMessages == 0)
            {
                totalMessages = messages[0].folder.total;
            }

            // Process the messages
            processedMessages += await MarkMessagesAsNotSpam(
                emailAccount, messages, config.BulkThreshold, config.BulkChunkSize, config.SingleThreshold);
            
        }

        return new ReturnTypeObject
        {
            Message = $"Marked messages: {processedMessages} out of {totalMessages}"
        };
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
   
    private async Task<ObservableCollection<FolderMessages>> GetMessagesFromFolder(EmailAccount emailAccount , string dirId)
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
            ObservableCollection<FolderMessages> result = new();
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

    #region Pre reporting calls
    
    private async Task<string> MarkMessagesAsRead(EmailAccount emailAccount, 
        IEnumerable<FolderMessages> messages,
        int  bulkThreshold,int bulkChunkSize,int singleThreshold,int minMessagesValue,int maxMessagesValue)
    {
        int marked = 0;
        int numberOfMessagesToMarkAsRead = RandomGenerator.GetRandomBetween2Numbers(minMessagesValue, maxMessagesValue);
        ObservableCollection<FolderMessages> messagesToMarkAsRead = new ObservableCollection<FolderMessages>();
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
        await BulkProcessor<FolderMessages>.ProcessItemsAsync(messagesToMarkAsRead,

            async (bulkMessages) =>
            {
                var messagesEnumerable = bulkMessages as FolderMessages[] ?? bulkMessages.ToArray();
                try
                {
                    var inboxMessagesEnumerable = bulkMessages as FolderMessages[] ?? messagesEnumerable.ToArray();
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
        
     
        
        return $"{marked} of {numberOfMessagesToMarkAsRead} messages has been marked as read ";
    }
                                     
    #endregion

    #region Mark not spam
        
        private async Task<int> MarkMessagesAsNotSpam(
            EmailAccount emailAccount,
            IEnumerable<FolderMessages> messages,
            int bulkThreshold,
            int bulkChunkSize,
            int singleThreshold)
        {
            int marked = 0;
            var folderMessagesArray = messages.ToArray();

            // Helper method for adding API responses
            void AddApiResponse(string tag, string message) =>
                emailAccount.ApiResponses.Add(new KeyValuePair<string, object>(
                    $"[{tag}] {DateTime.UtcNow:g}", message));
            // Helper method to process bulk messages
            async Task ProcessBulkMessages(FolderMessages[] bulkMessages)
            {
                var messageIds = bulkMessages.Select(m => m.id).ToList();
                try
                {
                    string payload = PayloadManager.GetMarkMessagesAsNotSpamPayload(emailAccount.MetaIds.MailId, messageIds);
                    string endpoint = GenerateEndpoint(emailAccount, EndpointType.UnifiedUpdate);
                    PopulateHeaders(emailAccount);

                    string response = await _apiConnector.PostDataAsync<string>(endpoint, payload, _headers, emailAccount.Proxy);
                    var responseMessage = JsonConvert.DeserializeObject<MNSRootObject>(response);

                    if (responseMessage.result.status.successRequests.Any())
                    {
                        marked += bulkMessages.Length;
                        AddApiResponse(
                            "BulkMNSMessages",
                            $"Successfully marked {bulkMessages.Length} messages. IDs: {messageIds.First()} to {messageIds.Last()}");
                    }
                    else
                    {
                        AddApiResponse("BulkMNSMessages", $"Failed to mark {bulkMessages.Length} messages.");
                    }
                }
                catch (Exception ex)
                {
                    AddApiResponse(
                        "BulkMNSMessages",
                        $"Failed to mark messages. IDs: {messageIds.FirstOrDefault()} to {messageIds.LastOrDefault()}. Error: {ex.Message}");
                }
                await Task.Delay(TimeSpan.FromSeconds(2));
            }

            // Helper method to process single messages
            async Task ProcessSingleMessage(FolderMessages singleMessage)
            {
                try
                {
                    string payload = PayloadManager.GetMarkMessageAsNotSpamPayload(emailAccount.MetaIds.MailId, singleMessage.id);
                    string endpoint = GenerateEndpoint(emailAccount, EndpointType.UnifiedUpdate);
                    PopulateHeaders(emailAccount);

                    string response = await _apiConnector.PostDataAsync<string>(endpoint, payload, _headers, emailAccount.Proxy);
                    var responseMessage = JsonConvert.DeserializeObject<HttpResponseMessage>(response);
                    responseMessage?.EnsureSuccessStatusCode();

                    marked++;
                    AddApiResponse(
                        "ReadMessage",
                        $"{marked} out of {folderMessagesArray.FirstOrDefault()?.folder.total ?? 0} marked. ID: {singleMessage.id}");
                }
                catch (Exception ex)
                {
                    AddApiResponse("MNSMessage", $"Failed to mark message ID: {singleMessage.id}. Error: {ex.Message}");
                }
                await Task.Delay(TimeSpan.FromSeconds(2));
            }

            // Use BulkProcessor to handle messages
            await BulkProcessor<FolderMessages>.ProcessItemsAsync(
                folderMessagesArray,
                bulkMessages => ProcessBulkMessages(bulkMessages.ToArray()),
                singleMessage => ProcessSingleMessage(singleMessage),
                bulkThreshold: bulkThreshold,
                bulkChunkSize: bulkChunkSize,
                singleThreshold: singleThreshold);

            return marked;
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
