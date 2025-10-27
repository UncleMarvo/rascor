using Rascor.Domain;
using Rascor.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Rascor.Infrastructure;

public class MockPushProvider : IPushProvider
{
    private readonly ILogger<MockPushProvider> _logger;

    public MockPushProvider(ILogger<MockPushProvider> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string userId, string title, string message, CancellationToken ct = default)
    {
        _logger.LogInformation("[MOCK PUSH] To: {UserId} | Title: {Title} | Message: {Message}", 
            userId, title, message);
        return Task.CompletedTask;
    }
}

public class ConsoleEmailProvider : IEmailProvider
{
    private readonly ILogger<ConsoleEmailProvider> _logger;

    public ConsoleEmailProvider(ILogger<ConsoleEmailProvider> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string toEmail, string subject, string body, CancellationToken ct = default)
    {
        _logger.LogInformation("[CONSOLE EMAIL] To: {Email} | Subject: {Subject} | Body: {Body}", 
            toEmail, subject, body);
        return Task.CompletedTask;
    }
}

// TODO: Stubs for OneSignal/SES - implement when needed
public class OneSignalPushProvider : IPushProvider
{
    public Task SendAsync(string userId, string title, string message, CancellationToken ct = default)
    {
        throw new NotImplementedException("OneSignal integration pending");
    }
}

public class AwsSesEmailProvider : IEmailProvider
{
    public Task SendAsync(string toEmail, string subject, string body, CancellationToken ct = default)
    {
        throw new NotImplementedException("AWS SES integration pending");
    }
}
