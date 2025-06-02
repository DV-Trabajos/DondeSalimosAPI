using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DondeSalimos.Server.Data;
using DondeSalimos.Shared.Modelos;

namespace DondeSalimos.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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
            if (_context.Reserva == null)
            {
                return NotFound();
            }

            return await _context.Reserva
                                .Include(x => x.Comercio)
                                .Include(x => x.Usuario)
                                .ToListAsync();
        }
        #endregion

        #region // GET: api/reservas/buscarIdReserva/{id}
        [HttpGet] //("{id:int}", Name = "GetIdReserva")]
        [Route("buscarIdReserva/{id}")]
        public async Task<ActionResult<Reserva>> GetIdReservation(int id)
        {
            if (_context.Reserva == null)
            {
                return NotFound();
            }

            var reserva = await _context.Reserva.Where(x => x.ID_Reserva == id)
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
            return await _context.Reserva.Where(x => x.Comercio.Nombre.ToLower().Contains(comercio))
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
            if (_context.Reserva == null)
            {
                return Problem("Entity set 'Contexto.Reserva'  is null.");
            }

            try
            {
                _context.Reserva.Add(reserva);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }

            //return CreatedAtAction("GetIdReserva", new { id = reserva.ID_Reserva }, reserva);
            return Ok("Reserva creada correctamente");
        }
        #endregion

        #region // DELETE: api/reservas/eliminar/{id}
        [HttpDelete] //("{id}")]
        [Route("eliminar/{id}")]
        public async Task<IActionResult> DeleteReservation(int id)
        {
            if (_context.Reserva == null)
            {
                return NotFound();
            }

            var reserva = await _context.Reserva.FindAsync(id);

            if (reserva == null)
            {
                return NotFound();
            }

            try
            {
                _context.Reserva.Remove(reserva);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return NoContent();
        }
        #endregion

        private bool ReservaExists(int id)
        {
            return (_context.Reserva?.Any(e => e.ID_Reserva == id)).GetValueOrDefault();
        }
    }
}
