namespace RepportingApp.Request_Connection_Core.Payload;

public static class PayloadManager
{
    public static string GetCorrectFolderPayload(string id,string folderId ,in int  count = 100)
    {
        return
            "{\"requests\":[{\"id\":\"GetMessageGroupList\",\"uri\":\"/ws/v3/mailboxes/@.id=="+id+"/messages/@.select==q?q=folderId%3A"+folderId+"%20acctId%3A1%20groupBy%3AconversationId%20count%3A"+count+"%20offset%3A0%20-folderType%3A(SYNC)-folderType%3A(INVISIBLE)%20-sort%3Adate\",\"method\":\"GET\",\"payloadType\":\"embedded\"},{\"id\":\"UnseenCountReset\",\"uri\":\"/ws/v3/mailboxes/@.id=="+id+"/decos/@.id==FTI\",\"method\":\"POST\",\"payload\":{\"id\":\"FTI\",\"counts\":[{\"accountId\":\"1\",\"unseen\":0}]}}],\"responseType\":\"json\"}";
        
    }  
    public static string GetFoldersInformation(string id)
    {
        return
            "{\"requests\":{\"id\":\"GetFolders\",\"uri\":\"/ws/v3/mailboxes/@.id=="+id+
            "/folders\",\"method\":\"GET\",\"payloadType\":\"embedded\"},\"responseType\":\"json\"}";

    }  
    public static string PrepareMailBoxPayload(string id)
    {
        return
            "{\"requests\":[{\"id\":\"GetMailboxAttribute_web.composeFontFamily\",\"uri\":\"/ws/v3/mailboxes/@.id=="+id+"/attributes/@.id==web.composeFontFamily\",\"method\":\"GET\"},{\"id\":\"GetMailboxAttribute_web.composeFontSize\",\"uri\":\"/ws/v3/mailboxes/@.id=="+id+"/attributes/@.id==web.composeFontSize\",\"method\":\"GET\"},{\"id\":\"GetMailboxAttribute_web.enableEnhancerLinkPreview\",\"uri\":\"/ws/v3/mailboxes/@.id=="+id+"/attributes/@.id==web.enableEnhancerLinkPreview\",\"method\":\"GET\"},{\"id\":\"GetMailboxAttribute_web.emojiRecentlyUsed\",\"uri\":\"/ws/v3/mailboxes/@.id=="+id+"/attributes/@.id==web.emojiRecentlyUsed\",\"method\":\"GET\"},{\"id\":\"GetMailboxAttribute_web.experimentalFeatures\",\"uri\":\"/ws/v3/mailboxes/@.id=="+id+"/attributes/@.id==web.experimentalFeatures\",\"method\":\"GET\"},{\"id\":\"GetMailboxAttribute_vacationPreferences\",\"uri\":\"/ws/v3/mailboxes/@.id=="+id+"/attributes/@.id==vacationPreferences\",\"method\":\"GET\"},{\"id\":\"GetMailboxAttribute_deliveryBlockedSenders\",\"uri\":\"/ws/v3/mailboxes/@.id=="+id+"/attributes/@.id==deliveryBlockedSenders\",\"method\":\"GET\"},{\"id\":\"GetMailboxAttribute_messageFilters\",\"uri\":\"/ws/v3/mailboxes/@.id=="+id+"/attributes/@.id==messageFilters\",\"method\":\"GET\"},{\"id\":\"GetMailboxAttribute_disposableAddressesPrefix\",\"uri\":\"/ws/v3/mailboxes/@.id=="+id+"/attributes/@.id==disposableAddressesPrefix\",\"method\":\"GET\"},{\"id\":\"GetMailboxAttribute_mailboxSize\",\"uri\":\"/ws/v3/mailboxes/@.id=="+id+"/attributes/@.id==mailboxSize\",\"method\":\"GET\"},{\"id\":\"GetMailboxAttribute_cp.consentEvents\",\"uri\":\"/ws/v3/mailboxes/@.id=="+id+"/attributes/@.id==cp.consentEvents\",\"method\":\"GET\"},{\"id\":\"GetMailboxAttribute_mailAutoForwardAllowed\",\"uri\":\"/ws/v3/mailboxes/@.id=="+id+"/attributes/@.id==mailAutoForwardAllowed\",\"method\":\"GET\"},{\"id\":\"GetMailboxAttribute_mailPremiumDeaAllowed\",\"uri\":\"/ws/v3/mailboxes/@.id=="+id+"/attributes/@.id==mailPremiumDeaAllowed\",\"method\":\"GET\"}],\"responseType\":\"json\"}";
        
    }  
    public static string GetPermanentDeletePayload(string id,string messagesNumber)
    {
        return
            "{\"requests\":[{\"id\":\"UnifiedDeleteMessage_0\",\"uri\":\"/ws/v3/mailboxes/@.id==" + id +
            "/messages/@.select==q?q=folderId%3A4%20count%3A"+messagesNumber+"&async=true\",\"method\":\"DELETE\"}],\"responseType\":\"json\"}";

    }   
    public static string CreateAliasePayload(string id , string nickName, string alias , string sendingName)
    {
        return
        "{\"requests\":[{\"id\":\"AddAccount\",\"uri\":\"/ws/v3/mailboxes/@.id==" + id +
            "/accounts\",\"method\":\"POST\",\"payload\":{\"account\":{\"sendingName\":\"" + sendingName +
            "\",\"type\":\"DEA\",\"email\":\"" + nickName.ToLower() + "-" + alias + "@yahoo.com\"}},\"requests\":[{\"id\":\"GetAccounts\",\"uri\":\"/ws/v3/mailboxes/@.id==" + id +
            "/accounts\",\"method\":\"GET\"}]}],\"responseType\":\"json\"}";
        
    }     
    public static string DeleteAliasePayload(string id , int deleteId)
    {
        return
            "{\"requests\":[{\"id\":\"deleteAccount\",\"uri\":\"/ws/v3/mailboxes/@.id=="+id+"/accounts/@.id=="+deleteId+"\",\"method\":\"DELETE\",\"requests\":[{\"id\":\"GetAccounts\",\"uri\":\"/ws/v3/mailboxes/@.id=="+id+"/accounts\",\"method\":\"GET\"}]}],\"responseType\":\"json\"}";

    }  
    public static string CollectMailBoxPayload(string id)
    {
        return
            "{\"requests\":[{\"id\":\"GetFolders\",\"uri\":\"/ws/v3/mailboxes/@.id==" + id +
            "/folders\",\"method\":\"GET\",\"payloadType\":\"embedded\"},{\"id\":\"GetAccounts\",\"uri\":\"/ws/v3/mailboxes/@.id==" +
            id +
            "/accounts\",\"method\":\"GET\"},{\"id\":\"GetMailboxAttribute_web.mailPreviewPane\",\"uri\":\"/ws/v3/mailboxes/@.id==" +
            id +
            "/attributes/@.id==web.mailPreviewPane\",\"method\":\"GET\"},{\"id\":\"GetMailboxAttribute_web.mailTabs\",\"uri\":\"/ws/v3/mailboxes/@.id==" +
            id +
            "/attributes/@.id==web.mailTabs\",\"method\":\"GET\"},{\"id\":\"GetMailboxAttribute_web.generalSnippet\",\"uri\":\"/ws/v3/mailboxes/@.id==" +
            id +
            "/attributes/@.id==web.generalSnippet\",\"method\":\"GET\"},{\"id\":\"GetMailboxAttribute_web.msgsListDensity\",\"uri\":\"/ws/v3/mailboxes/@.id==" +
            id +
            "/attributes/@.id==web.msgsListDensity\",\"method\":\"GET\"},{\"id\":\"GetMailboxAttribute_web.desktopNotification\",\"uri\":\"/ws/v3/mailboxes/@.id==" +
            id +
            "/attributes/@.id==web.desktopNotification\",\"method\":\"GET\"},{\"id\":\"GetMailboxAttribute_web.showCustomFolders\",\"uri\":\"/ws/v3/mailboxes/@.id==" +
            id +
            "/attributes/@.id==web.showCustomFolders\",\"method\":\"GET\"},{\"id\":\"GetMailboxAttribute_web.showSmartViews\",\"uri\":\"/ws/v3/mailboxes/@.id==" +
            id +
            "/attributes/@.id==web.showSmartViews\",\"method\":\"GET\"},{\"id\":\"GetMailboxAttribute_web.hideAd\",\"uri\":\"/ws/v3/mailboxes/@.id==" +
            id +
            "/attributes/@.id==web.hideAd\",\"method\":\"GET\"},{\"id\":\"GetMailboxAttribute_web.expandedFolders\",\"uri\":\"/ws/v3/mailboxes/@.id==" +
            id +
            "/attributes/@.id==web.expandedFolders\",\"method\":\"GET\"},{\"id\":\"GetMailboxAttribute_web.stationeryTheme\",\"uri\":\"/ws/v3/mailboxes/@.id==" +
            id +
            "/attributes/@.id==web.stationeryTheme\",\"method\":\"GET\"},{\"id\":\"GetMailboxAttribute_web.useRichText\",\"uri\":\"/ws/v3/mailboxes/@.id==" +
            id +
            "/attributes/@.id==web.useRichText\",\"method\":\"GET\"},{\"id\":\"GetMailboxAttribute_web.hideComposeToolbar.gdpr\",\"uri\":\"/ws/v3/mailboxes/@.id==" +
            id +
            "/attributes/@.id==web.hideComposeToolbar.gdpr\",\"method\":\"GET\"},{\"id\":\"GetMailboxAttribute_web.actionAfterMsgMove\",\"uri\":\"/ws/v3/mailboxes/@.id==" +
            id +
            "/attributes/@.id==web.actionAfterMsgMove\",\"method\":\"GET\"},{\"id\":\"GetMailboxAttribute_web.markAsReadInterval\",\"uri\":\"/ws/v3/mailboxes/@.id==" +
            id +
            "/attributes/@.id==web.markAsReadInterval\",\"method\":\"GET\"},{\"id\":\"GetMailboxAttribute_web.systemTabs\",\"uri\":\"/ws/v3/mailboxes/@.id==" +
            id +
            "/attributes/@.id==web.systemTabs\",\"method\":\"GET\"},{\"id\":\"GetMailboxAttribute_web.adBlockFeatureCueId\",\"uri\":\"/ws/v3/mailboxes/@.id==" +
            id +
            "/attributes/@.id==web.adBlockFeatureCueId\",\"method\":\"GET\"},{\"id\":\"GetMailboxAttribute_web.breakingNews\",\"uri\":\"/ws/v3/mailboxes/@.id==" +
            id +
            "/attributes/@.id==web.breakingNews\",\"method\":\"GET\"},{\"id\":\"GetMailboxAttribute_isShipmentTrackingEnabled\",\"uri\":\"/ws/v3/mailboxes/@.id==" +
            id +
            "/attributes/@.id==isShipmentTrackingEnabled\",\"method\":\"GET\"},{\"id\":\"GetMailboxAttribute_dealsStories\",\"uri\":\"/ws/v3/mailboxes/@.id==" +
            id +
            "/attributes/@.id==dealsStories\",\"method\":\"GET\"},{\"id\":\"GetMailboxAttribute_ampEmail\",\"uri\":\"/ws/v3/mailboxes/@.id==" +
            id +
            "/attributes/@.id==ampEmail\",\"method\":\"GET\"},{\"id\":\"GetMailboxAttribute_web.hulk.readabilityMode\",\"uri\":\"/ws/v3/mailboxes/@.id==" +
            id +
            "/attributes/@.id==web.hulk.readabilityMode\",\"method\":\"GET\"},{\"id\":\"GetMailboxAttribute_web.composeFontFamily\",\"uri\":\"/ws/v3/mailboxes/@.id==" +
            id +
            "/attributes/@.id==web.composeFontFamily\",\"method\":\"GET\"},{\"id\":\"GetMailboxAttribute_web.composeFontSize\",\"uri\":\"/ws/v3/mailboxes/@.id==" +
            id +
            "/attributes/@.id==web.composeFontSize\",\"method\":\"GET\"},{\"id\":\"GetMailboxAttribute_web.enableEnhancerLinkPreview\",\"uri\":\"/ws/v3/mailboxes/@.id==" +
            id +
            "/attributes/@.id==web.enableEnhancerLinkPreview\",\"method\":\"GET\"},{\"id\":\"GetMailboxAttribute_web.emojiRecentlyUsed\",\"uri\":\"/ws/v3/mailboxes/@.id==" +
            id +
            "/attributes/@.id==web.emojiRecentlyUsed\",\"method\":\"GET\"},{\"id\":\"GetMailboxAttribute_web.composeMode\",\"uri\":\"/ws/v3/mailboxes/@.id==" +
            id + "/attributes/@.id==web.composeMode\",\"method\":\"GET\"}],\"responseType\":\"json\"}";

    }   
    public static string PrepareMailBoxPayloadSetName(string id ,string randomUserName)
    {
        return
            "{\"requests\":[{\"id\":\"SetDeaPrefix\",\"uri\":\"/ws/v3/mailboxes/@.id==" + id +
            "/attributes/@.id==disposableAddressesPrefix\",\"method\":\"POST\",\"payload\":{\"id\":\"disposableAddressesPrefix\",\"link\":{\"type\":\"RELATIVE\",\"href\":\"/ws/v3/mailboxes/@.id==" +
            id + "/attributes/@.id==disposableAddressesPrefix\"},\"value\":{\"deaPrefix\":\"" +
            randomUserName + "\",\"deaDomain\":\"yahoo.com\"}},\"suppressResponse\":false}],\"responseType\":\"json\"}";

    }  
    public static string GetCorrectFolderOffset(string id,int offset,string folderId)
    {
        return
            "{\"requests\":[{\"id\":\"GetMessageGroupList\",\"uri\":\"/ws/v3/mailboxes/@.id=="+id+"/messages/@.select==q?q=folderId%3A"+folderId+"%20acctId%3A1%20groupBy%3AconversationId%20count%3A100%20offset%3A"+offset+"%20-folderType%3A(SYNC)-folderType%3A(INVISIBLE)%20-sort%3Adate\",\"method\":\"GET\",\"payloadType\":\"embedded\"}],\"responseType\":\"json\"}";
    }
    public static string GetMarkMessagesAsReadBulkPayload(string id,IEnumerable<string> messageIds)
    {
        string listIds = GetIdsChain(new ObservableCollection<string>(messageIds));
        return
        $"{{\"requests\":[{{\"id\":\"UnifiedUpdateMessage_0\",\"uri\":\"/ws/v3/mailboxes/@.id=={id}/messages/@.select==q?q=id%3A({listIds})\",\"method\":\"POST\",\"payloadType\":\"embedded\",\"payload\":{{\"message\":{{\"flags\":{{\"read\":true}}}}}}}}],\"responseType\":\"json\"}}"
        ;

    }
    public static string GetMarkMessageAsReadSSinglePayload(string id,string messageIds)
    {
        return "{\"requests\":[{\"id\":\"UnifiedUpdateMessage_0\",\"uri\":\"/ws/v3/mailboxes/@.id==" +
               id + "/messages/@.select==q?q=id%3A(" + messageIds +
               ")\",\"method\":\"POST\",\"payloadType\":\"embedded\",\"payload\":{\"message\":{\"flags\":{\"read\":true}}}}],\"responseType\":\"json\"}";

    }

    public static string GetArchiveMessageSinglePayload(string id,string messageId)
    {
        return 
            "{\"requests\":[{\"id\":\"UnifiedUpdateMessage_0\",\"uri\":\"/ws/v3/mailboxes/@.id=="+id+"/messages/@.select==q?q=id%3A("+messageId+")\",\"method\":\"POST\",\"payloadType\":\"embedded\",\"payload\":{\"message\":{\"folder\":{\"id\":\"21\"}}}}],\"responseType\":\"json\"}";
    }

    public static string GetArchiveMessageBulkPayload(string id, IEnumerable<string> messageIds)
    {
        string listIds = GetIdsChain(new ObservableCollection<string>(messageIds));
        return
            "{\"requests\":[{\"id\":\"UnifiedUpdateMessage_0\",\"uri\":\"/ws/v3/mailboxes/@.id=="+id+"/messages/@.select==q?q=id%3A("+listIds+")\",\"method\":\"POST\",\"payloadType\":\"embedded\",\"payload\":{\"message\":{\"folder\":{\"id\":\"21\"}}}}],\"responseType\":\"json\"}";
    }
    public static string MoveMessageToTrashSinglePayload(string id,string messageId)
    {
        return
        "{\"requests\":[{\"id\":\"UnifiedUpdateMessage_0\",\"uri\":\"/ws/v3/mailboxes/@.id==" + id + 
            "/messages/@.select==q?q=id%3A("+ messageId + 
            ")\",\"method\":\"POST\",\"payloadType\":\"embedded\",\"payload\":{\"message\":{\"folder\":{\"id\":\"4\"}}}}],\"responseType\":\"json\"}";
        
    }

    public static string MoveMessagesToTrashBulkPayload(string id, IEnumerable<string> messageIds)
    {
        string listIds = GetIdsChain(new ObservableCollection<string>(messageIds));
        return
            "{\"requests\":[{\"id\":\"UnifiedUpdateMessage_0\",\"uri\":\"/ws/v3/mailboxes/@.id=="+id+"/messages/@.select==q?q=id%3A("+listIds+")\",\"method\":\"POST\",\"payloadType\":\"embedded\",\"payload\":{\"message\":{\"folder\":{\"id\":\"4\"}}}}],\"responseType\":\"json\"}";
    }

    #region MNS

    public static string GetMarkMessagesAsNotSpamPayload(string id, IEnumerable<string> messageIds)
    {
        string listIds = GetIdsChain(messageIds);
        return

            "{\"requests\":[{\"id\":\"UnifiedUpdateMessage_0\",\"uri\":\"/ws/v3/mailboxes/@.id=="+id+"/messages/@.select==q?q=id%3A("+listIds+")\",\"method\":\"POST\",\"payloadType\":\"embedded\",\"payload\":{\"message\":{\"flags\":{\"spam\":false},\"folder\":{\"id\":\"1\"}}}}],\"responseType\":\"json\"}";
    }

    public static string GetMarkMessageAsNotSpamPayload(string id, string messageId)
    {
        return 
            "{\"requests\":[{\"id\":\"UnifiedUpdateMessage_0\",\"uri\":\"/ws/v3/mailboxes/@.id=="+id+"/messages/@.select==q?q=id%3A("+messageId+")\",\"method\":\"POST\",\"payloadType\":\"embedded\",\"payload\":{\"message\":{\"flags\":{\"spam\":false},\"folder\":{\"id\":\"1\"}}}}],\"responseType\":\"json\"}";
    }

    #endregion
    
    private static string GetIdsChain(IEnumerable<string> ids)
    {
        string idsChian = "";

        foreach (var value in ids)
        {
            if (value == ids.First())
            {
                idsChian += $"{value}%20";
            }else if (value == ids.Last())
            {
                idsChian += $"{value}";
            }
            else
            {
                idsChian += $"{value}%20";
            }
            
        }
        return idsChian;
    }
}