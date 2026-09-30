using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Back.Conf;
using Back.DTOs;
using Back.Utils;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Back.Services
{
    public class ArcaAuthenticationService : IArcaAuthenticationService
    {
        private const string CacheKeyPrefix = "arca-auth-";
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        private readonly ArcaOptions _options;
        private readonly ILogger<ArcaAuthenticationService> _logger;
        private readonly SemaphoreSlim _ticketLock = new(1, 1);

        public ArcaAuthenticationService(
            IHttpClientFactory httpClientFactory,
            IMemoryCache cache,
            IOptions<ArcaOptions> options,
            ILogger<ArcaAuthenticationService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _cache = cache;
            _options = options.Value;
            _logger = logger;
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
            var cacheKey = BuildCacheKey();
            
            if (_cache.TryGetValue(cacheKey, out ArcaAuthenticationTicket? cached) &&
                cached!.ExpirationTimeUtc > DateTime.UtcNow.AddMinutes(5))
            {
                _logger.LogInformation("Ticket ARCA obtenido desde cache (Ambiente: {Environment}, CUIT: {Cuit}, Service: {Service})", 
                    _options.Environment ?? "Default", _options.CuitEmisor, _options.Service);
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
                    _logger.LogInformation("Ticket ARCA obtenido desde persistencia (Ambiente: {Environment}, CUIT: {Cuit})", 
                        _options.Environment ?? "Default", _options.CuitEmisor);
                    _cache.Set(cacheKey, persisted, persisted.ExpirationTimeUtc.AddMinutes(-5));
                    return persisted;
                }

                _logger.LogInformation("Solicitando nuevo Ticket ARCA a WSAA (Ambiente: {Environment}, URL: {WsaaUrl}, CUIT: {Cuit})", 
                    _options.Environment ?? "Default", _options.WsaaUrl, _options.CuitEmisor);

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
                    var faultMessage = GetSoapFaultMessage(responseXml);
                    _logger.LogError("WSAA rechazó la autenticación ({StatusCode}): {FaultMessage}", 
                        (int)response.StatusCode, faultMessage);
                    throw new InvalidOperationException($"WSAA rechazó la autenticación ({(int)response.StatusCode}): {faultMessage}");
                }

                response.EnsureSuccessStatusCode();

                var result = ParseResponse(responseXml);
                _cache.Set(cacheKey, result, result.ExpirationTimeUtc.AddMinutes(-5));
                SaveTicket(result);
                
                _logger.LogInformation("Ticket ARCA obtenido exitosamente de WSAA (Vencimiento: {Expiration})", 
                    result.ExpirationTimeUtc);
                return result;
            }
            finally
            {
                _ticketLock.Release();
            }
        }

        private string BuildCacheKey()
        {
            // Obtener thumbprint del certificado como identificador único
            var thumbprint = GetCertificateThumbprint();
            return $"{CacheKeyPrefix}{_options.Environment ?? "default"}-{_options.CuitEmisor}-{_options.Service}-{thumbprint}";
        }

        private string GetCertificateThumbprint()
        {
            try
            {
                using var cert = X509Certificate2.CreateFromPemFile(_options.CertificatePath, _options.PrivateKeyPath);
                return cert.Thumbprint ?? "unknown";
            }
            catch
            {
                return "unknown";
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
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error cargando ticket persistente desde {TicketCachePath}", _options.TicketCachePath);
                return null;
            }
        }

        private void SaveTicket(ArcaAuthenticationTicket ticket)
        {
            try
            {
                var directory = Path.GetDirectoryName(_options.TicketCachePath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var encryptedTicket = EncryptionHelper.Encrypt(JsonSerializer.Serialize(ticket));
                File.WriteAllText(_options.TicketCachePath, encryptedTicket);
                _logger.LogDebug("Ticket guardado en cache persistente en {TicketCachePath}", _options.TicketCachePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error guardando ticket en cache persistente");
            }
        }

        private void ValidateOptions()
        {
            if (string.IsNullOrWhiteSpace(_options.WsaaUrl) || string.IsNullOrWhiteSpace(_options.Service) ||
                !File.Exists(_options.CertificatePath) || !File.Exists(_options.PrivateKeyPath))
            {
                throw new InvalidOperationException("La configuración ARCA o los archivos de certificado no son válidos.");
            }

            // Validaciones cruzadas de ambiente
            bool isProduction = _options.Environment?.Equals("Production", StringComparison.OrdinalIgnoreCase) ?? false;
            bool isDevelopment = _options.Environment?.Equals("Development", StringComparison.OrdinalIgnoreCase) ?? true;

            if (isProduction)
            {
                // En producción, rechazar URLs de homologación
                if (_options.WsaaUrl.Contains("wsaahomo", StringComparison.OrdinalIgnoreCase) ||
                    _options.WsfeUrl.Contains("wswhomo", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"CONFIGURACIÓN INVÁLIDA: Ambiente=Production pero se detectaron URLs de HOMOLOGACIÓN. " +
                        $"WSAA URL: {_options.WsaaUrl}, WSFE URL: {_options.WsfeUrl}");
                }

                // En producción, PuntoDeVenta debe ser 4
                if (_options.PuntoDeVenta != 4)
                {
                    throw new InvalidOperationException(
                        $"CONFIGURACIÓN INVÁLIDA: Ambiente=Production pero PuntoDeVenta={_options.PuntoDeVenta} (debe ser 4)");
                }

                // En producción, verificar que el certificado NO sea el de homologación
                if (_options.CertificatePath.Contains("psicopedagoga", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"CONFIGURACIÓN INVÁLIDA: Ambiente=Production pero se detectó certificado de HOMOLOGACIÓN: {_options.CertificatePath}");
                }

                _logger.LogInformation(
                    "✓ PRODUCCIÓN CONFIGURADA CORRECTAMENTE: WSAA={WsaaUrl}, WSFE={WsfeUrl}, CUIT={Cuit}, PuntoDeVenta={PuntoDeVenta}, Certificado={CertPath}",
                    _options.WsaaUrl, _options.WsfeUrl, _options.CuitEmisor, _options.PuntoDeVenta, _options.CertificatePath);
            }
            else if (isDevelopment)
            {
                // En desarrollo, advertencia si se detectan URLs de producción
                if (_options.WsaaUrl.Contains("wsaa.afip.gov.ar", StringComparison.OrdinalIgnoreCase) ||
                    _options.WsfeUrl.Contains("servicios1.afip.gov.ar", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning(
                        "⚠ ADVERTENCIA: Ambiente=Development pero se detectaron URLs de PRODUCCIÓN. WSAA URL: {WsaaUrl}, WSFE URL: {WsfeUrl}",
                        _options.WsaaUrl, _options.WsfeUrl);
                }

                _logger.LogInformation(
                    "✓ DESARROLLO CONFIGURADO: WSAA={WsaaUrl}, WSFE={WsfeUrl}, CUIT={Cuit}, PuntoDeVenta={PuntoDeVenta}, Certificado={CertPath}",
                    _options.WsaaUrl, _options.WsfeUrl, _options.CuitEmisor, _options.PuntoDeVenta, _options.CertificatePath);
            }
        }
    }
}