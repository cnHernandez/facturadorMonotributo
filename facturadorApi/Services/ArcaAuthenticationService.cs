using System.Globalization;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Back.Conf;
using Back.DTOs;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Back.Services
{
    public class ArcaAuthenticationService : IArcaAuthenticationService
    {
        private const string CacheKeyPrefix = "arca-auth-";
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        private readonly ArcaOptions _options;
        private readonly SemaphoreSlim _ticketLock = new(1, 1);

        public ArcaAuthenticationService(
            IHttpClientFactory httpClientFactory,
            IMemoryCache cache,
            IOptions<ArcaOptions> options)
        {
            _httpClientFactory = httpClientFactory;
            _cache = cache;
            _options = options.Value;
        }

        public async Task<ArcaAuthenticationResponseDTO> AuthenticateAsync(CancellationToken cancellationToken = default)
        {
            var ticket = await GetTicketAsync(cancellationToken);
            return new ArcaAuthenticationResponseDTO
            {
                Service = _options.Service,
                ExpirationTimeUtc = ticket.ExpirationTimeUtc
            };
        }

        public async Task<ArcaAuthenticationTicket> GetTicketAsync(CancellationToken cancellationToken = default)
        {
            ValidateOptions();
            var cacheKey = CacheKeyPrefix + _options.Service;
            if (_cache.TryGetValue(cacheKey, out ArcaAuthenticationTicket? cached) &&
                cached!.ExpirationTimeUtc > DateTime.UtcNow.AddMinutes(5))
            {
                return cached;
            }

            await _ticketLock.WaitAsync(cancellationToken);
            try
            {
                if (_cache.TryGetValue(cacheKey, out cached) &&
                    cached!.ExpirationTimeUtc > DateTime.UtcNow.AddMinutes(5))
                {
                    return cached;
                }

                var persisted = LoadPersistedTicket();
                if (persisted != null && persisted.ExpirationTimeUtc > DateTime.UtcNow.AddMinutes(5))
                {
                    _cache.Set(cacheKey, persisted, persisted.ExpirationTimeUtc.AddMinutes(-5));
                    return persisted;
                }

                var loginTicketRequest = CreateLoginTicketRequest();
                var cms = Sign(loginTicketRequest);
            var soapRequest = $"""
<?xml version="1.0" encoding="utf-8"?>
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
  <soap:Body>
    <loginCms xmlns="http://wsaa.view.sua.dvadac.desein.afip.gov">
      <in0>{cms}</in0>
    </loginCms>
  </soap:Body>
</soap:Envelope>
""";

            using var request = new HttpRequestMessage(HttpMethod.Post, _options.WsaaUrl)
            {
                Content = new StringContent(soapRequest, Encoding.UTF8, "text/xml")
            };
            request.Headers.Add("SOAPAction", string.Empty);

            using var response = await _httpClientFactory.CreateClient("ArcaWsaa")
                .SendAsync(request, cancellationToken);
            var responseXml = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"WSAA rechazó la autenticación ({(int)response.StatusCode}): {GetSoapFaultMessage(responseXml)}");
            }

            response.EnsureSuccessStatusCode();

            var result = ParseResponse(responseXml);
            _cache.Set(cacheKey, result, result.ExpirationTimeUtc.AddMinutes(-5));
                SaveTicket(result);
            return result;
            }
            finally
            {
                _ticketLock.Release();
            }
        }

        private string CreateLoginTicketRequest()
        {
            var now = DateTime.UtcNow;
            return new XDocument(
                new XElement("loginTicketRequest", new XAttribute("version", "1.0"),
                    new XElement("header",
                        new XElement("uniqueId", new DateTimeOffset(now).ToUnixTimeSeconds()),
                        new XElement("generationTime", now.AddMinutes(-5).ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)),
                        new XElement("expirationTime", now.AddMinutes(10).ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture))),
                    new XElement("service", _options.Service)))
                .ToString(SaveOptions.DisableFormatting);
        }

        private string Sign(string loginTicketRequest)
        {
            var certificate = X509Certificate2.CreateFromPemFile(_options.CertificatePath, _options.PrivateKeyPath);
            var content = new ContentInfo(Encoding.UTF8.GetBytes(loginTicketRequest));
            var signedCms = new SignedCms(content, detached: false);
            signedCms.ComputeSignature(new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, certificate));
            return Convert.ToBase64String(signedCms.Encode());
        }

        private ArcaAuthenticationTicket ParseResponse(string soapResponse)
        {
            var soap = XDocument.Parse(soapResponse);
            var loginTicketResponse = soap.Descendants().FirstOrDefault(x => x.Name.LocalName == "loginCmsReturn")?.Value;
            if (string.IsNullOrWhiteSpace(loginTicketResponse))
            {
                throw new InvalidOperationException("WSAA no devolvió LoginTicketResponse.");
            }

            var ticket = XDocument.Parse(loginTicketResponse);
            var expirationTime = ticket.Descendants().FirstOrDefault(x => x.Name.LocalName == "expirationTime")?.Value;
            if (!DateTime.TryParse(expirationTime, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var expirationTimeUtc))
            {
                throw new InvalidOperationException("WSAA devolvió una fecha de vencimiento inválida.");
            }

            var token = ticket.Descendants().FirstOrDefault(x => x.Name.LocalName == "token")?.Value;
            var sign = ticket.Descendants().FirstOrDefault(x => x.Name.LocalName == "sign")?.Value;
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(sign))
            {
                throw new InvalidOperationException("WSAA no devolvió Token o Sign.");
            }

            return new ArcaAuthenticationTicket
            {
                Token = token,
                Sign = sign,
                ExpirationTimeUtc = expirationTimeUtc
            };
        }

        private static string GetSoapFaultMessage(string soapResponse)
        {
            try
            {
                var fault = XDocument.Parse(soapResponse)
                    .Descendants()
                    .FirstOrDefault(x => x.Name.LocalName is "faultstring" or "faultcode")?
                    .Value;

                return string.IsNullOrWhiteSpace(fault) ? "WSAA no informó el motivo." : fault;
            }
            catch (Exception)
            {
                return "WSAA devolvió una respuesta de error no válida.";
            }
        }

        private ArcaAuthenticationTicket? LoadPersistedTicket()
        {
            try
            {
                if (!File.Exists(_options.TicketCachePath))
                {
                    return null;
                }

                var encryptedTicket = File.ReadAllText(_options.TicketCachePath);
                return JsonSerializer.Deserialize<ArcaAuthenticationTicket>(EncryptionHelper.Decrypt(encryptedTicket));
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void SaveTicket(ArcaAuthenticationTicket ticket)
        {
            var directory = Path.GetDirectoryName(_options.TicketCachePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var encryptedTicket = EncryptionHelper.Encrypt(JsonSerializer.Serialize(ticket));
            File.WriteAllText(_options.TicketCachePath, encryptedTicket);
        }

        private void ValidateOptions()
        {
            if (string.IsNullOrWhiteSpace(_options.WsaaUrl) || string.IsNullOrWhiteSpace(_options.Service) ||
                !File.Exists(_options.CertificatePath) || !File.Exists(_options.PrivateKeyPath))
            {
                throw new InvalidOperationException("La configuración ARCA o los archivos de certificado no son válidos.");
            }
        }
    }
}