using Microsoft.AspNetCore.Mvc;
using MercadoPago.Config;
using MercadoPago.Client.Preference;
using MercadoPago.Resource.Preference;
using MercadoPago.Client.Payment;

using Microsoft.AspNetCore.Authorization;
using DondeSalimos.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace DondeSalimos.Server.Controllers

{
    [ApiController]
    [Authorize] 
    [Route("api/[controller]")]
    public class PagosController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly Contexto _context;

        public PagosController(IConfiguration configuration, Contexto context)
        {
            _configuration = configuration;
            _context = context;
            var accessToken = _configuration["MercadoPago:AccessToken"];
            MercadoPagoConfig.AccessToken = accessToken;
        }

        #region // POST: api/pagos/crear-preferencia
        [HttpPost("crear-preferencia")]
        public async Task<IActionResult> CrearPreferencia([FromBody] PreferenciaRequest request)
        {
            try
            {
                var externalReference = request.PublicidadId.ToString();

                // Determinar URLs según origen (web o app)
                PreferenceBackUrlsRequest backUrls;

                if (request.IsWeb)
                {
                    // URLs para la web
                    var webBaseUrl = _configuration["App:WebUrl"] ?? "https://donde-salimos-web.vercel.app";
                    backUrls = new PreferenceBackUrlsRequest
                    {
                        Success = $"{webBaseUrl}/payment/callback?status=success",
                        Failure = $"{webBaseUrl}/payment/callback?status=failure",
                        Pending = $"{webBaseUrl}/payment/callback?status=pending"
                    };
                }
                else
                {
                    // URLs para la app móvil
                    backUrls = new PreferenceBackUrlsRequest
                    {
                        Success = $"dondesalimos://payment/success?publicidad_id={request.PublicidadId}",
                        Failure = $"dondesalimos://payment/failure?publicidad_id={request.PublicidadId}",
                        Pending = $"dondesalimos://payment/pending?publicidad_id={request.PublicidadId}"
                    };
                }

                var preferenceRequest = new PreferenceRequest
                {
                    Items = new List<PreferenceItemRequest>
            {
                new PreferenceItemRequest
                {
                    Title = request.Titulo,
                    Quantity = 1,
                    CurrencyId = "ARS",
                    UnitPrice = request.Precio,
                }
            },
                    BackUrls = backUrls,
                    AutoReturn = "approved",
                    ExternalReference = externalReference,
                    StatementDescriptor = "DondeSalimos",
                     NotificationUrl = $"{_configuration["App:ApiUrl"]}/api/pagos/webhook"
                };

                var client = new PreferenceClient();
                Preference preference = await client.CreateAsync(preferenceRequest);

                return Ok(new { init_point = preference.InitPoint, id = preference.Id });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        #endregion

        #region // POST: api/pagos/verificar-pago
        [HttpPost("verificar-pago")]
        public async Task<IActionResult> VerificarPago([FromBody] VerificarPagoRequest request)
        {
            try
            {
                var paymentClient = new PaymentClient();
                var payment = await paymentClient.GetAsync(long.Parse(request.PaymentId));

                if (payment.Status == "approved")
                {
                    
                    var publicidadId = int.Parse(payment.ExternalReference);
                    var publicidad = await _context.Publicidad.FindAsync(publicidadId);
                    if (publicidad != null)
                    {
                        publicidad.Pago = true;
                        await _context.SaveChangesAsync();
                        Console.WriteLine($"[DEBUG] Publicidad {publicidadId} marcada como pagada");
                    }

                    return Ok(new
                    {
                        success = true,
                        message = "Pago verificado correctamente",
                        paymentStatus = payment.Status,
                        publicidadId = publicidadId,
                        estadoPublicidad = publicidad?.Estado ?? false,
                        pagoRealizado = true
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "El pago no fue aprobado",
                        paymentStatus = payment.Status
                    });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    error = ex.Message,
                    details = ex.InnerException?.Message
                });
            }
        }
        #endregion

        #region // POST: api/pagos/webhook
        [AllowAnonymous]
        [HttpPost("webhook")]
        public async Task<IActionResult> WebhookMercadoPago(
            [FromBody] dynamic notification,
            [FromHeader(Name = "x-signature")] string xSignature,
            [FromHeader(Name = "x-request-id")] string xRequestId)
        {
            try
            {
                Console.WriteLine($"[WEBHOOK] Notificación recibida: {notification}");
                Console.WriteLine($"[WEBHOOK] x-signature: {xSignature}");
                Console.WriteLine($"[WEBHOOK] x-request-id: {xRequestId}");

                // <CHANGE> Validar firma del webhook
                var webhookSecret = _configuration["MercadoPago:WebhookSecret"] ??
                                   Environment.GetEnvironmentVariable("MERCADOPAGO_WEBHOOK_SECRET");

                if (!string.IsNullOrEmpty(webhookSecret) && !string.IsNullOrEmpty(xSignature))
                {
                    // Extraer ts y hash de x-signature (formato: "ts=123456,v1=hash")
                    var signatureParts = xSignature.Split(',');
                    var ts = signatureParts[0].Replace("ts=", "");
                    var hash = signatureParts[1].Replace("v1=", "");

                    // Obtener el data.id de la notificación
                    string dataId = notification?.data?.id?.ToString() ?? "";

                    // Crear manifest: "id:{data.id};request-id:{x-request-id};ts:{ts};"
                    var manifest = $"id:{dataId};request-id:{xRequestId};ts:{ts};";

                    // Calcular HMAC-SHA256
                    using (var hmac = new System.Security.Cryptography.HMACSHA256(
                        System.Text.Encoding.UTF8.GetBytes(webhookSecret)))
                    {
                        var hashBytes = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(manifest));
                        var calculatedHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

                        if (calculatedHash != hash)
                        {
                            Console.WriteLine($"[WEBHOOK] Firma inválida. Rechazando notificación.");
                            return Unauthorized();
                        }
                    }

                    Console.WriteLine($"[WEBHOOK] Firma válida. Procesando notificación.");
                }

                // Mercado Pago envía el ID del pago en data.id
                string paymentIdStr = notification?.data?.id?.ToString();

                if (string.IsNullOrEmpty(paymentIdStr))
                {
                    return Ok(); // Mercado Pago requiere 200 OK
                }

                var paymentClient = new PaymentClient();
                var payment = await paymentClient.GetAsync(long.Parse(paymentIdStr));

                if (payment.Status == "approved" && !string.IsNullOrEmpty(payment.ExternalReference))
                {
                    var publicidadId = int.Parse(payment.ExternalReference);
                    var publicidad = await _context.Publicidad.FindAsync(publicidadId);

                    if (publicidad != null && !publicidad.Pago)
                    {
                        publicidad.Pago = true;
                        await _context.SaveChangesAsync();
                        Console.WriteLine($"[WEBHOOK] Publicidad {publicidadId} marcada como pagada automáticamente");
                    }
                }

                return Ok(); // Siempre responder 200 OK a Mercado Pago
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WEBHOOK ERROR] {ex.Message}");
                return Ok(); // Aún con error, responder 200 OK
            }
        }
        #endregion
    }

    public class PreferenciaRequest
    {
        public string Titulo { get; set; }
        public decimal Precio { get; set; }
        public int PublicidadId { get; set; }
        public bool IsWeb { get; set; } = false;
    }

    public class VerificarPagoRequest
    {
        public string PaymentId { get; set; }
        public string PreferenceId { get; set; }
    }
}