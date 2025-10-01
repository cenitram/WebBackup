using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OfficialBoardMailing.Options;
using OfficialBoardMailing.Repositories;
using System.Net;
using System.Text.Json;

namespace OfficialBoardMailing;

public class Functions(
    ILoggerFactory loggerFactory,
    IOfficialBoardRepository officialBoardRepository,
    IRecipientsRepository recipientsRepository,
    IEmailSender emailSender,
    IOptions<SshOptions> sshOptions,
    TokenVerificationService tokenVerificationService)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<Functions>();

    [Function("MailNewFilesInOfficialBoard")]
    public async Task Run([TimerTrigger("0 0 18 * * *", RunOnStartup = false)] TimerInfo myTimer)
    {
        _logger.LogInformation("C# Timer trigger function executed at: {DateTimeNow}", DateTime.Now);

        if (myTimer.ScheduleStatus is not null)
        {
            _logger.LogInformation("Next timer schedule at: {NextSchedule}", myTimer.ScheduleStatus.Next);
        }
        var connector = new SshConnector();
        try
        {
            connector.Connect(sshOptions.Value);
            _logger.LogInformation("SSH tunnel with port forwarding established successfully.");

            var unsentDocuments = officialBoardRepository.GetUnsentDocuments();
            if (!unsentDocuments.Any())
            {
                _logger.LogInformation("No unsent documents found.");
                return;
            }

            var recipients = recipientsRepository.GetRecipientsEmails();

            if (!recipients.Any())
            {
                _logger.LogWarning("No email recipients provided. Skipping email send and not marking documents as sent.");
                return;
            }

            await emailSender.SendUnsentDocumentsAsync(unsentDocuments, recipients);

            officialBoardRepository.MarkDocumentsAsSent(unsentDocuments.Select(d => d.Id));

            _logger.LogInformation("Official board files processed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError("Error establishing SSH tunnel: {ExceptionMessage}", ex.Message);
        }
        finally
        {
            connector.Disconnect();
        }
    }

    [Function("RegisterSubscriber")]
    public async Task<HttpResponseData> RegisterSubscriber(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "register-subscriber")] HttpRequestData req)
    {
        var logger = loggerFactory.CreateLogger("RegisterSubscriber");
        var connector = new SshConnector();
        try
        {
            connector.Connect(sshOptions.Value);
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonSerializer.Deserialize<RegisterSubscriberRequest>(requestBody);
            if (data is null || string.IsNullOrWhiteSpace(data.Email))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid request body or missing email.");
                return badResponse;
            }

            var result = recipientsRepository.AddRecipient(data.Email);
            if (!result)
            {
                var conflictResponse = req.CreateResponse(HttpStatusCode.Conflict);
                await conflictResponse.WriteStringAsync("Subscriber already exists.");
                return conflictResponse;
            }

            // Send confirmation email
            await emailSender.SendConfirmationEmailAsync(data.Email);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync("Subscriber registered successfully. Confirmation email sent.");
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error registering subscriber");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync("Error registering subscriber.");
            return errorResponse;
        }
        finally
        {
            connector.Disconnect();
        }
    }

    [Function("VerifyToken")]
    public async Task<HttpResponseData> VerifyToken(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "subscription/verify-token")] HttpRequestData req)
    {
        var logger = loggerFactory.CreateLogger("VerifyToken");
        var connector = new SshConnector();
        try
        {
            connector.Connect(sshOptions.Value);
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonSerializer.Deserialize<VerifyTokenRequest>(requestBody, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
            if (data is null || string.IsNullOrWhiteSpace(data.Token))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid request body or missing token.");
                return badResponse;
            }

            var verificationResult = tokenVerificationService.VerifyToken(data.Token);
            if (!verificationResult.IsValid)
            {
                var invalidResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await invalidResponse.WriteStringAsync(verificationResult.Message);
                return invalidResponse;
            }

            // If token is valid, update the subscriber status to 'subscribed'
            var confirmationResult = recipientsRepository.ConfirmSubscription(verificationResult.Email);

            if (confirmationResult)
                logger.LogInformation("Subscription successfully confirmed for email: {Email}", verificationResult.Email);
            else
                logger.LogInformation("Subscription already confirmed for email: {Email}", verificationResult.Email);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                IsValid = true,
                Message = "Your subscription has been confirmed successfully",
                Email = verificationResult.Email
            });
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error verifying token");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync("Error verifying token.");
            return errorResponse;
        }
        finally
        {
            connector.Disconnect();
        }
    }

    private class RegisterSubscriberRequest
    {
        public string? Email { get; set; }
    }
}
