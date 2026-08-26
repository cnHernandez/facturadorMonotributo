using System.Globalization;
using Back.Conf;
using Back.DTOs;
using Microsoft.Extensions.Options;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Back.Services
{
    public class ArcaPdfService : IArcaPdfService
    {
        private readonly ArcaOptions _options;

        public ArcaPdfService(IOptions<ArcaOptions> options)
        {
            _options = options.Value;
        }

        public byte[] GenerarFacturaC(ArcaSolicitudCaeDTO solicitud, ArcaCaeResponseDTO comprobante)
        {
            var culturaArgentina = new CultureInfo("es-AR");
            var fechaComprobante = solicitud.FechaComprobante ?? DateOnly.FromDateTime(DateTime.Today);
            var receptor = string.IsNullOrWhiteSpace(solicitud.NombreReceptor) ? "Consumidor final" : solicitud.NombreReceptor;
            var esObraSocial = solicitud.TipoDocumentoReceptor == 80;
            var documentoReceptor = FormatearDocumento(solicitud.NumeroDocumentoReceptor, solicitud.TipoDocumentoReceptor);
            var tipoDocumento = esObraSocial ? "CUIT" : "DNI";

            // Generar QR con información del comprobante
            var qrData = GenerarQr(comprobante, solicitud);

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(style => style.FontSize(9));

                    // Encabezado
                    page.Header().Column(column =>
                    {
                        column.Item().Row(row =>
                        {
                            // Lado izquierdo: Letra grande "C"
                            row.ConstantItem(80).AlignCenter().Text("C").FontSize(72).Bold().FontColor(Colors.Grey.Lighten2);

                            // Lado central: FACTURA C
                            row.RelativeItem().AlignCenter().Column(centerColumn =>
                            {
                                centerColumn.Item().Text("FACTURA C").FontSize(24).Bold();
                                centerColumn.Item().Text("ORIGINAL").FontSize(12).Bold().FontColor(Colors.Grey.Darken1);
                                centerColumn.Item().Text($"Tipo Comprobante: Factura de Crédito").FontSize(9);
                            });

                            // Lado derecho: Datos del comprobante
                            row.RelativeItem().Column(rightColumn =>
                            {
                                rightColumn.Item().Text($"Punto de Venta").FontSize(8).Italic();
                                rightColumn.Item().Text($"{comprobante.PuntoDeVenta:0000}").FontSize(14).Bold();
                                rightColumn.Item().PaddingTop(8).Text($"Comprobante").FontSize(8).Italic();
                                rightColumn.Item().Text($"{comprobante.NumeroComprobante:00000000}").FontSize(14).Bold();
                            });
                        });

                        column.Item().PaddingTop(16).LineHorizontal(1f);
                    });

                    // Contenido principal
                    page.Content().PaddingVertical(10).Column(column =>
                    {
                        column.Spacing(12);

                        // Sección EMISOR
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Column(emColumn =>
                            {
                                emColumn.Item().Text("DATOS DEL EMISOR").FontSize(9).Bold().Underline();
                                emColumn.Item().PaddingTop(4).Text("Nombre / Razón Social").FontSize(8).Italic();
                                emColumn.Item().Text(_options.NombreEmisor).FontSize(10).Bold();
                                emColumn.Item().PaddingTop(4).Text($"CUIT: {FormatearCuit(_options.CuitEmisor)}").FontSize(9);
                                emColumn.Item().Text($"Domicilio: {_options.DomicilioComercial}").FontSize(9);
                                emColumn.Item().Text($"Condición ante el IVA: RESPONSABLE MONOTRIBUTO").FontSize(9);
                            });

                            row.ConstantItem(120).AlignCenter().Column(qrColumn =>
                            {
                                qrColumn.Item().Text($"Fecha de emisión").FontSize(8).Italic();
                                qrColumn.Item().Text($"{fechaComprobante:dd/MM/yyyy}").FontSize(10).Bold();
                                if (qrData != null)
                                {
                                    qrColumn.Item().PaddingTop(8).Image(qrData);
                                }
                            });
                        });

                        column.Item().PaddingVertical(8).LineHorizontal(1f);

                        // Sección RECEPTOR
                        column.Item().Column(recColumn =>
                        {
                            recColumn.Item().Text("DATOS DEL RECEPTOR").FontSize(9).Bold().Underline();
                            recColumn.Item().PaddingTop(4).Text("Nombre / Razón Social").FontSize(8).Italic();
                            recColumn.Item().Text(receptor).FontSize(10).Bold();
                            recColumn.Item().Text($"{tipoDocumento}: {documentoReceptor}").FontSize(9);
                        });

                        column.Item().PaddingVertical(8).LineHorizontal(1f);

                        // Detalle de servicios
                        column.Item().Column(detColumn =>
                        {
                            detColumn.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Descripción").FontSize(9).Bold();
                                row.ConstantItem(100).AlignRight().Text("Importe").FontSize(9).Bold();
                            });

                            detColumn.Item().LineHorizontal(0.5f);

                            var descripcion = ObtenerDescripcionServicio(receptor, solicitud.TipoDocumentoReceptor);
                            detColumn.Item().Row(row =>
                            {
                                row.RelativeItem().Text(descripcion).FontSize(9);
                                row.ConstantItem(100).AlignRight().Text(solicitud.ImporteTotal.ToString("C2", culturaArgentina)).FontSize(9);
                            });

                            detColumn.Item().LineHorizontal(0.5f);

                            detColumn.Item().AlignRight().Row(row =>
                            {
                                row.ConstantItem(150).AlignRight().Text("TOTAL").FontSize(10).Bold();
                                row.ConstantItem(100).AlignRight().Text(solicitud.ImporteTotal.ToString("C2", culturaArgentina)).FontSize(11).Bold();
                            });
                        });

                        column.Item().PaddingVertical(8).LineHorizontal(1f);

                        // CAE y vencimiento
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Column(caeColumn =>
                            {
                                caeColumn.Item().Text("AUTORIZACIÓN AFIP").FontSize(9).Bold().Underline();
                                caeColumn.Item().PaddingTop(4).Text("CAE").FontSize(8).Italic();
                                caeColumn.Item().Text(comprobante.Cae).FontSize(12).Bold();
                                caeColumn.Item().PaddingTop(4).Text("Vencimiento CAE").FontSize(8).Italic();
                                caeColumn.Item().Text(comprobante.FechaVencimientoCae.ToString("dd/MM/yyyy")).FontSize(10).Bold();
                            });

                            row.ConstantItem(1);
                        });
                    });

                    // Footer
                    page.Footer().Column(column =>
                    {
                        column.Item().LineHorizontal(1f);
                        column.Item().PaddingTop(4).AlignCenter().Text("Comprobante autorizado por ARCA - AFIP en modo Homologación")
                            .FontSize(8).Italic();
                        column.Item().AlignCenter().Text("Este documento no tiene validez fiscal hasta ser autorizado por AFIP")
                            .FontSize(7).FontColor(Colors.Red.Medium);
                    });
                });
            }).GeneratePdf();
        }

        private string FormatearCuit(long cuit)
        {
            var cuitStr = cuit.ToString().PadLeft(11, '0');
            return $"{cuitStr.Substring(0, 2)}-{cuitStr.Substring(2, 8)}-{cuitStr.Substring(10, 1)}";
        }

        private string FormatearDocumento(long numero, int tipo)
        {
            if (tipo == 80)
            {
                var cuitStr = numero.ToString().PadLeft(11, '0');
                return $"{cuitStr.Substring(0, 2)}-{cuitStr.Substring(2, 8)}-{cuitStr.Substring(10, 1)}";
            }
            else
            {
                var dniStr = numero.ToString().PadLeft(8, '0');
                return $"{dniStr.Substring(0, 2)}.{dniStr.Substring(2, 3)}.{dniStr.Substring(5, 3)}";
            }
        }

        private string ObtenerDescripcionServicio(string receptor, int tipoDocumento)
        {
            if (tipoDocumento == 80)
            {
                return $"Sesiones de psicopedagogía - Correspondiente a paciente {receptor}";
            }
            else
            {
                return "Sesiones de psicopedagogía";
            }
        }

        private byte[]? GenerarQr(ArcaCaeResponseDTO comprobante, ArcaSolicitudCaeDTO solicitud)
        {
            try
            {
                var qrContent = $"FACTURA|C|{comprobante.PuntoDeVenta:0000}|{comprobante.NumeroComprobante:00000000}|{comprobante.Cae}";
                using var qrGenerator = new QRCodeGenerator();
                using var qrCodeData = qrGenerator.CreateQrCode(qrContent, QRCodeGenerator.ECCLevel.M);
                using var qrCode = new PngByteQRCode(qrCodeData);
                return qrCode.GetGraphic(10);
            }
            catch
            {
                return null;
            }
        }
    }
}