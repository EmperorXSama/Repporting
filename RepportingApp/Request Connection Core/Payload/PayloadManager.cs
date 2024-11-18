namespace RepportingApp.Request_Connection_Core.Payload;

public static class PayloadManager
{
    public static string GetCorrectFolderPayload(string id,string folderId,string count = "100")
    {
        return
            "{\"requests\":[{\"id\":\"GetMessageGroupList\",\"uri\":\"/ws/v3/mailboxes/@.id=="+id+"/messages/@.select==q?q=folderId%3A"+folderId+"%20acctId%3A1%20groupBy%3AconversationId%20count%3A"+count+"%20offset%3A0%20-folderType%3A(SYNC)-folderType%3A(INVISIBLE)%20-sort%3Adate\",\"method\":\"GET\",\"payloadType\":\"embedded\"},{\"id\":\"UnseenCountReset\",\"uri\":\"/ws/v3/mailboxes/@.id=="+id+"/decos/@.id==FTI\",\"method\":\"POST\",\"payload\":{\"id\":\"FTI\",\"counts\":[{\"accountId\":\"1\",\"unseen\":0}]}}],\"responseType\":\"json\"}";
        
    }
}