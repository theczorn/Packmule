using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Packmule.Configuration;
using Packmule.Repositories.MuleRepository;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Packmule
{
    public class ImageUploadChain
    {
        private readonly IComputerVisionClient _visionClient;
        private readonly IMuleRepository _muleRepository;
        private readonly PackmuleConfiguration _packmuleConfiguration;

        public ImageUploadChain(IOptions<PackmuleConfiguration> packmuleConfiguration,
            IMuleRepository muleRepository,
            IComputerVisionClient visionClient
        )
        {
            _muleRepository = muleRepository;
            _packmuleConfiguration = packmuleConfiguration.Value;
            _visionClient = visionClient;
        }

        [FunctionName("PackmuleStarter")]
        public async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")]HttpRequestMessage request,
            [OrchestrationClient]DurableOrchestrationClient starter,
            ILogger log)
        {
            // TODO: Ascertain pass/fail and stash in BLOBS accordingly

            // QOL: Function Proxy/Api
            // QOL: Middleware Response handler?

            var rawImage = await request.Content.ReadAsByteArrayAsync();
            if (rawImage == null || rawImage.Length == 0)
            {
                throw new Exception("No Image in request body, please try again.");
            }

            string instanceId = await starter.StartNewAsync("Chain", rawImage);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(request, instanceId);
        }

        [FunctionName("Chain")]
        public async Task RunOrchestrator(
            [OrchestrationTrigger] DurableOrchestrationContext context)
        {
            var ocrJobUri = await context.CallActivityAsync<string>("SubmitImage", context.GetInput<byte[]>());
            var recipients = await context.CallActivityAsync<IEnumerable<Notifications>>("ParseOcrResults", ocrJobUri);
            await context.CallActivityAsync("SendEmails", recipients);
        }

        [FunctionName("SubmitImage")]
        public async Task<string> SubmitImage([ActivityTrigger] byte[] fileBytes, ILogger log)
        {
            using (var imageStream = new MemoryStream(fileBytes))
            {
                imageStream.Seek(0, SeekOrigin.Begin);

                log.LogInformation("Calling VisionClient to batch OCR job...");
                var result = await _visionClient.RecognizeTextInStreamAsync(imageStream, TextRecognitionMode.Printed);

                if (result?.OperationLocation == null)
                {
                    throw new Exception("OCR not able to stash job.");
                }
                log.LogInformation($"Successful OCR job at {result.OperationLocation}");
                return result.OperationLocation;
            }
        }

        [FunctionName("ParseOcrResults")]
        public async Task<IEnumerable<Notifications>> ParseOcrResults([ActivityTrigger] string ocrJobUri, ILogger log)
        {
            var operationId = ocrJobUri.Substring(ocrJobUri.LastIndexOf('/') + 1);
            Thread.Sleep(3000);
            // QOL: NotStarted/Running should loop back and try again (polly.net?) 	The text recognition process has not started.

            var result = await _visionClient.GetTextOperationResultAsync(operationId);
            if (result.Status == TextOperationStatusCodes.Failed)
            {
                throw new Exception("Text Operation Failed to parse properly, see logs for details.");
            }

            // QOL: Bounding Box prefer "centered" text
            var lines = result.RecognitionResult.Lines
                .Where(x => x.Text.Length < 30)
                .Where(x => Regex.IsMatch(x.Text.Trim(), "^[A-z]+\\s{1}[A-z]+$"))
                .ToList();

            return await _muleRepository.GetUsersNeedingNotification(lines);
        }

        [FunctionName("SendEmails")]
        public async Task SendEmails([ActivityTrigger] IEnumerable<Notifications> recipients, ILogger log)
        {
            foreach (var recipient in recipients)
            {
                try
                {
                    var client = new SendGridClient(_packmuleConfiguration.SendgridApiKey);
                    var msg = new SendGridMessage()
                    {
                        From = new EmailAddress("bigburro@donotreply.com", "Packmule Mailbot"),
                        Subject = "[Packmule] You've Got Mail!",
                        PlainTextContent = $"Hello there {recipient.FirstName},\r\n\r\nYou've got a letter or package on the 38th floor!\r\n\r\n<3 Packmule"
                    };
                    msg.AddTo(new EmailAddress(recipient.EmailAddress, recipient.FullName));
                    var response = await client.SendEmailAsync(msg);
                }
                // QOL: Flag failing entities into blob stash
                catch (Exception ex)
                {
                    throw ex;
                }
            }

            await _muleRepository.UpdateUsersSentNotification(recipients);
        }
    }
}