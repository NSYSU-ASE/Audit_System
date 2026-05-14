namespace AseAudit.DbTool;

public static class ExitCodes
{
    public const int Success           = 0;
    public const int GeneralError      = 1;
    public const int EnvironmentError  = 2;  // LocalDB / 連線
    public const int DependencyMissing = 3;  // bcp.exe
    public const int ConfigInvalid     = 4;  // manifest 不合法
    public const int UserCancelled     = 5;
    public const int DbStateUnexpected = 6;
    public const int Interrupted       = 130; // Ctrl+C
}
