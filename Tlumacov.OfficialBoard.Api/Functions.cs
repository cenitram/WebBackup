using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;
using Tlumacov.OfficialBoard.Shared;
using Tlumacov.OfficialBoard.Shared.Options;
using Tlumacov.OfficialBoard.Shared.Repositories;

namespace Tlumacov.OfficialBoard.Api;

public class Functions(
    ILoggerFactory loggerFactory,
    IRecipientsRepository recipientsRepository,
    IEmailSender emailSender,
    IOptions<SshOptions> sshOptions,
    ITokenService tokenService)
{
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

            var verificationResult = tokenService.VerifyToken(data.Token);
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

    [Function("Unsubscribe")]
    public async Task<HttpResponseData> Unsubscribe(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "subscription/unsubscribe")] HttpRequestData req)
    {
        var logger = loggerFactory.CreateLogger("Unsubscribe");
        var connector = new SshConnector();
        try
        {
            connector.Connect(sshOptions.Value);
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonSerializer.Deserialize<UnsubscribeRequest>(requestBody, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

            if (data is null || string.IsNullOrWhiteSpace(data.Email) || string.IsNullOrWhiteSpace(data.Token))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid request body. Email and token are required.");
                return badResponse;
            }

            // First verify the token
            var verificationResult = tokenService.VerifyToken(data.Token);
            if (!verificationResult.IsValid)
            {
                var invalidResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await invalidResponse.WriteStringAsync(verificationResult.Message);
                return invalidResponse;
            }

            // Make sure the email in the token matches the one in the request
            if (!string.Equals(verificationResult.Email, data.Email, StringComparison.OrdinalIgnoreCase))
            {
                var invalidResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await invalidResponse.WriteStringAsync("Token email does not match the provided email.");
                return invalidResponse;
            }

            // Update the recipient's status to 'unsubscribed'
            var unsubscribeResult = recipientsRepository.UnsubscribeRecipient(verificationResult.Email);

            if (unsubscribeResult)
                logger.LogInformation("Email successfully unsubscribed: {Email}", verificationResult.Email);
            else
                logger.LogWarning("Failed to unsubscribe or email not found: {Email}", verificationResult.Email);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                Success = unsubscribeResult,
                Message = unsubscribeResult
                    ? "You have been successfully unsubscribed from notifications"
                    : "Unable to complete unsubscribe process. You may have already unsubscribed or the email was not found."
            });
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing unsubscribe request");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync("An error occurred while processing your unsubscribe request.");
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

    private class VerifyTokenRequest
    {
        public string? Token { get; set; }
    }

    private class UnsubscribeRequest
    {
        public string? Email { get; set; }
        public string? Token { get; set; }
    }
}