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
        [HttpGet] //("{id:int}", Name = "GetIdReserva")]
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
        [HttpGet] //("{nombreCliente}")]
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

        #region // PUT: api/reservas/actualizar/{id}
        [HttpPut] //("{id}")]
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
        [HttpDelete] //("{id}")]
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
