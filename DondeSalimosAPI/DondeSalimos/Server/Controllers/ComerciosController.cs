using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DondeSalimos.Server.Data;
using DondeSalimos.Shared.Modelos;
using Microsoft.AspNetCore.Authorization; 
using System.Text.RegularExpressions;
namespace DondeSalimos.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ComerciosController : ControllerBase

    {
        private readonly Contexto _context;
        private static readonly HashSet<string> ValidEmailDomains = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Dominios más comunes
            "gmail.com", "hotmail.com", "outlook.com", "yahoo.com", "live.com",
    
            // Dominios educativos
            "edu.ar", "unc.edu.ar", "unl.edu.ar", "uba.ar",
    
            // Otros dominios corporativos comunes
            "icloud.com", "me.com", "protonmail.com", "zoho.com",
            "aol.com", "msn.com", "ymail.com", "mail.com"
        };

        public ComerciosController(Contexto context)
        {
            _context = context;
        }

        #region // GET: api/comercios/listado
        [AllowAnonymous]
        [HttpGet]
        [Route("listado")]
        public async Task<ActionResult<IEnumerable<Comercio>>> GetShops()
        {
            return await _context.Comercio
                                .AsNoTracking()
                                .Include(x => x.TipoComercio)
                                .Include(x => x.Usuario)
                                .ToListAsync();
        }
        #endregion

        #region // GET: api/comercios/buscarIdComercio/{id}
        [AllowAnonymous]
        [HttpGet]
        [Route("buscarIdComercio/{id}")]
        public async Task<ActionResult<Comercio>> GetShopById(int id)
        {
            var comercioId = await _context.Comercio
                                            .AsNoTracking()
                                            .Where(x => x.ID_Comercio == id)
                                            .Include(x => x.TipoComercio)
                                            .Include(x => x.Usuario)
                                            .FirstOrDefaultAsync();

            if (comercioId == null)
            {
                return NotFound("Comercio no encontrado");
            }

            return comercioId;
        }
        #endregion

        #region // GET: api/comercios/buscarNombreComercio/{comercio}
        [AllowAnonymous]
        [HttpGet]
        [Route("buscarNombreComercio/{comercio}")]
        public async Task<ActionResult<List<Comercio>>> GetShopByName(string comercio)
        {
            var filter = comercio.ToLower();
            var comercioNombre = await _context.Comercio
                                                .AsNoTracking()
                                                .Where(x => x.Nombre.ToLower().Contains(filter))
                                                .Include(x => x.TipoComercio)
                                                .Include(x => x.Usuario)
                                                .ToListAsync();

            if (!comercioNombre.Any())
            {
                return NotFound("Comercio no encontrado");
            }

            return comercioNombre;
        }
        #endregion

        #region // GET: api/comercios/buscarComerciosPorUsuario/{usuarioId}
        [HttpGet]
        [Route("buscarComerciosPorUsuario/{usuarioId}")]
        public async Task<ActionResult<List<Comercio>>> GetShopsPerUser(int usuarioId)
        {
            var comercioNombre = await _context.Comercio
                                                .AsNoTracking()
                                                .Where(x => x.ID_Usuario == usuarioId)
                                                .Include(x => x.TipoComercio)
                                                .Include(x => x.Usuario)
                                                .ToListAsync();

            if (comercioNombre == null)
            {
                return NotFound("El Usuario no tiene comercios asociados");
            }

            return comercioNombre;
        }
        #endregion

        #region // GET: api/comercios/listadoAdmin
        [AllowAnonymous]
        [HttpGet]
        [Route("listadoAdmin")]
        public async Task<ActionResult> GetShopsAdmin()
        {
            try
            {
                var comercios = await _context.Comercio
                    .AsNoTracking()
                    .Include(x => x.TipoComercio)
                    .Include(x => x.Usuario)
                    .Select(c => new
                    {
                        iD_Comercio = c.ID_Comercio,
                        nombre = c.Nombre,
                        direccion = c.Direccion,
                        telefono = c.Telefono,
                        correo = c.Correo,
                        nroDocumento = c.NroDocumento,
                        tipoDocumento = c.TipoDocumento,
                        capacidad = c.Capacidad,
                        mesas = c.Mesas,
                        generoMusical = c.GeneroMusical,
                        horaIngreso = c.HoraIngreso,
                        horaCierre = c.HoraCierre,
                        estado = c.Estado,
                        motivoRechazo = c.MotivoRechazo,
                        fechaCreacion = c.FechaCreacion,
                        iD_TipoComercio = c.ID_TipoComercio,
                        iD_Usuario = c.ID_Usuario,
                        tieneFoto = c.Foto != null && c.Foto.Length > 0,
                        tipoComercio = c.TipoComercio == null ? null : new
                        {
                            iD_TipoComercio = c.TipoComercio.ID_TipoComercio,
                            descripcion = c.TipoComercio.Descripcion
                        },
                        usuario = c.Usuario == null ? null : new
                        {
                            iD_Usuario = c.Usuario.ID_Usuario,
                            nombreUsuario = c.Usuario.NombreUsuario,
                            correo = c.Usuario.Correo,
                            telefono = c.Usuario.Telefono
                        }
                    })
                    .ToListAsync();

                return Ok(comercios);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al obtener comercios", details = ex.Message });
            }
        }
        #endregion

        #region // GET: api/comercios/{id}/imagen
        [AllowAnonymous]
        [HttpGet]
        [Route("{id}/imagen")]
        public async Task<ActionResult> GetComercioImagen(int id)
        {
            try
            {
                var comercio = await _context.Comercio
                    .AsNoTracking()
                    .Where(c => c.ID_Comercio == id)
                    .Select(c => new
                    {
                        iD_Comercio = c.ID_Comercio,
                        foto = c.Foto,
                        tieneFoto = c.Foto != null && c.Foto.Length > 0
                    })
                    .FirstOrDefaultAsync();

                if (comercio == null)
                {
                    return NotFound(new { mensaje = "Comercio no encontrado" });
                }

                return Ok(comercio);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al obtener imagen", details = ex.Message });
            }
        }
        #endregion

        #region // GET: api/comercios/{id}/imagenRaw
        [AllowAnonymous]
        [HttpGet]
        [Route("{id}/imagen-raw")]
        public async Task<ActionResult> GetComercioImagenRaw(int id)
        {
            try
            {
                var foto = await _context.Comercio
                    .AsNoTracking()
                    .Where(c => c.ID_Comercio == id)
                    .Select(c => c.Foto)
                    .FirstOrDefaultAsync();

                if (foto == null || foto.Length == 0)
                {
                    return NotFound();
                }

                // Detectar tipo de imagen (básico)
                string contentType = "image/jpeg";
                if (foto.Length > 8)
                {
                    // PNG magic bytes: 137 80 78 71
                    if (foto[0] == 137 && foto[1] == 80 && foto[2] == 78 && foto[3] == 71)
                    {
                        contentType = "image/png";
                    }
                    // GIF magic bytes: 71 73 70
                    else if (foto[0] == 71 && foto[1] == 73 && foto[2] == 70)
                    {
                        contentType = "image/gif";
                    }
                    // WebP magic bytes: 82 73 70 70
                    else if (foto[0] == 82 && foto[1] == 73 && foto[2] == 70 && foto[3] == 70)
                    {
                        contentType = "image/webp";
                    }
                }

                // Agregar cache headers (1 hora)
                Response.Headers.Add("Cache-Control", "public, max-age=3600");

                return File(foto, contentType);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al obtener imagen" });
            }
        }
        #endregion

        #region // PUT: api/comercios/actualizar/{id}
        [HttpPut]
        [Route("actualizar/{id}")]
        public async Task<IActionResult> PutShop(int id, Comercio comercio)
        {
            if (id != comercio.ID_Comercio)
            {
                return BadRequest();
            }

            _context.Entry(comercio).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ShopExists(id))
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

        #region // POST: api/comercios/crear
        [HttpPost]
        [Route("crear")]
        public async Task<ActionResult<Comercio>> PostShop(Comercio comercio)
        {

            if (!string.IsNullOrEmpty(comercio.Correo))
            {
                // Validar formato básico de email
                var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                if (!emailRegex.IsMatch(comercio.Correo))
                {
                    return BadRequest(new { Correo = new[] { "El formato del correo es inválido." } });
                }

                // Validar que el dominio esté en la lista de dominios permitidos
                var emailParts = comercio.Correo.Split('@');
                if (emailParts.Length == 2)
                {
                    var domain = emailParts[1].ToLower();

                    // Verificar si el dominio está en la lista de dominios válidos
                    // O si termina con .edu.ar para instituciones educativas
                    bool isValidDomain = ValidEmailDomains.Contains(domain) ||
                                        domain.EndsWith(".edu.ar") ||
                                        domain.EndsWith(".edu");

                    if (!isValidDomain)
                    {
                        return BadRequest(new
                        {
                            Correo = new[] {
                                "El correo debe ser de un dominio conocido (gmail.com, hotmail.com, outlook.com, yahoo.com, etc.)."
                            }
                        });
                    }
                }
            }
            // Validar dígito verificador del CUIT
            if (!string.IsNullOrEmpty(comercio.NroDocumento))
            {
                if (!ValidateCUITCheckDigit(comercio.NroDocumento))
                {
                    return BadRequest(new
                    {
                        NroDocumento = new[] { "El dígito verificador del CUIT es incorrecto." }
                    });
                }
            }

            var comercioCUIT = await _context.Comercio
                                              .AsNoTracking()
                                              .Where(x => x.NroDocumento == comercio.NroDocumento)
                                              .FirstOrDefaultAsync();

            if (comercioCUIT != null)
            {
                return BadRequest("Existe comercio con el mismo CUIT");
            }

            _context.Comercio.Add(comercio);
            await _context.SaveChangesAsync();

            return Ok("Comercio creado correctamente");
        }
        #endregion

        #region // DELETE: api/comercios/eliminar/{id}
        [HttpDelete]
        [Route("eliminar/{id}")]
        public async Task<IActionResult> DeleteShop(int id)
        {
            var comercio = await _context.Comercio.FindAsync(id);

            if (comercio == null)
            {
                return NotFound("Comercio no encontrado");
            }

            try
            {
                // 1. ELIMINAR RESERVAS DEL COMERCIO
                var reservasComercio = await _context.Reserva
                    .Where(r => r.ID_Comercio == id)
                    .ToListAsync();

                if (reservasComercio.Any())
                {
                    _context.Reserva.RemoveRange(reservasComercio);
                    await _context.SaveChangesAsync();
                }

                // 2. ELIMINAR PUBLICIDADES DEL COMERCIO
                var publicidadesComercio = await _context.Publicidad
                    .Where(p => p.ID_Comercio == id)
                    .ToListAsync();

                if (publicidadesComercio.Any())
                {
                    _context.Publicidad.RemoveRange(publicidadesComercio);
                    await _context.SaveChangesAsync();
                }

                // 3. ELIMINAR RESEÑAS DEL COMERCIO
                var reseniasComercio = await _context.Resenia
                    .Where(r => r.ID_Comercio == id)
                    .ToListAsync();

                if (reseniasComercio.Any())
                {
                    _context.Resenia.RemoveRange(reseniasComercio);
                    await _context.SaveChangesAsync();
                }

                // 4. ELIMINAR EL COMERCIO
                _context.Comercio.Remove(comercio);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al eliminar comercio: {ex.Message}");
            }
        }
        #endregion

        private bool ShopExists(int id)
        {
            return (_context.Comercio?
                            .AsNoTracking()
                            .Any(e => e.ID_Comercio == id)).GetValueOrDefault();
        }

        #region // Validación de CUIT
        /// <summary>
        /// Valida el dígito verificador de un CUIT argentino según el algoritmo oficial
        /// </summary>
        private bool ValidateCUITCheckDigit(string cuit)
        {
            string cleanCuit = cuit.Replace("-", "");

            if (cleanCuit.Length != 11 || !long.TryParse(cleanCuit, out _))
                return false;

            int[] multipliers = { 5, 4, 3, 2, 7, 6, 5, 4, 3, 2 };
            int sum = 0;

            for (int i = 0; i < 10; i++)
            {
                sum += int.Parse(cleanCuit[i].ToString()) * multipliers[i];
            }

            int remainder = sum % 11;
            int checkDigit = 11 - remainder;

            if (checkDigit == 11) checkDigit = 0;
            if (checkDigit == 10) checkDigit = 9;

            return checkDigit == int.Parse(cleanCuit[10].ToString());
        }
        #endregion
    }

}
