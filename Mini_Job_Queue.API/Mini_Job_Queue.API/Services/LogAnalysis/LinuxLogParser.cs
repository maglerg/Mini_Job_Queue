using System.Text.RegularExpressions;
using Mini_Job_Queue.API.Models.LogAnalysis;

namespace Mini_Job_Queue.API.Services.LogAnalysis
{
    public class LinuxLogParser
    {
        private static readonly Regex LogRegex = new(
            @"^(?<timestamp>[A-Z][a-z]{2}\s+\d{1,2}\s+\d{2}:\d{2}:\d{2})\s+" +
            @"(?<host>\S+)\s+" +
            @"(?<service>[^:\s]+)" +
            @"(?:\[(?<pid>\d+)\])?:\s*" +
            @"(?<message>.*)$",
            RegexOptions.Compiled);

        private static readonly Regex RemoteHostRegex = new(
            @"(?:^|\s)rhost=(?<host>\S*)",
            RegexOptions.Compiled);

        public bool TryParse(
            string line,
            out ParsedLinuxLog? parsedLog)
        {
            parsedLog = null;

            if (string.IsNullOrWhiteSpace(line))
                return false;

            var match = LogRegex.Match(line);

            if (!match.Success)
                return false;

            int? processId = null;

            if (match.Groups["pid"].Success)
            {
                if (!int.TryParse(
                    match.Groups["pid"].Value,
                    out var parsedPid))
                {
                    return false;
                }

                processId = parsedPid;
            }

            var message = match.Groups["message"].Value;

            var remoteHostMatch = RemoteHostRegex.Match(message);

            string? remoteHost = null;

            if (remoteHostMatch.Success &&
                !string.IsNullOrWhiteSpace(
                    remoteHostMatch.Groups["host"].Value))
            {
                remoteHost = remoteHostMatch.Groups["host"].Value;
            }

            parsedLog = new ParsedLinuxLog
            {
                Timestamp = match.Groups["timestamp"].Value,
                Host = match.Groups["host"].Value,
                Service = match.Groups["service"].Value,
                ProcessId = processId,
                Message = message,
                RemoteHost = remoteHost
            };

            return true;
        }
    }
}