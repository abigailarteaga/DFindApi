using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace DFindApi.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;

        public EmailService(IConfiguration config, HttpClient httpClient)
        {
            _config = config;
            _httpClient = httpClient;
        }

        public async Task EnviarCorreoAsync(string destinatario, string asunto, string cuerpoHtml)
        {
            var apiKey = _config["SendGrid:ApiKey"];
            var from   = _config["SendGrid:From"];

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("SendGrid:ApiKey no está configurado.");
            if (string.IsNullOrWhiteSpace(from))
                throw new InvalidOperationException("SendGrid:From no está configurado.");

            string fromEmail = from;
            string fromName = "DFind";

            var ltIndex = from.IndexOf('<');
            var gtIndex = from.IndexOf('>');
            if (ltIndex >= 0 && gtIndex > ltIndex)
            {
                fromName = from.Substring(0, ltIndex).Trim();
                fromEmail = from.Substring(ltIndex + 1, gtIndex - ltIndex - 1).Trim();
            }

            var url = "https://api.sendgrid.com/v3/mail/send";

            var payload = new
            {
                personalizations = new[]
                {
                    new
                    {
                        to = new[]
                        {
                            new { email = destinatario }
                        }
                    }
                },
                from = new
                {
                    email = fromEmail,
                    name = fromName
                },
                subject = asunto,
                content = new[]
                {
                    new
                    {
                        type = "text/html",
                        value = cuerpoHtml
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload);

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);

            Console.WriteLine($"[SENDGRID] Enviando correo a {destinatario}...");

            var response = await _httpClient.SendAsync(request);

            Console.WriteLine($"[SENDGRID] StatusCode={(int)response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[SENDGRID ERROR] {body}");
                throw new Exception($"Error enviando correo via SendGrid: {(int)response.StatusCode} {body}");
            }
        }
    }
}
