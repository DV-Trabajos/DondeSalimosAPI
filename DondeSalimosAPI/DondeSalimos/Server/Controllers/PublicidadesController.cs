using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DondeSalimos.Server.Data;
using DondeSalimos.Shared.Modelos;
using Microsoft.AspNetCore.Authorization; 

namespace DondeSalimos.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PublicidadesController : ControllerBase
    {
        private readonly Contexto _context;

        public PublicidadesController(Contexto context)
        {
            _context = context;
        }

        #region // GET: api/publicidades/listado
        [AllowAnonymous]
        [HttpGet]
        [Route("listado")]
        public async Task<ActionResult<List<Publicidad>>> GetAdvertisements()
        {
            return await _context.Publicidad
                                    .AsNoTracking()
                                    .Include(x => x.Comercio)
                                    .ToListAsync();
        }
        #endregion

        #region // GET: api/publicidades/buscarIdPublicidad/{id}
        [AllowAnonymous]
        [HttpGet]
        [Route("buscarIdPublicidad/{id}")]
        public async Task<ActionResult<Publicidad>> GetIdAdvertising(int id)
        {
            var publicidad = await _context.Publicidad
                                                .AsNoTracking()
                                                .Where(x => x.ID_Publicidad == id)
                                                .Include(x => x.Comercio)
                                                .FirstOrDefaultAsync();

            if (publicidad == null)
            {
                return NotFound("Publicidad no encontrada");
            }

            return publicidad;
        }
        #endregion

        #region // GET: api/publicidades/buscarNombreComercio/{comercio}
        [AllowAnonymous]
        [HttpGet]
        [Route("buscarNombreComercio/{comercio}")]
        public async Task<ActionResult<List<Publicidad>>> GetAdvertisingByName(string comercio)
        {
            return await _context.Publicidad
                                    .AsNoTracking()
                                    .Where(x => x.Comercio.Nombre.ToLower().Contains(comercio))
                                    .Include(x => x.Comercio)
                                    .ToListAsync();
        }
        #endregion

        #region // PUT: api/publicidades/actualizar/{id}
        [HttpPut]
        [Route("actualizar/{id}")]
        public async Task<IActionResult> PutAdvertising(int id, Publicidad publicidad)
        {
            if (id != publicidad.ID_Publicidad)
            {
                return BadRequest();
            }

            _context.Entry(publicidad).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PublicidadExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }
        #endregion

        #region // PUT: api/publicidades/cambiar-estado/{id}
        [HttpPut]
        [Route("cambiar-estado/{id}")]
        public async Task<IActionResult> CambiarEstadoPublicidad(int id, [FromBody] CambiarEstadoRequest request)
        {
            var publicidad = await _context.Publicidad.FindAsync(id);

            if (publicidad == null)
            {
                return NotFound("Publicidad no encontrada");
            }

            // Validar que si se rechaza, tenga motivo
            if (!request.Estado && string.IsNullOrWhiteSpace(request.MotivoRechazo))
            {
                return BadRequest("El motivo de rechazo es requerido");
            }

            publicidad.Estado = request.Estado;
            publicidad.MotivoRechazo = request.Estado ? null : request.MotivoRechazo;

            await _context.SaveChangesAsync();

            var mensaje = request.Estado ? "Publicidad aprobada correctamente" : "Publicidad rechazada correctamente";
            return Ok(new { message = mensaje, estado = publicidad.Estado });
        }
        #endregion

        #region // POST: api/publicidades/crear
        [HttpPost]
        [Route("crear")]
        public async Task<ActionResult<Publicidad>> PostAdvertising(Publicidad publicidad)
        {
            try
            {
                publicidad.Estado = false; // Pendiente de aprobación del admin
                publicidad.Pago = false;   // Pendiente de pago
           
                _context.Publicidad.Add(publicidad);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Publicidad creada correctamente",
                    id = publicidad.ID_Publicidad,
                    publicidad = publicidad
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Error al crear publicidad",
                    details = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }
        #endregion

        #region // DELETE: api/publicidades/eliminar/{id}
        [HttpDelete]
        [Route("eliminar/{id}")]
        public async Task<IActionResult> DeleteAdvertising(int id)
        {
            var publicidad = await _context.Publicidad.FindAsync(id);

            if (publicidad == null)
            {
                return NotFound("Publicidad no encontrada");
            }

            _context.Publicidad.Remove(publicidad);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        #endregion

        #region // PUT: api/publicidades/incrementar-visualizacion/{id}
        [AllowAnonymous]
        [HttpPut]
        [Route("incrementar-visualizacion/{id}")]
        public async Task<IActionResult> IncrementarVisualizacion(int id)
        {
            try
            {
                var publicidad = await _context.Publicidad.FindAsync(id);

                if (publicidad == null)
                {
                    return NotFound(new { message = "Publicidad no encontrada" });
                }

                publicidad.Visualizaciones += 1;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Visualización incrementada correctamente",
                    visualizaciones = publicidad.Visualizaciones
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al incrementar visualización" });
            }
        }
        #endregion
        private bool PublicidadExists(int id)
        {
            return (_context.Publicidad?
                            .AsNoTracking()
                            .Any(e => e.ID_Publicidad == id)).GetValueOrDefault();
        }

        public class CambiarEstadoRequest
        {
            public bool Estado { get; set; }
            public string? MotivoRechazo { get; set; }
        }
    }
}
