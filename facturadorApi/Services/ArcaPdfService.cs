using System.Globalization;
using Back.Conf;
using Back.DTOs;
using Microsoft.Extensions.Options;
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

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(style => style.FontSize(10));

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(column =>
                        {
                            column.Item().Text("FACTURA C").FontSize(22).Bold();
                            column.Item().Text($"Punto de venta: {comprobante.PuntoDeVenta:0000}");
                            column.Item().Text($"Comprobante: {comprobante.NumeroComprobante:00000000}");
                        });
                        row.RelativeItem().AlignRight().Column(column =>
                        {
                            column.Item().Text("Emisor").Bold();
                            column.Item().Text($"CUIT: {_options.CuitEmisor}");
                            column.Item().Text(_options.DomicilioComercial);
                            column.Item().Text($"Fecha: {fechaComprobante:dd/MM/yyyy}");
                        });
                    });

                    page.Content().PaddingVertical(25).Column(column =>
                    {
                        column.Spacing(8);
                        column.Item().Text("Receptor").Bold();
                        column.Item().Text(receptor);
                        column.Item().Text($"Documento: {solicitud.TipoDocumentoReceptor} - {solicitud.NumeroDocumentoReceptor}");
                        column.Item().PaddingTop(16).LineHorizontal(1);
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Servicios profesionales psicopedagógicos");
                            row.ConstantItem(120).AlignRight().Text(solicitud.ImporteTotal.ToString("C2", culturaArgentina));
                        });
                        column.Item().PaddingTop(10).LineHorizontal(1);
                        column.Item().AlignRight().Text($"Total: {solicitud.ImporteTotal.ToString("C2", culturaArgentina)}").Bold();
                        column.Item().PaddingTop(28).Text($"CAE: {comprobante.Cae}").Bold();
                        column.Item().Text($"Vencimiento CAE: {comprobante.FechaVencimientoCae:dd/MM/yyyy}");
                    });

                    page.Footer().AlignCenter().Text("Comprobante autorizado por ARCA - Homologación");
                });
            }).GeneratePdf();
        }
    }
}