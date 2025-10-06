using System.Net.Sockets;
using Newtonsoft.Json.Linq;
using Polly;
using Reporting.lib.Models.Secondary;
using RepportingApp.CoreSystem.ProxyService;

namespace RepportingApp.Request_Connection_Core.Reporting;

public class ReportingRequests : IReportingRequests
{
    private readonly IApiConnector _apiConnector;
    private readonly ProxyListManager proxyListManager;
    
    
    
    public ReportingRequests(IApiConnector apiConnector)
    {
        _apiConnector = apiConnector;
       proxyListManager = new ProxyListManager();
    }
    
    public async Task<List<ReturnTypeObject>> ProcessGetMessagesFromDirs(EmailAccount emailAccount, IEnumerable<string> directoryIds)
    {
        var results = new List<ReturnTypeObject>();

        foreach (var directoryId in directoryIds)
        {
            var result = await ProcessGetMessagesFromDir(emailAccount, directoryId);
            results.Add(result);
        }

        return results;
    }

public async   Task<ReturnTypeObject> ProcessGetMessagesFromDir(EmailAccount emailAccount, string directoryId)
{
    var retryPolicy = Policy
        .Handle<SocketException>()
        .Or<HttpRequestException>()
        .Or<Exception>(ex => ex.Message.Contains("Proxy error") || ex.Message.Contains("connection"))
        .WaitAndRetryAsync(
            retryCount: 3,
            retryAttempt => TimeSpan.FromSeconds(1),
            (exception, timeSpan, retryCount, context) =>
            {
                var reservedProxyInUse = proxyListManager.GetRandomDifferentSubnetProxyDb(emailAccount.Proxy);
                emailAccount.Proxy = reservedProxyInUse;
            }
        );

    return await retryPolicy.ExecuteAsync(async () =>
    {
        var folderNames = new Dictionary<string, string>
        {
            { Statics.InboxDir, "Inbox" },
            { Statics.SpamDir, "Spam" },
            { Statics.DraftDir, "Draft" },
            { Statics.SentDir, "Sent" },
            { Statics.TrashDir, "Trash" },
            { Statics.ArchiveDir, "Archive" },
        };
        folderNames.TryGetValue(directoryId, out var folderName);
        folderName ??= "Unknown";

        CheckEmailMetaData(emailAccount);
        var messages = await GetMessagesFromFolder(emailAccount, directoryId);

        if (emailAccount.Stats == null)
        {
            emailAccount.Stats = new EmailAccountStats();
        }
        var count = messages.Any() ? messages[0].folder.total : 0;

        switch (directoryId)
        {
            case Statics.InboxDir:
                emailAccount.Stats.InboxCount = count;
                break;
            case Statics.SpamDir:
                emailAccount.Stats.SpamCount = count;
                break;
        }

        return new ReturnTypeObject
        {
            ReturnedValue = messages,
            Message =
                $"Number of retrieved messages in {folderName}: {messages.Count}"
        };
    });
}

public async Task<List<ReturnTypeObject>> ProcessMarkMessagesAsReadFromDirs(EmailAccount emailAccount, MarkMessagesAsReadConfig config, List<string> directoryIds)
{
    var results = new List<ReturnTypeObject>();

    foreach (var directoryId in directoryIds)
    {
        var retryPolicy = Policy
            .Handle<SocketException>()
            .Or<HttpRequestException>()
            .Or<Exception>(ex => ex.Message.Contains("Proxy error") || ex.Message.Contains("connection"))
            .WaitAndRetryAsync(
                retryCount: 3,
                retryAttempt => TimeSpan.FromSeconds(1),
                (exception, timeSpan, retryCount, context) =>
                {
                    var reservedProxyInUse = proxyListManager.GetRandomDifferentSubnetProxyDb(emailAccount.Proxy);
                    emailAccount.Proxy = reservedProxyInUse;
                }
            );

        var result = await retryPolicy.ExecuteAsync(async () =>
        {
            CheckEmailMetaData(emailAccount);
            var messages = await GetMessagesFromFolder(emailAccount, directoryId);

            var messageResult = await MarkMessagesAsRead(
                emailAccount,
                messages,
                config.BulkThreshold,
                config.BulkChunkSize,
                config.SingleThreshold,
                config.PreReportingSettings.MinMessagesToRead,
                config.PreReportingSettings.MaxMessagesToRead
            );

            return new ReturnTypeObject { Message = messageResult };
        });

        results.Add(result);
    }

    return results;
}


public async Task<List<ReturnTypeObject>> MoveMessagesToTargetDirectory(EmailAccount emailAccount, MarkMessagesAsReadConfig config,  List<string> directoryIds , string toDirectoryId)
{
    var results = new List<ReturnTypeObject>();
    foreach (var fromDirectoryId in directoryIds)
    {
        var retryPolicy = Policy
            .Handle<SocketException>()
            .Or<HttpRequestException>()
            .Or<Exception>(ex => ex.Message.Contains("Proxy error") || ex.Message.Contains("connection") )
            .WaitAndRetryAsync(
                retryCount: 3,
                retryAttempt => TimeSpan.FromSeconds(1),
                (exception, timeSpan, retryCount, context) =>
                {
                    var reservedProxyInUse = proxyListManager.GetRandomDifferentSubnetProxyDb(emailAccount.Proxy);
                    emailAccount.Proxy = reservedProxyInUse;
                }
            );

        var result =  await retryPolicy.ExecuteAsync(async () =>
        {
            CheckEmailMetaData(emailAccount);
            
            string messageResult = string.Empty;
            if (toDirectoryId == Statics.ArchiveDir)
            {
                var messages = await GetMessagesFromFolder(emailAccount, fromDirectoryId,config.PreReportingSettings.MaxMessagesToArchive);
                if (messages.Count <= 0) return new ReturnTypeObject { Message = "No messages found." };
                messageResult = await SendMessagesToArchive(
                    emailAccount,
                    messages,
                    config.BulkThreshold,
                    config.BulkChunkSize,
                    config.SingleThreshold,
                    config.PreReportingSettings.MinMessagesToArchive,
                    config.PreReportingSettings.MaxMessagesToArchive
                );
            }else if (toDirectoryId == Statics.TrashDir)
            {
                var messages = await GetMessagesFromFolder(emailAccount, fromDirectoryId,config.PreReportingSettings.MaxMessagesToRead);
                if (messages.Count <= 0) return new ReturnTypeObject { Message = "No messages found." };
                messageResult = await SendMessagesToTrash(
                    emailAccount,
                    messages,
                    config.BulkThreshold,
                    config.BulkChunkSize,
                    config.SingleThreshold,
                    config.PreReportingSettings.MinMessagesToRead,
                    config.PreReportingSettings.MaxMessagesToRead
                );
            }
        

            return new ReturnTypeObject { Message = messageResult };
        });
        results.Add(result);
    }
    var numberOfMessagesInTrash = await GetTotalNumberOfMessagesInaFolder(emailAccount,"4");
    if (numberOfMessagesInTrash != 0 && numberOfMessagesInTrash != null)
    {
        var deletionResult = await DeleteMessagesPermanent(emailAccount, numberOfMessagesInTrash.ToString());
        results.Add(new ReturnTypeObject { Message =deletionResult });
    }
    
    return results;
}

public async Task<List<ReturnTypeObject>> ProcessMarkMessagesAsNotSpam(EmailAccount emailAccount, MarkMessagesAsReadConfig config)
{
    var headers = PopulateHeaders(emailAccount);
    var retryPolicy = Policy
        .Handle<SocketException>()
        .Or<HttpRequestException>()
        .Or<Exception>(ex => ex.Message.Contains("Proxy error") || ex.Message.Contains("connection"))
        .WaitAndRetryAsync(
            retryCount: 3,
            retryAttempt => TimeSpan.FromSeconds(1),
            (exception, timeSpan, retryCount, context) =>
            {
                var reservedProxyInUse = proxyListManager.GetRandomDifferentSubnetProxyDb(emailAccount.Proxy);
                emailAccount.Proxy = reservedProxyInUse;
            }
        );

    return await retryPolicy.ExecuteAsync(async () =>
    {
        CheckEmailMetaData(emailAccount);

        var messages = await GetMessagesFromFolder(emailAccount, Statics.SpamDir);
        if (!messages.Any())
        {
             return new  List<ReturnTypeObject>
            {
                new ReturnTypeObject { Message = "Spam empty" }
                };
        }

        int totalMessages = 0;
        int processedMessages = 0;

        if (config.PreReportingSettings.IsPreReporting)
        {
            await ProcessMarkMessagesAsReadFromDirs(emailAccount, config,new List<string>(){Statics.InboxDir});
            await MoveMessagesToTargetDirectory(emailAccount, config,new List<string>(){Statics.InboxDir},Statics.ArchiveDir);
        }

        while (true)
        {
            messages = await GetMessagesFromFolder(emailAccount, Statics.SpamDir);

            if (!messages.Any())
            {
                break; // No more messages to process
            }

            if (totalMessages == 0)
            {
                totalMessages = messages[0].folder.total;
            }

            processedMessages += await MarkMessagesAsNotSpam(
                emailAccount,
                messages,
                config.BulkThreshold,
                config.BulkChunkSize,
                config.SingleThreshold
            );

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        if (emailAccount.Stats == null)
            emailAccount.Stats = new EmailAccountStats();

        emailAccount.Stats.LastNotSpamCount = processedMessages;
        return new  List<ReturnTypeObject>
        {
            new ReturnTypeObject()
            {
                Message = $"Marked messages: {processedMessages} out of {totalMessages}"
                
            }
        };
    });
}


   
    private async Task<ObservableCollection<FolderMessages>> GetMessagesFromFolder(EmailAccount emailAccount, string dirId,int messageCount = 100)
    {
        var headers = PopulateHeaders(emailAccount);

        // Define a retry policy for specific exceptions.
        var retryPolicy = Policy
            .Handle<SocketException>() // Catch socket errors
            .Or<HttpRequestException>() // Catch network-related errors
            .Or<Exception>(ex => ex.Message.Contains("Proxy error") || ex.Message.Contains("connection")) // Catch proxy-related errors
            .WaitAndRetryAsync(
                retryCount: 3, // Retry only once
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
            string endpoint = GenerateEndpoint(emailAccount, EndpointType.ReadSync);
            string payload = PayloadManager.GetCorrectFolderPayload(emailAccount.MetaIds.MailId, dirId,messageCount);

            // Call the API
            string response = await _apiConnector.PostDataAsync<string>(
                emailAccount,
                endpoint,
                payload,
                headers,
                emailAccount.Proxy
            );

            // Deserialize response and process messages
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
        });
    }  
    public static int? GetFolderTotalTyped(string jsonString, string folderId)
    {
        try
        {
            var data = JsonConvert.DeserializeObject<SyncRootObject>(jsonString);
            var folder = data.result.responses[0].response.result.folders
                .FirstOrDefault(f => f.id == folderId);
        
            return folder?.total;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deserializing JSON: {ex.Message}");
            return null;
        }
    }
    private async Task<int?> GetTotalNumberOfMessagesInaFolder(EmailAccount emailAccount, string dirId)
    {
        var headers = PopulateHeaders(emailAccount);

        // Define a retry policy for specific exceptions.
        var retryPolicy = Policy
            .Handle<SocketException>() // Catch socket errors
            .Or<HttpRequestException>() // Catch network-related errors
            .Or<Exception>(ex => ex.Message.Contains("Proxy error") || ex.Message.Contains("connection")) // Catch proxy-related errors
            .WaitAndRetryAsync(
                retryCount: 3, // Retry only once
                retryAttempt => TimeSpan.FromSeconds(1), // Wait 2 seconds before retry
                (exception, timeSpan, retryCount, context) =>
                {
                    var reservedProxyInUse = proxyListManager.GetRandomDifferentSubnetProxyDb(emailAccount.Proxy);
                    emailAccount.Proxy = reservedProxyInUse;
                }
            );


        var result =  await retryPolicy.ExecuteAsync(async () =>
        {
            // Generate endpoint and payload
            string endpoint = GenerateEndpoint(emailAccount, EndpointType.Sync);
            string payload = PayloadManager.GetFoldersInformation(emailAccount.MetaIds.MailId);

            // Call the API
            string response = await _apiConnector.PostDataAsync<string>(
                emailAccount,
                endpoint,
                payload,
                headers,
                emailAccount.Proxy
            );

            int? totalTyped = GetFolderTotalTyped(response, dirId);
            return totalTyped;
        });
        return result;
    }
private async Task<string> DeleteMessagesPermanent(EmailAccount emailAccount, string messagesNumber)
    {
        var headers = PopulateHeaders(emailAccount);

        // Define a retry policy for specific exceptions.
        var retryPolicy = Policy
            .Handle<SocketException>() // Catch socket errors
            .Or<HttpRequestException>() // Catch network-related errors
            .Or<Exception>(ex => ex.Message.Contains("Proxy error") || ex.Message.Contains("connection")) // Catch proxy-related errors
            .WaitAndRetryAsync(
                retryCount: 3, // Retry only once
                retryAttempt => TimeSpan.FromSeconds(1), // Wait 2 seconds before retry
                (exception, timeSpan, retryCount, context) =>
                {
                    var reservedProxyInUse = proxyListManager.GetRandomDifferentSubnetProxyDb(emailAccount.Proxy);
                    emailAccount.Proxy = reservedProxyInUse;
                }
            );


        var result =  await retryPolicy.ExecuteAsync(async () =>
        {
            // Generate endpoint and payload
            string endpoint = GenerateEndpoint(emailAccount, EndpointType.Delete);
            string payload = PayloadManager.GetPermanentDeletePayload(emailAccount.MetaIds.MailId,messagesNumber);

            // Call the API
            string response = await _apiConnector.PostDataAsync<string>(
                emailAccount,
                endpoint,
                payload,
                headers,
                emailAccount.Proxy
            );

            return CheckDeletionStatus(response);
        });
        return result;
    }
    public static string CheckDeletionStatus(string jsonResponse)
    {
        try
        {
            JObject jsonData = JObject.Parse(jsonResponse);
            var failedRequests = jsonData["result"]["status"]["failedRequests"] as JArray;
        
            return failedRequests != null && failedRequests.Count == 0 
                ? "success deletion" 
                : "fail deletion";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing response: {ex.Message}");
            return "fail deletion";
        }
    }

    #region Pre reporting calls
    
    private async Task<string> MarkMessagesAsRead(EmailAccount emailAccount, 
        IEnumerable<FolderMessages> messages,
        int  bulkThreshold,int bulkChunkSize,int singleThreshold,int minMessagesValue,int maxMessagesValue)
    {
        emailAccount.ApiResponses.Clear();
        var headers = PopulateHeaders(emailAccount);
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
                    string response = await _apiConnector.PostDataAsync<string>(emailAccount,endpoint, payload, headers, emailAccount.Proxy);
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
                    string response = await _apiConnector.PostDataAsync<string>(emailAccount,endpoint, payload, headers, emailAccount.Proxy);
                    var responseMessage = JsonConvert.DeserializeObject<HttpResponseMessage>(response);

                    // Ensure the request was successful
                    responseMessage?.EnsureSuccessStatusCode();
                    // Increment the marked count and add a success message
                    marked++;
                    emailAccount.ApiResponses.Add(new KeyValuePair<string, object>(
                        $"[ReadMessage] {DateTime.UtcNow.ToString("g")}", 
                        $"{marked} out of {numberOfMessagesToMarkAsRead} id: {singleMessages.headers.subject} \n"
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
        
     
        emailAccount.Stats.LastReadCount = marked;
        return $"{marked} of {numberOfMessagesToMarkAsRead} messages has been marked as read ";
    }
                                     
     private async Task<string> SendMessagesToArchive(EmailAccount emailAccount, 
        IEnumerable<FolderMessages> messages,
        int  bulkThreshold,int bulkChunkSize,int singleThreshold,int minMessagesValue,int maxMessagesValue)
    {
        emailAccount.ApiResponses.Clear();
        var headers = PopulateHeaders(emailAccount);
        int marked = 0;
        int numberOfMessagesToMarkAsRead = RandomGenerator.GetRandomBetween2Numbers(minMessagesValue, maxMessagesValue);
        ObservableCollection<FolderMessages> messagesToArchive = new ObservableCollection<FolderMessages>();
        var folderMessagesEnumerable = messages.ToList();
        for (int i = 0; i < numberOfMessagesToMarkAsRead; i++)
        {
            int index = RandomGenerator.GetRandomBetween2Numbers(0, folderMessagesEnumerable.Count() - 1);
            messagesToArchive.Add(folderMessagesEnumerable[index]);
            if (messagesToArchive.Count >= numberOfMessagesToMarkAsRead)
            {
                break;
            }
        }
        await BulkProcessor<FolderMessages>.ProcessItemsAsync(messagesToArchive,

            async (bulkMessages) =>
            {
                var messagesEnumerable = bulkMessages as FolderMessages[] ?? bulkMessages.ToArray();
                try
                {
                    var inboxMessagesEnumerable = bulkMessages as FolderMessages[] ?? messagesEnumerable.ToArray();
                    var messageIds = inboxMessagesEnumerable.Select(m => m.id).ToList();

                    // Create the payload
                    string payload = PayloadManager.GetArchiveMessageBulkPayload(emailAccount.MetaIds.MailId, messageIds);
                    string endpoint = GenerateEndpoint(emailAccount, EndpointType.Move);
                    PopulateHeaders(emailAccount);

                    // Send the request
                    string response = await _apiConnector.PostDataAsync<string>(emailAccount,endpoint, payload, headers, emailAccount.Proxy);
                    var responseMessage = JsonConvert.DeserializeObject<HttpResponseMessage>(response);

                    // Ensure the request was successful
                    responseMessage?.EnsureSuccessStatusCode();

                    // Increment the marked count and add a success message
                    marked += inboxMessagesEnumerable.Count();
                    string firstId = messagesEnumerable.First().headers.subject;
                    string lastId = messagesEnumerable.Last().headers.subject;
                    emailAccount.ApiResponses.Add(new KeyValuePair<string, object>(
                        $"[BulkArchiveMessages] {DateTime.UtcNow.ToString("g")}",
                        $"Successfully archived {inboxMessagesEnumerable.Count()} messages. IDs: {firstId} to {lastId} \n"
                    ));
                }
                catch (Exception ex)
                {
                    // Handle errors and add an error message to the responses
                    string firstId = messagesEnumerable.FirstOrDefault()?.id ?? "N/A";
                    string lastId = messagesEnumerable.LastOrDefault()?.id ?? "N/A";
                    emailAccount.ApiResponses.Add(new KeyValuePair<string, object>(
                        $"[BulkArchiveMessages] {DateTime.UtcNow.ToString("g")}",
                        $"Failed to archive messages. IDs: {firstId} to {lastId}. Error: {ex.Message} \n"
                    ));
                }
                await Task.Delay(TimeSpan.FromSeconds(2));
            },
            async (singleMessages) =>
            {
                try
                {
                    string payload = PayloadManager.GetArchiveMessageSinglePayload(emailAccount.MetaIds.MailId, singleMessages.id);
                    string endpoint = GenerateEndpoint(emailAccount, EndpointType.Move);
                    PopulateHeaders(emailAccount);

                    // Send the request
                    string response = await _apiConnector.PostDataAsync<string>(emailAccount,endpoint, payload, headers, emailAccount.Proxy);
                    var responseMessage = JsonConvert.DeserializeObject<HttpResponseMessage>(response);

                    // Ensure the request was successful
                    responseMessage?.EnsureSuccessStatusCode();
                    // Increment the marked count and add a success message
                    marked++;
                    emailAccount.ApiResponses.Add(new KeyValuePair<string, object>(
                        $"[ReadMessage] {DateTime.UtcNow.ToString("g")}", 
                        $"{marked} out of {numberOfMessagesToMarkAsRead} id: {singleMessages.headers.subject} \n"
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
        
     
        emailAccount.Stats.LastArchivedCount = marked;
        return $"{marked} of {numberOfMessagesToMarkAsRead} messages has been sent as archive ";
    }  
     private async Task<string> SendMessagesToTrash(EmailAccount emailAccount, 
        IEnumerable<FolderMessages> messages,
        int  bulkThreshold,int bulkChunkSize,int singleThreshold,int minMessagesValue,int maxMessagesValue)
    {
        emailAccount.ApiResponses.Clear();
        var headers = PopulateHeaders(emailAccount);
        int marked = 0;
        int numberOfMessagesToMarkAsRead = RandomGenerator.GetRandomBetween2Numbers(minMessagesValue, maxMessagesValue);
        ObservableCollection<FolderMessages> messagesToTrash = new ObservableCollection<FolderMessages>();
        var folderMessagesEnumerable = messages.ToList();
        for (int i = 0; i < numberOfMessagesToMarkAsRead; i++)
        {
            int index = RandomGenerator.GetRandomBetween2Numbers(0, folderMessagesEnumerable.Count() - 1);
            messagesToTrash.Add(folderMessagesEnumerable[index]);
            if (messagesToTrash.Count >= numberOfMessagesToMarkAsRead)
            {
                break;
            }
        }
        await BulkProcessor<FolderMessages>.ProcessItemsAsync(messagesToTrash,

            async (bulkMessages) =>
            {
                var messagesEnumerable = bulkMessages as FolderMessages[] ?? bulkMessages.ToArray();
                try
                {
                    var inboxMessagesEnumerable = bulkMessages as FolderMessages[] ?? messagesEnumerable.ToArray();
                    var messageIds = inboxMessagesEnumerable.Select(m => m.id).ToList();

                    // Create the payload
                    string payload = PayloadManager.MoveMessagesToTrashBulkPayload(emailAccount.MetaIds.MailId, messageIds);
                    string endpoint = GenerateEndpoint(emailAccount, EndpointType.Move);
                    PopulateHeaders(emailAccount);

                    // Send the request
                    string response = await _apiConnector.PostDataAsync<string>(emailAccount,endpoint, payload, headers, emailAccount.Proxy);
                    var responseMessage = JsonConvert.DeserializeObject<HttpResponseMessage>(response);

                    // Ensure the request was successful
                    responseMessage?.EnsureSuccessStatusCode();

                    // Increment the marked count and add a success message
                    marked += inboxMessagesEnumerable.Count();
                    string firstId = messagesEnumerable.First().headers.subject;
                    string lastId = messagesEnumerable.Last().headers.subject;
                    emailAccount.ApiResponses.Add(new KeyValuePair<string, object>(
                        $"[BulkTrashMessages] {DateTime.UtcNow.ToString("g")}",
                        $"Successfully Trash {inboxMessagesEnumerable.Count()} messages. IDs: {firstId} to {lastId} \n"
                    ));
                }
                catch (Exception ex)
                {
                    // Handle errors and add an error message to the responses
                    string firstId = messagesEnumerable.FirstOrDefault()?.id ?? "N/A";
                    string lastId = messagesEnumerable.LastOrDefault()?.id ?? "N/A";
                    emailAccount.ApiResponses.Add(new KeyValuePair<string, object>(
                        $"[BulkTrashMessages] {DateTime.UtcNow.ToString("g")}",
                        $"Failed to Trash messages. IDs: {firstId} to {lastId}. Error: {ex.Message} \n"
                    ));
                }
                await Task.Delay(TimeSpan.FromSeconds(2));
            },
            async (singleMessages) =>
            {
                try
                {
                    string payload = PayloadManager.MoveMessageToTrashSinglePayload(emailAccount.MetaIds.MailId, singleMessages.id);
                    string endpoint = GenerateEndpoint(emailAccount, EndpointType.Move);
                    PopulateHeaders(emailAccount);

                    // Send the request
                    string response = await _apiConnector.PostDataAsync<string>(emailAccount,endpoint, payload, headers, emailAccount.Proxy);
                    var responseMessage = JsonConvert.DeserializeObject<HttpResponseMessage>(response);

                    // Ensure the request was successful
                    responseMessage?.EnsureSuccessStatusCode();
                    // Increment the marked count and add a success message
                    marked++;
                    emailAccount.ApiResponses.Add(new KeyValuePair<string, object>(
                        $"[TrashMessage] {DateTime.UtcNow.ToString("g")}", 
                        $"{marked} out of {messagesToTrash.Count()} id: {singleMessages.headers.subject} \n"
                    ));
                    
                }
                catch (Exception ex)
                {
                    // Handle errors and add an error message to the responses
                    emailAccount.ApiResponses.Add(new KeyValuePair<string, object>(
                        $"[TrashMessage] {DateTime.UtcNow.ToString("g")}",
                        $"Failed to TrashMessage id: {singleMessages.id}. Error: {ex.Message} \n"
                    ));
                    
                }
                
                await Task.Delay(TimeSpan.FromSeconds(2));
            },
            bulkThreshold: bulkThreshold,
            bulkChunkSize: bulkChunkSize,
            singleThreshold: singleThreshold
            
            
            );
        
     
        emailAccount.Stats.LastArchivedCount = marked;
        return $"{marked} of {messagesToTrash.Count} messages has been sent as TrashMessage ";
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
            emailAccount.ApiResponses.Clear();
            var headers = PopulateHeaders(emailAccount);
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

                    string response = await _apiConnector.PostDataAsync<string>(emailAccount,endpoint, payload, headers, emailAccount.Proxy);
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

                    string response = await _apiConnector.PostDataAsync<string>(emailAccount,endpoint, payload, headers, emailAccount.Proxy);
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
    
    private Dictionary<string, string> PopulateHeaders(EmailAccount emailAccount)
    {
        string referrer = emailAccount.EmailAddress.EndsWith("@aol.com", StringComparison.OrdinalIgnoreCase)
            ? "https://mail.aol.com/"
            : "https://mail.yahoo.com/";
        return new Dictionary<string, string>
        {
            { "accept", "application/json" },
            { "accept-language", "en-US,en;q=0.9" },
            { "cache-control", "no-cache" },
            { "pragma", "no-cache" },
            { "priority", "u=1, i" },
            { "referer", "https://mail.yahoo.com/" },
            { "sec-ch-ua-mobile", "?0" },
            { "sec-ch-ua-platform", "\"Windows\"" },
            { "sec-fetch-dest", "empty" },
            { "sec-fetch-mode", "cors" },
            { "sec-fetch-site", "same-origin" },
            { "user-agent", emailAccount.UserAgent},
            { "cookie", emailAccount.MetaIds.Cookie.Trim() }
        };
    }


    private string GenerateEndpoint(EmailAccount emailAccount, EndpointType endpointType)
    {
        emailAccount.MetaIds.YmreqId = YmriqId.GetYmreqid(emailAccount.MetaIds.YmreqId, 2).LastOrDefault()!;
        string hash = RandomGenerator.GenerateRandomHexString(8);
    
        // Determine the base URL based on email domain
        string baseUrl = emailAccount.EmailAddress.EndsWith("@aol.com", StringComparison.OrdinalIgnoreCase)
            ? "https://mail.aol.com/ws/v3/batch"
            : "https://mail.yahoo.com/ws/v3/batch"; 
        string appId = emailAccount.EmailAddress.EndsWith("@aol.com", StringComparison.OrdinalIgnoreCase)
            ? "AolMailNorrin"
            : "YMailNorrin";
    
        return endpointType switch
        {
            EndpointType.ReadFlagUpdate =>
                $"{baseUrl}?name=messages.readFlagUpdate&hash={hash}&appId={appId}&ymreqid={emailAccount.MetaIds.YmreqId}&wssid={emailAccount.MetaIds.Wssid}",
            EndpointType.UnifiedUpdate =>
                $"{baseUrl}?name=messages.UnifiedUpdate&hash={hash}&appId={appId}&ymreqid={emailAccount.MetaIds.YmreqId}&wssid={emailAccount.MetaIds.Wssid}", 
            EndpointType.ReadSync =>
                $"{baseUrl}?name=folderChange.getList&hash={hash}&appId={appId}&ymreqid={emailAccount.MetaIds.YmreqId}&wssid={emailAccount.MetaIds.Wssid}",  
            EndpointType.Move =>
                $"{baseUrl}?name=messages.move&hash={hash}&appId={appId}&ymreqid={emailAccount.MetaIds.YmreqId}&wssid={emailAccount.MetaIds.Wssid}",
            EndpointType.Delete =>
                $"{baseUrl}?name=messages.delete&hash={hash}&appId={appId}&ymreqid={emailAccount.MetaIds.YmreqId}&wssid={emailAccount.MetaIds.Wssid}",
            EndpointType.Sync =>
                $"{baseUrl}?name=mailbox.sync&hash={hash}&appId={appId}&ymreqid={emailAccount.MetaIds.YmreqId}&wssid={emailAccount.MetaIds.Wssid}",
            EndpointType.OtherEndpoint =>
                $"{baseUrl}?name=messages.OtherEndpoint&hash={hash}&appId={appId}&ymreqid={emailAccount.MetaIds.YmreqId}&wssid={emailAccount.MetaIds.Wssid}",
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
    Sync,
    Move,
    Delete,
    OtherEndpoint
}
