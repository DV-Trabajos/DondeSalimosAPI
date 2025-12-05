using Microsoft.AspNetCore.Mvc;
using MercadoPago.Config;
using MercadoPago.Client.Preference;
using MercadoPago.Resource.Preference;
using MercadoPago.Client.Payment;
using Microsoft.AspNetCore.Authorization;
using DondeSalimos.Server.Data;

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
                        Success = "dondesalimos://payment/success",
                        Failure = "dondesalimos://payment/failure",
                        Pending = "dondesalimos://payment/pending"
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