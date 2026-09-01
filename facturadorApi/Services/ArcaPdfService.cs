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

        public ArcaPdfService(
            IOptions<ArcaOptions> options)
        {
            _options = options.Value;
        }
private void CrearPaginaFactura(
    IDocumentContainer container,
    string copia,
    ArcaSolicitudCaeDTO solicitud,
    ArcaCaeResponseDTO comprobante,
    DateOnly fechaComprobante,
    string receptor,
    string tipoDocumento,
    string documentoReceptor,
    string descripcion,
    byte[]? qrData,
    CultureInfo culturaArgentina)
{
     var esObraSocial =
        solicitud.TipoDocumentoReceptor == 80;
    var subtotal = !solicitud.EsFacturacionManual &&
        solicitud.CantidadSesiones.HasValue &&
        solicitud.PrecioSesion.HasValue
            ? solicitud.CantidadSesiones.Value * solicitud.PrecioSesion.Value
            : solicitud.ImporteTotal;
    container.Page(page =>
    {
        page.Size(PageSizes.A4);
        page.Margin(10);
        page.DefaultTextStyle(
            x => x
                .FontSize(8)
                .FontFamily(Fonts.Arial));

        page.Content().Column(column =>
        {
            column.Spacing(0);

            // =====================================================
            // ORIGINAL / DUPLICADO / TRIPLICADO
            // =====================================================
            column.Item()
                .Border(1)
                .Height(28)
                .AlignCenter()
                .AlignMiddle()
                .Text(copia)
                .Bold()
                .FontSize(15);

            // =====================================================
            // ENCABEZADO PRINCIPAL
            // =====================================================
            column.Item()
                .BorderLeft(1)
                .BorderRight(1)
                .BorderBottom(1)
                .Height(126)
                .Row(row =>
                {
                    // DATOS EMISOR
                    row.RelativeItem(1.2f)
                        .Padding(8)
                        .Column(left =>
                        {
                            left.Item()
                                .PaddingLeft(32)
                                .PaddingTop(8)
                                .Text(_options.NombreEmisor.ToUpperInvariant())
                                .Bold()
                                .FontSize(10);

                            left.Item()
                                .PaddingTop(34)
                                .Text(text =>
                                {
                                    text.Span("Razón Social: ").Bold();
                                    text.Span(_options.NombreEmisor);
                                });

                            left.Item()
                                .PaddingTop(7)
                                .Text(text =>
                                {
                                    text.Span("Domicilio Comercial: ").Bold();
                                    text.Span(_options.DomicilioComercial);
                                });

                            left.Item()
                                .PaddingTop(7)
                                .Text(text =>
                                {
                                    text.Span("Condición frente al IVA: ").Bold();
                                    text.Span("Responsable Monotributo");
                                });
                        });

                    // LETRA C
                    row.ConstantItem(58)
                        .BorderLeft(1)
                        .BorderRight(1)
                        .AlignTop()
                        .Column(letter =>
                        {
                            letter.Item()
                                .AlignCenter()
                                .PaddingTop(6)
                                .Text("C")
                                .Bold()
                                .FontSize(28);

                            letter.Item()
                                .AlignCenter()
                                .Text("COD. 011")
                                .Bold()
                                .FontSize(7);
                        });

                    // DATOS FACTURA
                    row.RelativeItem(1.15f)
                        .Padding(8)
                        .Column(right =>
                        {
                            right.Item()
                                .PaddingTop(5)
                                .Text("FACTURA")
                                .Bold()
                                .FontSize(19);

                            right.Item()
                                .PaddingTop(14)
                                .Text(text =>
                                {
                                    text.Span("Punto de Venta: ").Bold();
                                    text.Span(
                                        comprobante.PuntoDeVenta
                                            .ToString("00000"))
                                        .Bold();

                                    text.Span("     Comp. Nro: ").Bold();

                                    text.Span(
                                        comprobante.NumeroComprobante
                                            .ToString("00000000"))
                                        .Bold();
                                });

                            right.Item()
                                .PaddingTop(7)
                                .Text(text =>
                                {
                                    text.Span("Fecha de Emisión: ").Bold();
                                    text.Span(
                                        fechaComprobante
                                            .ToString("dd/MM/yyyy"))
                                        .Bold();
                                });

                            right.Item()
                                .PaddingTop(11)
                                .Text(text =>
                                {
                                    text.Span("CUIT: ").Bold();
                                    text.Span(
                                        _options.CuitEmisor.ToString());
                                });

                            right.Item()
                                .Text(text =>
                                {
                                    text.Span("Ingresos Brutos: ").Bold();
                                    text.Span(
                                        _options.CuitEmisor.ToString());
                                });

                            right.Item()
                                .Text(text =>
                                {
                                    text.Span("Fecha de Inicio de Actividades: ")
                                        .Bold();

                                    text.Span("01/03/2010");
                                });
                        });
                });

            // =====================================================
            // PERIODO FACTURADO
            // =====================================================
            column.Item()
                .BorderLeft(1)
                .BorderRight(1)
                .BorderBottom(1)
                .Height(30)
                .PaddingHorizontal(7)
                .AlignMiddle()
                .Row(row =>
                {
                    row.RelativeItem()
                        .Text(text =>
                        {
                            text.Span("Período Facturado Desde: ")
                                .Bold();

                            var fechaDesde = solicitud.FechaDesde ?? 
                                new DateOnly(
                                    fechaComprobante.Year,
                                    fechaComprobante.Month,
                                    1);

                            text.Span(fechaDesde.ToString("dd/MM/yyyy"));
                        });

                    row.RelativeItem()
                        .Text(text =>
                        {
                            var ultimoDia = solicitud.FechaHasta.HasValue 
                                ? solicitud.FechaHasta.Value
                                : new DateOnly(
                                    fechaComprobante.Year,
                                    fechaComprobante.Month,
                                    DateTime.DaysInMonth(
                                        fechaComprobante.Year,
                                        fechaComprobante.Month));

                            text.Span("Hasta: ").Bold();
                            text.Span(ultimoDia.ToString("dd/MM/yyyy"));
                        });

                    row.RelativeItem()
                        .Text(text =>
                        {
                            var fechaVto = solicitud.FechaVencimiento ?? 
                                new DateOnly(
                                    fechaComprobante.Year,
                                    fechaComprobante.Month,
                                    10).AddMonths(1);

                            text.Span(
                                "Fecha de Vto. para el pago: ")
                                .Bold();

                            text.Span(fechaVto.ToString("dd/MM/yyyy"));
                        });
                });

            // =====================================================
            // RECEPTOR
            // =====================================================
            column.Item()
                .BorderLeft(1)
                .BorderRight(1)
                .BorderBottom(1)
                .Height(72)
                .Padding(7)
                .Column(rec =>
                {
                    rec.Item().Row(row =>
                    {
                        row.RelativeItem()
                            .Text(text =>
                            {
                                text.Span($"{tipoDocumento}: ")
                                    .Bold();

                                text.Span(documentoReceptor);
                            });

                        row.RelativeItem(2.2f)
                            .Text(text =>
                            {
                                text.Span(
                                    "Apellido y Nombre / Razón Social: ")
                                    .Bold();

                                text.Span(receptor);
                            });
                    });

                    rec.Item()
                        .PaddingTop(9)
                        .Row(row =>
                        {
                            row.RelativeItem()
                                .Text(text =>
                                {
                                    text.Span(
                                        "Condición frente al IVA: ")
                                        .Bold();

                                    text.Span(
                                        esObraSocial
                                            ? "Responsable Inscripto"
                                            : "Consumidor Final");
                                });

                            row.RelativeItem(2.2f)
                                .Text(text =>
                                {
                                    text.Span("Domicilio: ").Bold();

                                    text.Span(
                                        solicitud.DomicilioReceptor
                                        ?? string.Empty);
                                });
                        });

                    rec.Item()
                        .PaddingTop(9)
                        .Text(text =>
                        {
                            text.Span("Condición de venta: ").Bold();
                            text.Span("Contado");
                        });
                });

            // =====================================================
            // TABLA
            // =====================================================
            column.Item()
                .PaddingTop(2)
                .Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(45);
                        columns.RelativeColumn(3);
                        columns.ConstantColumn(70);
                        columns.ConstantColumn(55);
                        columns.ConstantColumn(80);
                        columns.ConstantColumn(55);
                        columns.ConstantColumn(70);
                        columns.ConstantColumn(85);
                    });

                    HeaderCell(table, "Código");
                    HeaderCell(table, "Producto / Servicio");
                    HeaderCell(table, "Cantidad");
                    HeaderCell(table, "U. Medida");
                    HeaderCell(table, "Precio Unit.");
                    HeaderCell(table, "% Bonif");
                    HeaderCell(table, "Imp. Bonif.");
                    HeaderCell(table, "Subtotal");

                    BodyCell(table, "");
                    BodyCell(table, descripcion);

                    var cantidad = solicitud.CantidadSesiones.HasValue
                        ? solicitud.CantidadSesiones.Value.ToString("N2", culturaArgentina)
                        : "1,00";
                    BodyCell(table, cantidad, true);
                    BodyCell(table, "unidades", true);

                    var precioUnitario = solicitud.PrecioSesion.HasValue
                        ? solicitud.PrecioSesion.Value.ToString("N2", culturaArgentina)
                        : solicitud.ImporteTotal.ToString("N2", culturaArgentina);
                    BodyCell(table, precioUnitario, true);

                    BodyCell(table, "0,00", true);
                    BodyCell(table, "0,00", true);

                    BodyCell(
                        table,
                        subtotal.ToString(
                            "N2",
                            culturaArgentina),
                        true);
                });

            // Espacio grande, como factura ARCA
            column.Item()
                .Height(235);

            // =====================================================
            // TOTALES
            // =====================================================
            column.Item()
                .Border(1)
                .Height(105)
                .Padding(12)
                .AlignBottom()
                .AlignRight()
                .Column(total =>
                {
                    total.Item()
                        .AlignRight()
                        .Text(text =>
                        {
                            text.Span("Subtotal: $     ")
                                .Bold();

                            text.Span(
                                subtotal.ToString(
                                    "N2",
                                    culturaArgentina))
                                .Bold();
                        });

                    total.Item()
                        .PaddingTop(9)
                        .AlignRight()
                        .Text(text =>
                        {
                            text.Span(
                                "Importe Otros Tributos: $     ")
                                .Bold();

                            text.Span("0,00").Bold();
                        });

                    total.Item()
                        .PaddingTop(9)
                        .AlignRight()
                        .Text(text =>
                        {
                            text.Span("Importe Total: $     ")
                                .Bold();

                            text.Span(
                                subtotal.ToString(
                                    "N2",
                                    culturaArgentina))
                                .Bold()
                                .FontSize(10);
                        });
                });

            // =====================================================
            // PIE
            // =====================================================
            column.Item()
                .PaddingTop(8)
                .Height(110)
                .Row(row =>
                {
                    // QR
                    row.ConstantItem(110)
                        .PaddingLeft(10)
                        .PaddingTop(5)
                        .Element(qr =>
                        {
                            if (qrData != null)
                            {
                                qr.Image(qrData);
                            }
                        });

                    // ARCA
                    row.RelativeItem()
                        .PaddingTop(9)
                        .Column(arca =>
                        {
                            arca.Item()
                                .Text("ARCA")
                                .Bold()
                                .FontSize(24);

                            arca.Item()
                                .Text(
                                    "AGENCIA DE RECAUDACIÓN Y CONTROL ADUANERO")
                                .FontSize(5);

                            arca.Item()
                                .PaddingTop(14)
                                .Text("Comprobante Autorizado")
                                .Bold()
                                .Italic();

                            arca.Item()
                                .PaddingTop(11)
                                .Text(
                                    "Esta Agencia no se responsabiliza por los datos ingresados en el detalle de la operación")
                                .Italic()
                                .FontSize(6);
                        });

                    // PAGINA
                    row.ConstantItem(85)
                        .PaddingTop(12)
                        .AlignCenter()
                        .Text("Pág. 1/1")
                        .Bold()
                        .FontSize(9);

                    // CAE
                    row.RelativeItem()
                        .PaddingTop(9)
                        .AlignRight()
                        .Column(cae =>
                        {
                            cae.Item()
                                .AlignRight()
                                .Text(
                                    $"CAE N°: {comprobante.Cae}")
                                .Bold()
                                .FontSize(9);

                            cae.Item()
                                .PaddingTop(7)
                                .AlignRight()
                                .Text(
                                    $"Fecha de Vto. de CAE: " +
                                    comprobante
                                        .FechaVencimientoCae
                                        .ToString("dd/MM/yyyy"))
                                .Bold()
                                .FontSize(9);
                        });
                });
        });
    });
}
private static void HeaderCell(
    TableDescriptor table,
    string texto)
{
    table.Cell()
        .Background(Colors.Grey.Lighten2)
        .Border(0.7f)
        .PaddingVertical(4)
        .PaddingHorizontal(2)
        .AlignCenter()
        .AlignMiddle()
        .Text(texto)
        .Bold()
        .FontSize(7);
}

private static void BodyCell(
    TableDescriptor table,
    string texto,
    bool center = false)
{
    var cell =
        table.Cell()
            .PaddingVertical(5)
            .PaddingHorizontal(3);

    if (center)
    {
        cell.AlignCenter()
            .Text(texto)
            .FontSize(7);
    }
    else
    {
        cell.Text(texto)
            .FontSize(7);
    }
}
       public byte[] GenerarFacturaC(
    ArcaSolicitudCaeDTO solicitud,
    ArcaCaeResponseDTO comprobante)
{
    var culturaArgentina = new CultureInfo("es-AR");

    var fechaComprobante =
        solicitud.FechaComprobante
        ?? DateOnly.FromDateTime(DateTime.Today);

    var receptor =
        string.IsNullOrWhiteSpace(solicitud.NombreReceptor)
            ? "Consumidor Final"
            : solicitud.NombreReceptor;

    var esObraSocial =
        solicitud.TipoDocumentoReceptor == 80;

    var documentoReceptor =
        FormatearDocumento(
            solicitud.NumeroDocumentoReceptor,
            solicitud.TipoDocumentoReceptor);

    var tipoDocumento =
        esObraSocial ? "CUIT" : "DNI";

    var qrData =
        GenerarQr(
            comprobante,
            solicitud);

    var descripcion =
        ObtenerDescripcionServicio(
            solicitud.PacienteNombre,
            solicitud.PacienteDni,
            solicitud.PacienteNumAfiliado,
            solicitud.FechaDesde ?? solicitud.FechaComprobante ?? DateOnly.FromDateTime(DateTime.Today),
            solicitud.TipoDocumentoReceptor);

    return Document.Create(container =>
    {
        CrearPaginaFactura(
            container,
            "ORIGINAL",
            solicitud,
            comprobante,
            fechaComprobante,
            receptor,
            tipoDocumento,
            documentoReceptor,
            descripcion,
            qrData,
            culturaArgentina);

        CrearPaginaFactura(
            container,
            "DUPLICADO",
            solicitud,
            comprobante,
            fechaComprobante,
            receptor,
            tipoDocumento,
            documentoReceptor,
            descripcion,
            qrData,
            culturaArgentina);

        CrearPaginaFactura(
            container,
            "TRIPLICADO",
            solicitud,
            comprobante,
            fechaComprobante,
            receptor,
            tipoDocumento,
            documentoReceptor,
            descripcion,
            qrData,
            culturaArgentina);
    }).GeneratePdf();
}

        private string FormatearCuit(long cuit)
        {
            var cuitStr =
                cuit.ToString()
                    .PadLeft(11, '0');

            return
                $"{cuitStr.Substring(0, 2)}-" +
                $"{cuitStr.Substring(2, 8)}-" +
                $"{cuitStr.Substring(10, 1)}";
        }

        private string FormatearDocumento(
            long numero,
            int tipo)
        {
            if (tipo == 80)
            {
                var cuitStr =
                    numero.ToString()
                        .PadLeft(11, '0');

                return
                    $"{cuitStr.Substring(0, 2)}-" +
                    $"{cuitStr.Substring(2, 8)}-" +
                    $"{cuitStr.Substring(10, 1)}";
            }

            var dniStr =
                numero.ToString()
                    .PadLeft(8, '0');

            return
                $"{dniStr.Substring(0, 2)}." +
                $"{dniStr.Substring(2, 3)}." +
                $"{dniStr.Substring(5, 3)}";
        }

        private string ObtenerDescripcionServicio(
            string? pacienteNombre,
            string? pacienteDni,
            string? pacienteNumAfiliado,
            DateOnly fechaFacturacion,
            int tipoDocumento)
        {
            var mesAnio = fechaFacturacion.ToString("MMMM yyyy", new CultureInfo("es-AR")).ToUpper();
            
            var descripcionBase =
                $"HONORARIOS PROFESIONALES POR SESIONES DE PSICOPEDAGOGIA CORRESPONDIENTE AL MES {mesAnio}";

            if (tipoDocumento == 80)
            {
                var nombrePaciente =
                    string.IsNullOrWhiteSpace(pacienteNombre)
                        ? "Paciente no informado"
                        : pacienteNombre;

                var afiliado =
                    string.IsNullOrWhiteSpace(pacienteNumAfiliado)
                        ? string.Empty
                        : $" + AF: {pacienteNumAfiliado}";

                return $"{descripcionBase} PACIENTE: {nombrePaciente}{afiliado}";
            }

            return descripcionBase;
        }

    private byte[]? GenerarQr(
    ArcaCaeResponseDTO comprobante,
    ArcaSolicitudCaeDTO solicitud)
{
    try
    {
        var fechaComprobante =
            solicitud.FechaComprobante
            ?? DateOnly.FromDateTime(DateTime.Today);

        var datosQr = new
        {
            ver = 1,
            fecha = fechaComprobante.ToString("yyyy-MM-dd"),
            cuit = _options.CuitEmisor,
            ptoVta = comprobante.PuntoDeVenta,
            tipoCmp = solicitud.TipoComprobante,
            nroCmp = comprobante.NumeroComprobante,
            importe = solicitud.ImporteTotal,
            moneda = "PES",
            ctz = 1,
            tipoDocRec = solicitud.TipoDocumentoReceptor,
            nroDocRec = solicitud.NumeroDocumentoReceptor,
            tipoCodAut = "E",
            codAut = comprobante.Cae
        };

        var json = System.Text.Json.JsonSerializer.Serialize(datosQr);

        var jsonBytes = System.Text.Encoding.UTF8.GetBytes(json);

        var base64 = Convert.ToBase64String(jsonBytes);

        var url =
            $"https://www.arca.gob.ar/fe/qr/?p={base64}";

        using var qrGenerator = new QRCodeGenerator();

        using var qrCodeData =
            qrGenerator.CreateQrCode(
                url,
                QRCodeGenerator.ECCLevel.M);

        using var qrCode =
            new PngByteQRCode(qrCodeData);

        return qrCode.GetGraphic(10);
    }
    catch
    {
        return null;
    }

}
    }
}