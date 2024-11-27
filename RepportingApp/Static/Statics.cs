namespace RepportingApp.Static;

public static  class Statics
{
    // colors
    public static string UploadFileColor { get; } = "#FF0000";
    public static string UploadFileSoftColor { get; } ="#FFFFFF";
    public static string ReportingColor { get; } ="#55C1FB";
    public static string ReportingSoftColor { get; } ="#b1e4ff";




    #region api statics

    public const string InboxDir = "1";
    public const string SentDir = "2";
    public const string SpamDir = "6";
    public const string TrashDir = "4";
    public const string ArchiveDir = "21";
    public const string DraftDir = "3";

    #endregion
    
    #region file Statics

    public static string GetDesktopFilePath()
    {
        return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
    }
    #endregion
}