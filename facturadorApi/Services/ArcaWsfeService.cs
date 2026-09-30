using System.Xml.Linq;
using Back.Conf;
using Back.DTOs;
using Microsoft.Extensions.Options;

namespace Back.Services
{
    public class ArcaWsfeService : IArcaWsfeService
    {
        private const string WsfeNamespace = "http://ar.gov.afip.dif.FEV1/";
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IArcaAuthenticationService _authenticationService;
        private readonly ArcaOptions _options;

        public ArcaWsfeService(
            IHttpClientFactory httpClientFactory,
            IArcaAuthenticationService authenticationService,
            IOptions<ArcaOptions> options)
        {
            _httpClientFactory = httpClientFactory;
            _authenticationService = authenticationService;
            _options = options.Value;
        }

        public async Task<ArcaUltimoComprobanteResponseDTO> GetUltimoComprobanteAsync(
            int tipoComprobante,
            CancellationToken cancellationToken = default)
        {
            if (tipoComprobante <= 0 || _options.CuitEmisor <= 0 || _options.PuntoDeVenta <= 0)
            {
                throw new InvalidOperationException("La configuración de emisor, punto de venta o tipo de comprobante no es válida.");
            }

            var ticket = await _authenticationService.GetTicketAsync(cancellationToken);
            var soapRequest = CreateSoapRequest(ticket, tipoComprobante);
            using var request = new HttpRequestMessage(HttpMethod.Post, _options.WsfeUrl)
            {
                Content = new StringContent(soapRequest, System.Text.Encoding.UTF8, "text/xml")
            };
            request.Headers.Add("SOAPAction", $"{WsfeNamespace}FECompUltimoAutorizado");

            using var response = await _httpClientFactory.CreateClient("ArcaWsaa").SendAsync(request, cancellationToken);
            var responseXml = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"WSFE rechazó la consulta ({(int)response.StatusCode}): {GetSoapError(responseXml)}");
            }

            return ParseResponse(responseXml, tipoComprobante);
        }

        public async Task<ArcaCaeResponseDTO> SolicitarCaeAsync(
            ArcaSolicitudCaeDTO solicitud,
            CancellationToken cancellationToken = default)
        {
            if (solicitud.TipoComprobante != 11)
            {
                throw new InvalidOperationException("Por el momento solo está habilitada la emisión de Factura C (tipo 11).");
            }

            if (solicitud.ImporteTotal <= 0 || _options.CuitEmisor <= 0 || _options.PuntoDeVenta <= 0)
            {
                throw new InvalidOperationException("La configuración de emisor, punto de venta o importe no es válida.");
            }

            var ultimoComprobante = await GetUltimoComprobanteAsync(solicitud.TipoComprobante, cancellationToken);
            var ticket = await _authenticationService.GetTicketAsync(cancellationToken);
            var numeroComprobante = ultimoComprobante.UltimoNumeroAutorizado + 1;
            var fechaComprobante = solicitud.FechaComprobante ?? DateOnly.FromDateTime(DateTime.Today);
            var soapRequest = CreateCaeSoapRequest(ticket, solicitud, numeroComprobante, fechaComprobante);

            using var request = new HttpRequestMessage(HttpMethod.Post, _options.WsfeUrl)
            {
                Content = new StringContent(soapRequest, System.Text.Encoding.UTF8, "text/xml")
            };
            request.Headers.Add("SOAPAction", $"{WsfeNamespace}FECAESolicitar");

            using var response = await _httpClientFactory.CreateClient("ArcaWsaa").SendAsync(request, cancellationToken);
            var responseXml = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"WSFE rechazó la emisión ({(int)response.StatusCode}): {GetSoapError(responseXml)}");
            }

            return ParseCaeResponse(responseXml, solicitud.TipoComprobante, numeroComprobante);
        }

        public async Task<ArcaTestConexionResponseDTO> TestConexionAsync(CancellationToken cancellationToken = default)
        {
            var response = new ArcaTestConexionResponseDTO
            {
                Ambiente = _options.Environment ?? "Default",
                CuitEmisor = _options.CuitEmisor,
                PuntoDeVenta = _options.PuntoDeVenta,
                WsaaUrl = _options.WsaaUrl,
                WsfeUrl = _options.WsfeUrl,
                FechaValidacion = DateTime.UtcNow,
                CertificadoThumbprint = GetCertificateThumbprint()
            };

            // Test WSAA
            try
            {
                var ticket = await _authenticationService.GetTicketAsync(cancellationToken);
                response.WsaaConexion = true;
                response.MensajeWsaa = "✓ Conexión exitosa con WSAA";
            }
            catch (Exception ex)
            {
                response.WsaaConexion = false;
                response.MensajeWsaa = $"✗ Error en WSAA: {ex.Message}";
            }

            // Test WSFE (FEDummy + FECompUltimoAutorizado)
            if (response.WsaaConexion)
            {
                try
                {
                    // FEDummy: diagnostico de servidores AFIP
                    response.FEDummy = await FEDummyAsync(cancellationToken);
                    
                    // FECompUltimoAutorizado: estado de numeración (tipo 11 = Factura C)
                    var ultimoComprobante = await GetUltimoComprobanteAsync(11, cancellationToken);
                    response.UltimoComprobante = new DTOs.FECompUltimoAutorizadoInfo
                    {
                        PuntoVenta = ultimoComprobante.PuntoDeVenta,
                        CbteTipo = ultimoComprobante.TipoComprobante,
                        UltimoAutorizado = (int)ultimoComprobante.UltimoNumeroAutorizado,
                        ProximoNumero = (int)ultimoComprobante.UltimoNumeroAutorizado + 1
                    };
                    
                    response.WsfeConexion = true;
                    response.MensajeWsfe = "✓ Conexión exitosa con WSFE";
                }
                catch (Exception ex)
                {
                    response.WsfeConexion = false;
                    response.MensajeWsfe = $"✗ Error en WSFE: {ex.Message}";
                }
            }
            else
            {
                response.WsfeConexion = false;
                response.MensajeWsfe = "⊘ No se probó (falló WSAA)";
            }

            return response;
        }

        public async Task<DTOs.FEDummyResponse> FEDummyAsync(CancellationToken cancellationToken = default)
        {
            var ticket = await _authenticationService.GetTicketAsync(cancellationToken);
            var soapRequest = CreateFEDummySoapRequest(ticket);
            
            using var request = new HttpRequestMessage(HttpMethod.Post, _options.WsfeUrl)
            {
                Content = new StringContent(soapRequest, System.Text.Encoding.UTF8, "text/xml")
            };
            request.Headers.Add("SOAPAction", $"{WsfeNamespace}FEDummy");

            using var response = await _httpClientFactory.CreateClient("ArcaWsaa").SendAsync(request, cancellationToken);
            var responseXml = await response.Content.ReadAsStringAsync(cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"WSFE rechazó FEDummy ({(int)response.StatusCode}): {GetSoapError(responseXml)}");
            }

            return ParseFEDummyResponse(responseXml);
        }

        private string CreateFEDummySoapRequest(ArcaAuthenticationTicket ticket)
        {
            XNamespace soap = "http://schemas.xmlsoap.org/soap/envelope/";
            XNamespace wsfe = WsfeNamespace;
            return new XDocument(
                new XElement(soap + "Envelope",
                    new XElement(soap + "Body",
                        new XElement(wsfe + "FEDummy",
                            new XElement(wsfe + "Auth",
                                new XElement(wsfe + "Token", ticket.Token),
                                new XElement(wsfe + "Sign", ticket.Sign),
                                new XElement(wsfe + "Cuit", _options.CuitEmisor))))))
                .ToString(SaveOptions.DisableFormatting);
        }

        private DTOs.FEDummyResponse ParseFEDummyResponse(string soapResponse)
        {
            var soap = XDocument.Parse(soapResponse);
            
            var appServer = soap.Descendants().FirstOrDefault(x => x.Name.LocalName == "AppServer")?.Value ?? "UNKNOWN";
            var dbServer = soap.Descendants().FirstOrDefault(x => x.Name.LocalName == "DbServer")?.Value ?? "UNKNOWN";
            var authServer = soap.Descendants().FirstOrDefault(x => x.Name.LocalName == "AuthServer")?.Value ?? "UNKNOWN";
            
            return new DTOs.FEDummyResponse
            {
                AppServer = appServer,
                DbServer = dbServer,
                AuthServer = authServer
            };
        }

        private string GetCertificateThumbprint()
        {
            try
            {
                using var cert = System.Security.Cryptography.X509Certificates.X509Certificate2.CreateFromPemFile(
                    _options.CertificatePath, _options.PrivateKeyPath);
                return cert.Thumbprint ?? "unknown";
            }
            catch
            {
                return "unknown";
            }
        }

        private string CreateSoapRequest(ArcaAuthenticationTicket ticket, int tipoComprobante)
        {
            XNamespace soap = "http://schemas.xmlsoap.org/soap/envelope/";
            XNamespace wsfe = WsfeNamespace;
            return new XDocument(
                new XElement(soap + "Envelope",
                    new XElement(soap + "Body",
                        new XElement(wsfe + "FECompUltimoAutorizado",
                            new XElement(wsfe + "Auth",
                                new XElement(wsfe + "Token", ticket.Token),
                                new XElement(wsfe + "Sign", ticket.Sign),
                                new XElement(wsfe + "Cuit", _options.CuitEmisor)),
                            new XElement(wsfe + "PtoVta", _options.PuntoDeVenta),
                            new XElement(wsfe + "CbteTipo", tipoComprobante)))))
                .ToString(SaveOptions.DisableFormatting);
        }

        private string CreateCaeSoapRequest(
            ArcaAuthenticationTicket ticket,
            ArcaSolicitudCaeDTO solicitud,
            long numeroComprobante,
            DateOnly fechaComprobante)
        {
            XNamespace soap = "http://schemas.xmlsoap.org/soap/envelope/";
            XNamespace wsfe = WsfeNamespace;
            return new XDocument(
                new XElement(soap + "Envelope",
                    new XElement(soap + "Body",
                        new XElement(wsfe + "FECAESolicitar",
                            new XElement(wsfe + "Auth",
                                new XElement(wsfe + "Token", ticket.Token),
                                new XElement(wsfe + "Sign", ticket.Sign),
                                new XElement(wsfe + "Cuit", _options.CuitEmisor)),
                            new XElement(wsfe + "FeCAEReq",
                                new XElement(wsfe + "FeCabReq",
                                    new XElement(wsfe + "CantReg", 1),
                                    new XElement(wsfe + "PtoVta", _options.PuntoDeVenta),
                                    new XElement(wsfe + "CbteTipo", solicitud.TipoComprobante)),
                                new XElement(wsfe + "FeDetReq",
                                    new XElement(wsfe + "FECAEDetRequest",
                                        new XElement(wsfe + "Concepto", 1),
                                        new XElement(wsfe + "DocTipo", solicitud.TipoDocumentoReceptor),
                                        new XElement(wsfe + "DocNro", solicitud.NumeroDocumentoReceptor),
                                        new XElement(wsfe + "CbteDesde", numeroComprobante),
                                        new XElement(wsfe + "CbteHasta", numeroComprobante),
                                        new XElement(wsfe + "CbteFch", fechaComprobante.ToString("yyyyMMdd")),
                                        new XElement(wsfe + "ImpTotal", solicitud.ImporteTotal),
                                        new XElement(wsfe + "ImpTotConc", 0),
                                        new XElement(wsfe + "ImpNeto", solicitud.ImporteTotal),
                                        new XElement(wsfe + "ImpOpEx", 0),
                                        new XElement(wsfe + "ImpTrib", 0),
                                        new XElement(wsfe + "ImpIVA", 0),
                                        new XElement(wsfe + "MonId", "PES"),
                                        new XElement(wsfe + "MonCotiz", 1),
                                        new XElement(wsfe + "CondicionIVAReceptorId", 5))))))))
                .ToString(SaveOptions.DisableFormatting);
        }

        private ArcaUltimoComprobanteResponseDTO ParseResponse(string soapResponse, int tipoComprobante)
        {
            var soap = XDocument.Parse(soapResponse);
            var error = soap.Descendants().FirstOrDefault(x => x.Name.LocalName == "Err")?.Value;
            if (!string.IsNullOrWhiteSpace(error))
            {
                throw new InvalidOperationException($"WSFE rechazó la consulta: {error}");
            }

            var numero = soap.Descendants().FirstOrDefault(x => x.Name.LocalName == "CbteNro")?.Value;
            if (!long.TryParse(numero, out var ultimoNumero))
            {
                throw new InvalidOperationException("WSFE no devolvió el último número de comprobante.");
            }

            return new ArcaUltimoComprobanteResponseDTO
            {
                PuntoDeVenta = _options.PuntoDeVenta,
                TipoComprobante = tipoComprobante,
                UltimoNumeroAutorizado = ultimoNumero
            };
        }

        private ArcaCaeResponseDTO ParseCaeResponse(string soapResponse, int tipoComprobante, long numeroComprobante)
        {
            var soap = XDocument.Parse(soapResponse);
            var error = soap.Descendants().FirstOrDefault(x => x.Name.LocalName == "Errors")?.Value;
            if (!string.IsNullOrWhiteSpace(error))
            {
                throw new InvalidOperationException($"WSFE rechazó la emisión: {error}");
            }

            var resultado = soap.Descendants().FirstOrDefault(x => x.Name.LocalName == "Resultado")?.Value;
            var cae = soap.Descendants().FirstOrDefault(x => x.Name.LocalName == "CAE")?.Value;
            var vencimiento = soap.Descendants().FirstOrDefault(x => x.Name.LocalName == "CAEFchVto")?.Value;
            if (resultado != "A" || string.IsNullOrWhiteSpace(cae) ||
                !DateOnly.TryParseExact(vencimiento, "yyyyMMdd", out var fechaVencimiento))
            {
                var observaciones = soap.Descendants().FirstOrDefault(x => x.Name.LocalName == "Observaciones")?.Value;
                throw new InvalidOperationException($"WSFE no autorizó el comprobante. Resultado: {resultado ?? "sin informar"}. {observaciones}".Trim());
            }

            return new ArcaCaeResponseDTO
            {
                PuntoDeVenta = _options.PuntoDeVenta,
                TipoComprobante = tipoComprobante,
                NumeroComprobante = numeroComprobante,
                Cae = cae,
                FechaVencimientoCae = fechaVencimiento
            };
        }

        private static string GetSoapError(string soapResponse)
        {
            try
            {
                var soap = XDocument.Parse(soapResponse);
                return soap.Descendants().FirstOrDefault(x => x.Name.LocalName is "faultstring" or "Err")?.Value
                    ?? "WSFE no informó el motivo.";
            }
            catch (System.Xml.XmlException)
            {
                return "WSFE devolvió una respuesta de error no válida.";
            }
        }
    }
}