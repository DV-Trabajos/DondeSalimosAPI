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
    public class ReservasController : ControllerBase
    {
        private readonly Contexto _context;

        public ReservasController(Contexto context)
        {
            _context = context;
        }

        #region // GET: api/reservas/listado
        [HttpGet]
        [Route("listado")]
        public async Task<ActionResult<List<Reserva>>> GetReservations()
        {
            return await _context.Reserva
                                    .AsNoTracking()
                                    .Include(x => x.Comercio)
                                    .Include(x => x.Usuario)
                                    .Where(x => x.Usuario.Estado == true)
                                    .ToListAsync();
        }
        #endregion

        #region // GET: api/reservas/buscarIdReserva/{id}
        [HttpGet]
        [Route("buscarIdReserva/{id}")]
        public async Task<ActionResult<Reserva>> GetIdReservation(int id)
        {
            var reserva = await _context.Reserva
                                            .AsNoTracking().Where(x => x.ID_Reserva == id)
                                            .Include(x => x.Comercio)
                                            .Include(x => x.Usuario)
                                            .FirstOrDefaultAsync();

            if (reserva == null)
            {
                return NotFound("Reserva no encontrada");
            }

            return reserva;
        }
        #endregion

        #region // GET: api/reservas/buscarNombreComercio/{comercio}
        [HttpGet]
        [Route("buscarNombreComercio/{comercio}")]
        public async Task<ActionResult<List<Reserva>>> GetReservationByName(string comercio)
        {
            return await _context.Reserva
                                    .AsNoTracking()
                                    .Where(x => x.Comercio.Nombre.ToLower().Contains(comercio))
                                    .Include(x => x.Usuario)
                                    .ToListAsync();
        }
        #endregion

        #region // GET: api/reservas/recibidasUsuario/{usuarioId}
        [HttpGet]
        [Route("recibidasUsuario/{usuarioId}")]
        public async Task<ActionResult> GetReservasRecibidasByUsuario(int usuarioId)
        {
            try
            {
                // Obtener reservas de comercios que pertenecen al usuario
                var reservas = await _context.Reserva
                    .AsNoTracking()
                    .Include(r => r.Comercio)
                    .Include(r => r.Usuario)
                    .Where(r => r.Comercio.ID_Usuario == usuarioId && r.Usuario.Estado == true)
                    .Select(r => new
                    {
                        iD_Reserva = r.ID_Reserva,
                        fechaReserva = r.FechaReserva,
                        tiempoTolerancia = r.TiempoTolerancia,
                        comenzales = r.Comenzales,
                        estado = r.Estado,
                        fechaCreacion = r.FechaCreacion,
                        motivoRechazo = r.MotivoRechazo,
                        iD_Usuario = r.ID_Usuario,
                        iD_Comercio = r.ID_Comercio,
                        // Datos del comercio (incluye foto porque son pocas reservas)
                        comercio = r.Comercio == null ? null : new
                        {
                            iD_Comercio = r.Comercio.ID_Comercio,
                            nombre = r.Comercio.Nombre,
                            direccion = r.Comercio.Direccion,
                            telefono = r.Comercio.Telefono,
                            foto = r.Comercio.Foto
                        },
                        // Datos del usuario que hizo la reserva
                        usuario = r.Usuario == null ? null : new
                        {
                            iD_Usuario = r.Usuario.ID_Usuario,
                            nombreUsuario = r.Usuario.NombreUsuario,
                            correo = r.Usuario.Correo,
                            telefono = r.Usuario.Telefono
                        }
                    })
                    .OrderByDescending(r => r.fechaReserva)
                    .ToListAsync();

                return Ok(reservas);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Error al obtener reservas del usuario",
                    details = ex.Message
                });
            }
        }
        #endregion

        #region // GET: api/reservas/usuario/{idUsuario}
        [HttpGet]
        [Route("usuario/{idUsuario}")]
        public async Task<ActionResult<List<Reserva>>> GetReservasByUsuario(int idUsuario)
        {
            var reservas = await _context.Reserva
                                        .AsNoTracking()
                                        .Where(x => x.ID_Usuario == idUsuario && x.Usuario.Estado == true)
                                        .Include(x => x.Comercio)
                                        .Include(x => x.Usuario)
                                        .ToListAsync();

            if (reservas == null || !reservas.Any())
            {
                return NotFound("No se encontraron reservas para este usuario");
            }

            return reservas;
        }
        #endregion

        #region // PUT: api/reservas/actualizar/{id}
        [HttpPut]
        [Route("actualizar/{id}")]
        public async Task<IActionResult> PutReservation(int id, Reserva reserva)
        {
            if (id != reserva.ID_Reserva)
            {
                return BadRequest();
            }

            _context.Entry(reserva).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ReservaExists(id))
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

        #region // POST: api/reservas/crear
        [HttpPost]
        [Route("crear")]
        public async Task<ActionResult<Reserva>> PostReservation(Reserva reserva)
        {
            var usuario = await _context.Usuario.FindAsync(reserva.ID_Usuario);
            if (usuario == null || usuario.Estado == false)
            {
                return BadRequest("No puedes crear una reserva porque tu cuenta está inactiva.");
            }

            var comercio = await _context.Comercio.FindAsync(reserva.ID_Comercio);
            if (comercio == null || comercio.Estado == false)
            {
                return BadRequest("El comercio no está disponible para reservas.");
            }

            var reservaPendiente = await _context.Reserva
                .AsNoTracking()
                .Where(x => x.ID_Usuario == reserva.ID_Usuario &&
                           x.ID_Comercio == reserva.ID_Comercio &&
                           x.FechaReserva.Date == reserva.FechaReserva.Date &&
                           x.Estado == false &&
                           x.MotivoRechazo == null) // Pendiente de aprobación
                .FirstOrDefaultAsync();

            if (reservaPendiente != null)
            {
                return BadRequest("Tiene una reserva pendiente de aprobación para este comercio en esta fecha. Seleccione otra fecha.");
            }

            var reservaAprobada = await _context.Reserva
                .AsNoTracking()
                .Where(x => x.ID_Usuario == reserva.ID_Usuario &&
                           x.ID_Comercio == reserva.ID_Comercio &&
                           x.FechaReserva.Date == reserva.FechaReserva.Date &&
                           x.Estado == true) // Aprobada
                .FirstOrDefaultAsync();

            if (reservaAprobada != null)
            {
                return BadRequest("Ya tiene una reserva aprobada para este comercio en esta fecha. Seleccione otra fecha.");
            }
            _context.Reserva.Add(reserva);
            await _context.SaveChangesAsync();

            //return CreatedAtAction("GetIdReserva", new { id = reserva.ID_Reserva }, reserva);
            return Ok("Reserva creada correctamente");
        }
        #endregion

        #region // DELETE: api/reservas/eliminar/{id}
        [HttpDelete]
        [Route("eliminar/{id}")]
        public async Task<IActionResult> DeleteReservation(int id)
        {
            var reserva = await _context.Reserva.FindAsync(id);

            if (reserva == null)
            {
                return NotFound("Reserva no encontrada");
            }

            _context.Reserva.Remove(reserva);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        #endregion

        private bool ReservaExists(int id)
        {
            return (_context.Reserva?
                            .AsNoTracking()
                            .Any(e => e.ID_Reserva == id)).GetValueOrDefault();
        }
    }
}
