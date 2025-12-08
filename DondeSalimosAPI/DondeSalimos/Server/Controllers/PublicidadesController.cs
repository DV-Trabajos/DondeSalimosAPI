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

        #region // GET: api/publicidades/buscarIdUsuario/{usuarioId}
        [HttpGet]
        [Route("BuscarIdUsuario/{usuarioId}")]
        public async Task<ActionResult> GetPublicidadesByUsuario(int usuarioId)
        {
            try
            {
                // Obtener publicidades de comercios que pertenecen al usuario
                var publicidades = await _context.Publicidad
                    .AsNoTracking()
                    .Include(p => p.Comercio)
                    .Where(p => p.Comercio.ID_Usuario == usuarioId)
                    .Select(p => new
                    {
                        iD_Publicidad = p.ID_Publicidad,
                        descripcion = p.Descripcion,
                        visualizaciones = p.Visualizaciones,
                        tiempo = p.Tiempo,
                        estado = p.Estado,
                        pago = p.Pago,
                        fechaCreacion = p.FechaCreacion,
                        motivoRechazo = p.MotivoRechazo,
                        iD_Comercio = p.ID_Comercio,
                        imagen = p.Imagen,
                        comercio = p.Comercio == null ? null : new
                        {
                            iD_Comercio = p.Comercio.ID_Comercio,
                            nombre = p.Comercio.Nombre,
                            direccion = p.Comercio.Direccion
                        }
                    })
                    .OrderByDescending(p => p.fechaCreacion)
                    .ToListAsync();

                return Ok(publicidades);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Error al obtener publicidades del usuario",
                    details = ex.Message
                });
            }
        }
        #endregion

        #region // GET: api/publicidades/listadoAdmin 
        [AllowAnonymous]
        [HttpGet]
        [Route("listadoAdmin")]
        public async Task<ActionResult> GetAdvertisementsAdmin()
        {
            try
            {
                var publicidades = await _context.Publicidad
                    .AsNoTracking()
                    .Include(x => x.Comercio)
                    .Select(p => new
                    {
                        iD_Publicidad = p.ID_Publicidad,
                        descripcion = p.Descripcion,
                        visualizaciones = p.Visualizaciones,
                        tiempo = p.Tiempo,
                        estado = p.Estado,
                        fechaCreacion = p.FechaCreacion,
                        iD_Comercio = p.ID_Comercio,
                        motivoRechazo = p.MotivoRechazo,
                        pago = p.Pago,
                        tieneImagen = p.Imagen != null && p.Imagen.Length > 0,
                        comercio = p.Comercio == null ? null : new
                        {
                            iD_Comercio = p.Comercio.ID_Comercio,
                            nombre = p.Comercio.Nombre,
                            direccion = p.Comercio.Direccion,
                            correo = p.Comercio.Correo,
                            telefono = p.Comercio.Telefono
                        }
                    })
                    .ToListAsync();

                return Ok(publicidades);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al obtener publicidades", details = ex.Message });
            }
        }
        #endregion

        #region // GET: api/publicidades/{id}/imagen
        [AllowAnonymous]
        [HttpGet]
        [Route("{id}/imagen")]
        public async Task<ActionResult> GetPublicidadImagen(int id)
        {
            try
            {
                var publicidad = await _context.Publicidad
                    .AsNoTracking()
                    .Where(p => p.ID_Publicidad == id)
                    .Select(p => new
                    {
                        iD_Publicidad = p.ID_Publicidad,
                        imagen = p.Imagen,
                        tieneImagen = p.Imagen != null && p.Imagen.Length > 0
                    })
                    .FirstOrDefaultAsync();

                if (publicidad == null)
                {
                    return NotFound(new { mensaje = "Publicidad no encontrada" });
                }

                return Ok(publicidad);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al obtener imagen", details = ex.Message });
            }
        }
        #endregion

        #region // GET: api/publicidades/{id}/imagenRaw
        [AllowAnonymous]
        [HttpGet]
        [Route("{id}/imagenRaw")]
        public async Task<ActionResult> GetPublicidadImagenRaw(int id)
        {
            try
            {
                var imagen = await _context.Publicidad
                    .AsNoTracking()
                    .Where(p => p.ID_Publicidad == id)
                    .Select(p => p.Imagen)
                    .FirstOrDefaultAsync();

                if (imagen == null || imagen.Length == 0)
                {
                    return NotFound();
                }

                // Detectar tipo de imagen (básico)
                string contentType = "image/jpeg";
                if (imagen.Length > 8)
                {
                    // PNG magic bytes: 137 80 78 71
                    if (imagen[0] == 137 && imagen[1] == 80 && imagen[2] == 78 && imagen[3] == 71)
                    {
                        contentType = "image/png";
                    }
                    // GIF magic bytes: 71 73 70
                    else if (imagen[0] == 71 && imagen[1] == 73 && imagen[2] == 70)
                    {
                        contentType = "image/gif";
                    }
                    // WebP magic bytes: 82 73 70 70
                    else if (imagen[0] == 82 && imagen[1] == 73 && imagen[2] == 70 && imagen[3] == 70)
                    {
                        contentType = "image/webp";
                    }
                }

                // Agregar cache headers (1 hora)
                Response.Headers.Add("Cache-Control", "public, max-age=3600");

                return File(imagen, contentType);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al obtener imagen" });
            }
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
