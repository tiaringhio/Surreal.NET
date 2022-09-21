using System.Text.RegularExpressions;

namespace SurrealDB.Driver.Tests; 

/// <summary>
/// Static class that returns the list of processes and the ports those processes use.
/// </summary>
public static class PortHelper {
    private static long s_cacheTicks;
    private static volatile List<ProcessPort>? s_cacheContent;
    private static readonly object s_cacheLock = new();


    /// <summary>
    /// This method distills the output from netstat -a -n -o into a list of ProcessPorts that provide a mapping between
    /// the process (name and id) and the ports that the process is using.
    /// </summary>
    /// <returns></returns>
    public static ValueTask<List<ProcessPort>> GetPortMapping(CancellationToken ct = default) {
        long nowTicks;
        lock (s_cacheLock) {
            nowTicks = Environment.TickCount64;
            List<ProcessPort>? content = s_cacheContent;
            if (nowTicks - s_cacheTicks <= 50 && content is not null) {
                return ValueTask.FromResult(content);
            }
        }

        return new(GetPortMappingAndUpdate(nowTicks, ct));
    }

    private static async Task<List<ProcessPort>> GetPortMappingAndUpdate(long nowTicks, CancellationToken ct) {
        List<ProcessPort> content = await GetPortMappingCore(ct);
        lock (s_cacheLock) {
            s_cacheTicks = nowTicks;
            s_cacheContent = content;
            return content;
        }
    }


    public static async Task<ProcessPort> ByPort(int port, CancellationToken ct = default) {
        List<ProcessPort> mapping = await GetPortMapping(ct);
        foreach (ProcessPort processPort in mapping) {
            if (processPort.PortNumber == port) {
                return processPort;
            }
        }

        return default;
    }

    private static async Task<List<ProcessPort>> GetPortMappingCore(CancellationToken ct) {
        if (Environment.OSVersion.Platform == PlatformID.Unix) {
            return await PortMappingCoreUnix(ct);
        }
        
        return await PortMappingCoreWin(ct);
    }

    private static async Task<List<ProcessPort>> PortMappingCoreUnix(CancellationToken ct) {
        List<ProcessPort> ports = new();
        Process? p = Process.Start(
            new ProcessStartInfo {
                FileName = "netstat",
                Arguments = "-lptn",
                RedirectStandardInput = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            }
        );

        if (p is null) {
            throw new InvalidOperationException("Process could not be started");
        }
        //await p.WaitForExitAsync(ct);
        p.WaitForExit();

        StreamReader stdOut = p.StandardOutput;
        StreamReader stdErr = p.StandardError;

        string content = await stdOut.ReadToEndAsync() + await stdErr.ReadToEndAsync();
        int existCode = p.ExitCode;
        
        if (existCode != 0) {
            throw new InvalidOperationException("NetStat command failed. This may require elevated permissions.");
        }

        string[] netStatRows = Regex.Split(content, "\n");
        foreach (string netStatRow in netStatRows) {
            Match tkn = Regex.Match(netStatRow, @"^([\w\d]+)\s*(\d+)\s*(\d+)\s*([\d\.\:]+?)([\d\*]+)\s*([\d\.\:]+?)([\d\*]+)\s*(\w+)\s*(\d+)\/(.*)");
            if (!tkn.Success) {
                continue;
            }

            try {
                ProcessPort pp = new(
                    tkn.Groups[10].Value,
                    Int32.Parse(tkn.Groups[9].Value),
                    tkn.Groups[1].Value,
                     Int32.Parse(tkn.Groups[5].Value)
                );

                if (!pp.IsDefault) {
                    ports.Add(pp);
                }
            } catch (Exception) {
                // Discard
            }
        }

        return ports;
    }

    private static async Task<List<ProcessPort>> PortMappingCoreWin(CancellationToken ct) {
        List<ProcessPort> ports = new();
        Process? p = Process.Start(
            new ProcessStartInfo {
                FileName = "netstat",
                Arguments = "-n -o",
                RedirectStandardInput = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            }
        );

        if (p is null) {
            throw new InvalidOperationException("Process could not be started");
        }
        //await p.WaitForExitAsync(ct);

        StreamReader stdOut = p.StandardOutput;
        StreamReader stdErr = p.StandardError;

        string content = await stdOut.ReadToEndAsync() + await stdErr.ReadToEndAsync();
        int existCode = p.ExitCode;

        if (existCode != 0) {
            throw new InvalidOperationException("NetStat command failed. This may require elevated permissions.");
        }

        string[] netStatRows = Regex.Split(content, "\n");

        foreach (string netStatRow in netStatRows) {
            string[] tkn = Regex.Split(netStatRow, "\\s+");
            if (tkn.Length <= 4
             || (!tkn[1].Equals("UDP", StringComparison.OrdinalIgnoreCase)
                 && !tkn[1].Equals("TCP", StringComparison.OrdinalIgnoreCase))) {
                continue;
            }

            try {
                string ipAddress = Regex.Replace(tkn[2], @"\[(.*?)\]", "0.0.0.0");
                ProcessPort pp = new(
                    tkn[1] == "UDP" ? GetProcessName(Convert.ToInt16(tkn[4])) : GetProcessName(Convert.ToInt16(tkn[5])),
                    tkn[1] == "UDP" ? Convert.ToInt16(tkn[4]) : Convert.ToInt16(tkn[5]),
                    ipAddress.Contains("1.1.1.1") ? $"{tkn[1]}v6" : $"{tkn[1]}v4",
                    Convert.ToInt32(ipAddress.Split(':')[1])
                );

                if (!pp.IsDefault) {
                    ports.Add(pp);
                }
            } catch (Exception) {
                // Discard
            }
        }

        return ports;
    }

    /// <summary>
    /// Private method that handles pulling the process name (if one exists) from the process id.
    /// </summary>
    /// <param name="processId"></param>
    /// <returns></returns>
    private static string GetProcessName(int processId)
    {
        string procName = "UNKNOWN";

        try {
            procName = Process.GetProcessById(processId).ProcessName;
        } catch {
            // Discard
        }

        return procName;
    }
}

/// <summary>
/// A mapping for processes to ports and ports to processes that are being used in the system.
/// </summary>
public readonly record struct ProcessPort
{
    private readonly string _processName;
    private readonly int _processId;
    private readonly string _protocol;
    private readonly int _portNumber;

    /// <summary>
    /// Internal constructor to initialize the mapping of process to port.
    /// </summary>
    /// <param name="processName">Name of process to be </param>
    /// <param name="processId"></param>
    /// <param name="protocol"></param>
    /// <param name="portNumber"></param>
    internal ProcessPort(string processName, int processId, string protocol, int portNumber)
    {
        _processName = processName;
        _processId = processId;
        _protocol = protocol;
        _portNumber = 0;
        _portNumber = portNumber;
    }

    public bool IsDefault => this == default;

    public string ProcessPortDescription => $"{_processName} ({_protocol} port {_portNumber} pid {_processId})";
    public string ProcessName => _processName;
    public int ProcessId => _processId;
    public string Protocol => _protocol;
    public int PortNumber => _portNumber;
}
