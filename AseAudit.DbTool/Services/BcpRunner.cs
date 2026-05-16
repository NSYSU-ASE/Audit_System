using System.Diagnostics;
using System.Text;

namespace AseAudit.DbTool.Services;

public sealed class BcpRunner
{
    public sealed record BcpResult(int ExitCode, string StdOut, string StdErr);

    public bool IsBcpAvailable()
    {
        try
        {
            var psi = new ProcessStartInfo("bcp", "-v")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            proc!.WaitForExit(5000);
            return proc.ExitCode == 0 || proc.ExitCode == 1;
        }
        catch
        {
            return false;
        }
    }

    public BcpResult ExportTable(string databaseName, string tableName, string outputFilePath, string serverInstance)
    {
        return Run([
            $"{databaseName}.dbo.{tableName}",
            "out",
            outputFilePath,
            "-S", serverInstance,
            "-T",   // Trusted connection (Windows auth)
            "-n"    // native format
        ]);
    }

    public BcpResult ImportTable(string databaseName, string tableName, string inputFilePath, string serverInstance)
    {
        return Run([
            $"{databaseName}.dbo.{tableName}",
            "in",
            inputFilePath,
            "-S", serverInstance,
            "-T",
            "-n",
            "-E"    // Keep identity values from file
        ]);
    }

    private static BcpResult Run(string[] args)
    {
        var psi = new ProcessStartInfo("bcp")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var proc = Process.Start(psi) ?? throw new InvalidOperationException("bcp.exe 無法啟動");
        string stdout = proc.StandardOutput.ReadToEnd();
        string stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit();
        return new BcpResult(proc.ExitCode, stdout, stderr);
    }
}
