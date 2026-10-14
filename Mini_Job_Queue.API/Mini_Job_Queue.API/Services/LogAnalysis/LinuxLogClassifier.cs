using Mini_Job_Queue.API.Models.Enums;
using Mini_Job_Queue.API.Models.LogAnalysis;

namespace Mini_Job_Queue.API.Services.LogAnalysis
{
    public class LinuxLogClassifier
    {
        public LinuxLogCategory Classify(ParsedLinuxLog log)
        {
            var message = log.Message.ToLowerInvariant();
            var service = log.Service.ToLowerInvariant();

            // Ошибки аутентификации
            if (message.Contains("authentication failure") ||
                message.Contains("failed password") ||
                message.Contains("user unknown") ||
                message.Contains("invalid user"))
            {
                return LinuxLogCategory.AuthenticationFailure;
            }

            // Системные ошибки и аварийные события
            if (message.Contains("exited abnormally") ||
                message.Contains("fatal") ||
                message.Contains("critical") ||
                message.Contains("alert"))
            {
                return LinuxLogCategory.SystemError;
            }

            // Открытие и закрытие пользовательских сессий
            if (message.Contains("session opened") ||
                message.Contains("session closed"))
            {
                return LinuxLogCategory.UserSession;
            }

            // Сетевые подключения
            if (message.Contains("connection from") ||
                service.Contains("ftpd"))
            {
                return LinuxLogCategory.NetworkConnection;
            }

            return LinuxLogCategory.Other;
        }
    }
}