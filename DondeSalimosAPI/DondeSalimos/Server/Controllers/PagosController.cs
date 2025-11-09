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
            _context = context; // <CHANGE> Inicializar contexto
            var accessToken = _configuration["MercadoPago:AccessToken"];

            Console.WriteLine("=== CONFIGURACIÓN MERCADO PAGO ===");
            Console.WriteLine($"Access Token: {accessToken?.Substring(0, 30)}...");
            Console.WriteLine($"Longitud del token: {accessToken?.Length}");

            MercadoPagoConfig.AccessToken = accessToken;
        }

        [HttpPost("crear-preferencia")]
        public async Task<IActionResult> CrearPreferencia([FromBody] PreferenciaRequest request)
        {
            try
            {
                Console.WriteLine($"[DEBUG] Creando preferencia - Titulo: {request.Titulo}, Precio: {request.Precio}");
                Console.WriteLine($"[DEBUG] PublicidadId recibido: {request.PublicidadId}");

                var externalReference = request.PublicidadId.ToString();
                Console.WriteLine($"[DEBUG] External Reference (PublicidadId): {externalReference}");

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
                    Payer = new PreferencePayerRequest
                    {
                        Name = "Test",
                        Surname = "User",
                        Email = "test_user@testuser.com",
                    },
                    BackUrls = new PreferenceBackUrlsRequest
                    {
                        Success = "dondesalimos://payment/success",
                        Failure = "dondesalimos://payment/failure",
                        Pending = "dondesalimos://payment/pending"
                    },
                    AutoReturn = "approved",
                    ExternalReference = externalReference,
                    StatementDescriptor = "DondeSalimos",
                };

                var client = new PreferenceClient();
                Preference preference = await client.CreateAsync(preferenceRequest);

                Console.WriteLine($"[DEBUG] Preferencia creada: {preference.Id}");
                Console.WriteLine($"[DEBUG] External Reference guardado: {preference.ExternalReference}");

                return Ok(new { init_point = preference.InitPoint, id = preference.Id });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex.Message}");
                return BadRequest(new
                {
                    error = ex.Message,
                    details = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        [HttpPost("verificar-pago")]
        public async Task<IActionResult> VerificarPago([FromBody] VerificarPagoRequest request)
        {
            try
            {
                Console.WriteLine($"[DEBUG] Verificando pago - PaymentId: {request.PaymentId}");

                var paymentClient = new PaymentClient();
                var payment = await paymentClient.GetAsync(long.Parse(request.PaymentId));

                Console.WriteLine($"[DEBUG] Payment status: {payment.Status}");
                Console.WriteLine($"[DEBUG] External Reference (PublicidadId): {payment.ExternalReference}");

                if (payment.Status == "approved")
                {
                    
                    var publicidadId = int.Parse(payment.ExternalReference);

                   // <CHANGE> Actualizar Pago = true en la publicidad
            var publicidad = await _context.Publicidad.FindAsync(publicidadId);
            if (publicidad != null)
            {
                publicidad.Pago = true;
                await _context.SaveChangesAsync();
                Console.WriteLine($"[DEBUG] Publicidad {publicidadId} marcada como pagada");
            }

            Console.WriteLine($"[DEBUG] Pago aprobado para publicidad ID: {publicidadId}");


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
                Console.WriteLine($"[ERROR] Error al verificar pago: {ex.Message}");
                return BadRequest(new
                {
                    success = false,
                    error = ex.Message,
                    details = ex.InnerException?.Message
                });
            }
        }
    }

    public class PreferenciaRequest
    {
        public string Titulo { get; set; }
        public decimal Precio { get; set; }
        public int PublicidadId { get; set; }
    }

    public class VerificarPagoRequest
    {
        public string PaymentId { get; set; }
        public string PreferenceId { get; set; }
    }
}