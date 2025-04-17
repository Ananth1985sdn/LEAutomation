using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
//using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using LEAutomation.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace LEAutomation.DocumentHandlers
{
    public class EncompassToLauraMacUploader
    {
        private static readonly HttpClient httpClient = new HttpClient();
        [FunctionName("DocumentTransferFunction")]
        public async Task Run([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            string token = await GetEncompassAccessTokenAsync(log);

            if (!string.IsNullOrEmpty(token))
            {
                await CallLoanPipelineApiAsync(token, log);
            }
            else
            {
                log.LogError("Failed to retrieve Encompass access token.");
            }
        }

        public async Task<string> GetEncompassAccessTokenAsync(ILogger log)
        {
            var encompassBaseURL = Environment.GetEnvironmentVariable("EncompassApiBaseURL");
            var tokenUrl = Environment.GetEnvironmentVariable("EncompassTokenUrl");

            var clientId = Environment.GetEnvironmentVariable("EncompassClientId");
            var clientSecret = Environment.GetEnvironmentVariable("EncompassClientSecret");
            var username = Environment.GetEnvironmentVariable("EncompassUsername");
            var password = Environment.GetEnvironmentVariable("EncompassPassword");
            var fullUrl = $"{encompassBaseURL.TrimEnd('/')}{tokenUrl}";

            using var client = new HttpClient();

            var requestBody = new FormUrlEncodedContent(new[]
             {
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("username", username),
                new KeyValuePair<string, string>("password", password),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret),
                new KeyValuePair<string, string>("scope", "lp")
            });


            var response = await client.PostAsync(fullUrl, requestBody);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                dynamic obj = JsonConvert.DeserializeObject(json);
                return obj.access_token;
            }

            var error = await response.Content.ReadAsStringAsync();
            log.LogError($"Failed to get token: {error}");
            return null;
        }

        public async Task CallLoanPipelineApiAsync(string token, ILogger log)
        {
            var encompassBaseURL = Environment.GetEnvironmentVariable("EncompassApiBaseURL");
            var pipeLineUrl = Environment.GetEnvironmentVariable("EncompassLoanPipelineURL");
            var requestUrl = $"{encompassBaseURL.TrimEnd('/')}{pipeLineUrl}";

            var documentPackage = Environment.GetEnvironmentVariable("DocumentPackageName");

            var filterTerms = new List<FilterTerm>
            {
                new FilterTerm {
                    canonicalName = "Loan.CurrentMilestoneName",
                    value = new[] { "Started" },
                    matchType = "MultiValue",
                    include = true
                },
                new FilterTerm {
                    canonicalName = "Loan.LoanNumber",
                    value = "5",
                    matchType = "startsWith",
                    include = false
                },
                new FilterTerm {
                    canonicalName = "Fields.CX.DUEDILIGENCE_START_DT",
                    value = "04/11/2025",
                    matchType = "Equals",
                    precision = "Day"
                },
                new FilterTerm {
                    canonicalName = "Fields.CX.NAME_DDPROVIDER",
                    value = "Canopy",
                    matchType = "Exact",
                    include = true
                }
            };

            var requestBody = new
            {
                fields = new[]
                {
                    "Loan.LoanNumber", "Fields.19", "Fields.608", "Loan.LoanAmount", "Loan.LTV", "Fields.976",
                    "Loan.Address1", "Loan.City", "Loan.State", "Fields.15", "Fields.1041", "Loan.OccupancyStatus",
                    "Fields.1401", "Fields.CX.VP.DOC.TYPE", "Fields.4000", "Fields.4002", "Fields.CX.CREDITSCORE",
                    "Fields.325", "Fields.3", "Fields.742", "Fields.CX.VP.BUSINESS.PURPOSE", "Fields.1550",
                    "Fields.675", "Fields.QM.X23", "Fields.QM.X25", "Fields.2278"
                },
                filter = new
                {
                    @operator = "and",
                    terms = filterTerms
                },
                orgType = "internal",
                loanOwnership = "AllLoans",
                sortOrder = new[]
                {
                    new {
                        canonicalName = "Loan.LastModified",
                        order = "Descending"
                    }
                }
            };

            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await httpClient.PostAsync(requestUrl, content);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                log.LogInformation("Loan Pipeline Response: " + result);

                try
                {
                    var loans = JsonConvert.DeserializeObject<List<Loan>>(result);
                    log.LogInformation($"Number of Loans: {loans.Count}");
                    
                    var documentsHttpClient = new HttpClient();
                    documentsHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    documentsHttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var baseUrl = Environment.GetEnvironmentVariable("EncompassApiBaseURL");
                    var endpointTemplate = Environment.GetEnvironmentVariable("EncompassGetDocumentsURL");

                    foreach (var loan in loans)
                    {
                        log.LogInformation($"Loan ID: {loan.LoanId}, Loan Number: {loan.Fields.LoanNumber}, Amount: {loan.Fields.LoanAmount}");

                        var endpoint = endpointTemplate.Replace("{loanId}", loan.LoanId);
                        var fullUrl = $"{baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";

                        var documentsResponse = await httpClient.GetAsync(fullUrl);

                        if (response.IsSuccessStatusCode)
                        {
                            var documentsResult = await documentsResponse.Content.ReadAsStringAsync();
                            log.LogInformation($"Attachments for Loan {loan.Fields.LoanNumber}: {documentsResult}");

                            var attachments = JsonConvert.DeserializeObject<List<Attachment>>(documentsResult);

                            foreach (var attachment in attachments)
                            {
                                if (attachment.AssignedTo?.EntityName != documentPackage || (attachment.FileSize <= 0 || attachment.Type != "Image"))
                                    continue;
                                else
                                {
                                   log.LogInformation($"Attachment Title: {attachment.Title}, CreatedBy: {attachment.AssignedTo?.EntityName}, File Size: {attachment.FileSize}");
                                   var url = await GetDocumentURL(loan.LoanId,attachment.Id,token);
                                    {
                                        var url = await GetDocumentURL(loan.LoanId, attachment.Id, token);
                                        if (url != null)
                                            await DownloadDocument(loan.LoanId, loan.Fields.Field4002, url, log);
                                    break;
                                    continue;
                                }
                            }
                        }
                        else
                        {
                            var error = await response.Content.ReadAsStringAsync();
                            log.LogError($"Failed to fetch attachments for Loan {loan.Fields.LoanNumber}: {error}");
                        }
                    }

                }
                catch (JsonException ex)
                {
                    log.LogError($"Error deserializing response: {ex.Message}");
                }
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                log.LogError($"Loan Pipeline API failed with {response.StatusCode}: {error}");
            }
        }

        private async Task DownloadDocument(string loanId,string lastName, string documentURL, ILogger log)
        {

            if (string.IsNullOrWhiteSpace(documentURL))
            {
                throw new ArgumentException("Document URL cannot be null or empty.", nameof(documentURL));
            }

            using (var httpClient = new HttpClient())
            {

                var request = new HttpRequestMessage(HttpMethod.Get, documentURL);
                var response = await httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    throw new Exception($"Failed to download document. Status: {response.StatusCode}, Response: {errorContent}");
                }
                var contentType = response.Content.Headers.ContentType?.MediaType;
                log.LogInformation($"Content-Type: {contentType}");
                var pdfBytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

                var fileName = loanId+"_"+lastName+"_shippingfiles.pdf";

                #if DEBUG
                // Local development - use project Downloads folder
                var downloadsPath = Path.Combine(Directory.GetCurrentDirectory(), "Downloads");
                #else
                var downloadsPath = Path.Combine(Path.GetTempPath(), "Downloads");
                #endif

                if (!Directory.Exists(downloadsPath))
                {
                    Directory.CreateDirectory(downloadsPath);
                }

                var filePath = Path.Combine(downloadsPath, fileName);

                await File.WriteAllBytesAsync(filePath, pdfBytes).ConfigureAwait(false);
                                        
                Console.WriteLine($"PDF downloaded successfully to: {filePath}");
            }
        }

        private async Task<string> GetDocumentURL(string loanId, string attachmentId, string accessToken)
        {
            var encompassBaseURL = Environment.GetEnvironmentVariable("EncompassApiBaseURL");
            var documentURL = Environment.GetEnvironmentVariable("EncompassGetDocumentURL");

            if (string.IsNullOrWhiteSpace(encompassBaseURL) || string.IsNullOrWhiteSpace(documentURL))
            {
                throw new InvalidOperationException("Missing environment variables for Encompass API base URL or document URL endpoint.");
            }

            var documentURLEndpoint = documentURL.Replace("{loanId}", loanId);
            var requestUrl = $"{encompassBaseURL.TrimEnd('/')}{documentURLEndpoint}";

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var payload = new
                {
                    attachments = new[] { attachmentId }
                };

                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(requestUrl, content).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    throw new Exception($"Failed to get document URL. Status: {response.StatusCode}, Response: {error}");
                }

                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var responseObject = JsonConvert.DeserializeObject<DownloadUrlResponse>(json);

                if (responseObject?.Attachments == null || responseObject.Attachments.Count == 0)
                {
                    throw new Exception("No attachments found in the response.");
                }

                var attachment = responseObject.Attachments[0];
                var pages = attachment?.Pages;

                if (pages != null && pages.Count > 0)
                {
                    if (pages.Count == 1)
                    {
                        return pages[0].Url;
                    }
                    else
                    {
                        return attachment.originalUrls?.ToString() ?? throw new Exception("Original URLs not found.");
                        return pages[0].Url;
                    }
                }

                throw new Exception("No pages found for the attachment.");
            }
        }
        
    }
}
