using Mini_Job_Queue.API.Models.Enums;
using Mini_Job_Queue.API.Models.LogAnalysis;

namespace Mini_Job_Queue.API.Services.LogAnalysis
{
    public class LogAnalysisService
    {
        private readonly LinuxLogParser _parser;
        private readonly LinuxLogClassifier _classifier;

        public LogAnalysisService(
            LinuxLogParser parser,
            LinuxLogClassifier classifier)
        {
            _parser = parser;
            _classifier = classifier;
        }

        public LogAnalysisSummary Analyze(string sourceText)
        {
            var summary = new LogAnalysisSummary();

            if (string.IsNullOrWhiteSpace(sourceText))
                return summary;

            // Убираем только завершающие переводы строк,
            // чтобы последняя пустая строка не считалась ошибкой.
            var normalizedText = sourceText.TrimEnd('\r', '\n');

            if (normalizedText.Length == 0)
                return summary;

            var lines = normalizedText.Split(
                new[] { "\r\n", "\n", "\r" },
                StringSplitOptions.None);

            summary.TotalLines = lines.Length;

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) ||
                    !_parser.TryParse(line, out var parsedLog) ||
                    parsedLog is null)
                {
                    summary.InvalidLines++;
                    continue;
                }

                summary.ParsedLines++;

                var category = _classifier.Classify(parsedLog);

                // Считаем события по категориям.
                summary.CategoryCounts.TryGetValue(
                    category,
                    out var categoryCount);

                summary.CategoryCounts[category] = categoryCount + 1;

                // Считаем частоту полных текстов сообщений.
                var message = parsedLog.Message.Trim();

                summary.TopMessages.TryGetValue(
                    message,
                    out var messageCount);

                summary.TopMessages[message] = messageCount + 1;

                // Отдельно считаем источники ошибок аутентификации.
                if (category == LinuxLogCategory.AuthenticationFailure &&
                    !string.IsNullOrWhiteSpace(parsedLog.RemoteHost))
                {
                    var remoteHost = parsedLog.RemoteHost;

                    summary.FailedAuthenticationByHost.TryGetValue(
                        remoteHost,
                        out var hostCount);

                    summary.FailedAuthenticationByHost[remoteHost] =
                        hostCount + 1;
                }
            }

            // Оставляем десять наиболее частых сообщений.
            summary.TopMessages = summary.TopMessages
                .OrderByDescending(item => item.Value)
                .ThenBy(item => item.Key)
                .Take(10)
                .ToDictionary(item => item.Key, item => item.Value);

            return summary;
        }
    }
}