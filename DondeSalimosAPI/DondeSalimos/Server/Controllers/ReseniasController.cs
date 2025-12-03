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
    public class ReseniasController : ControllerBase
    {
        private readonly Contexto _context;

        public ReseniasController(Contexto context)
        {
            _context = context;
        }

        #region // GET: api/resenias/listado
        [AllowAnonymous]
        [HttpGet]
        [Route("listado")]
        public async Task<ActionResult<List<Resenia>>> GetReviews()
        {
            return await _context.Resenia
                                    .AsNoTracking()
                                    .Include(x => x.Comercio)
                                    .Include(x => x.Usuario)
                                    .ToListAsync();
        }
        #endregion

        #region // GET: api/resenias/buscarIdResenia/{id}
        [AllowAnonymous]
        [HttpGet]
        [Route("buscarIdResenia/{id}")]
        public async Task<ActionResult<Resenia>> GetIdReview(int id)
        {
            var resenia = await _context.Resenia
                                            .AsNoTracking()
                                            .Where(x => x.ID_Resenia == id)
                                            .Include(x => x.Comercio)
                                            .Include(x => x.Usuario)
                                            .FirstOrDefaultAsync();

            if (resenia == null)
            {
                return NotFound("Reseña no encontrada");
            }

            return resenia;
        }
        #endregion

        #region // GET: api/resenias/buscarIdComercio/{idComercio}
        [AllowAnonymous]
        [HttpGet]
        [Route("buscarIdComercio/{idComercio}")]
        public async Task<ActionResult<List<Resenia>>> GetReviewByShopId(int idComercio)
        {
            var resenias = await _context.Resenia
                                    .AsNoTracking()
                                    .Where(x => x.ID_Comercio == idComercio)
                                    .Include(x => x.Comercio)
                                    .Include(x => x.Usuario)
                                    .ToListAsync();

            return Ok(resenias);
        }
        #endregion

        #region // GET: api/resenias/buscarNombreComercio/{comercio}
        [AllowAnonymous]
        [HttpGet]
        [Route("buscarNombreComercio/{comercio}")]
        public async Task<ActionResult<List<Resenia>>> GetReviewByShopName(string comercio)
        {
            return await _context.Resenia
                                    .AsNoTracking()
                                    //.Where(x => x.Comercio.Nombre.ToLower().Contains(comercio))
                                    .Where(x => x.Comercio.Nombre.ToLower() == comercio.ToLower())

                                    .Include(x => x.Comercio)
                                    .Include(x => x.Usuario)
                                    .ToListAsync();
        }
        #endregion

        #region // PUT: api/resenias/actualizar/{id}
        [HttpPut]
        [Route("actualizar/{id}")]
        public async Task<IActionResult> PutReview(int id, Resenia resenia)
        {

            if (resenia.Puntuacion < 1 || resenia.Puntuacion > 5)
            {
                return BadRequest("La puntuación debe estar entre 1 y 5");
            }

            if (id != resenia.ID_Resenia)
            {
                return BadRequest();
            }
            if (resenia.Puntuacion < 1 || resenia.Puntuacion > 5)
            {
                return BadRequest("La puntuación debe estar entre 1 y 5");
            }

            _context.Entry(resenia).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ReseniaExists(id))
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

        #region // POST: api/resenias/crear
        [HttpPost]
        [Route("crear")]
        public async Task<ActionResult<Resenia>> PostReview(Resenia resenia)
        {
            if (resenia.Puntuacion < 1 || resenia.Puntuacion > 5)
            {
                return BadRequest("La puntuación debe estar entre 1 y 5");
            }

            var usuario = await _context.Usuario.FindAsync(resenia.ID_Usuario);
            if (usuario == null || usuario.Estado == false)
            {
                return BadRequest("No puedes crear una reseña porque tu cuenta está inactiva.");
            }

            var comercio = await _context.Comercio.FindAsync(resenia.ID_Comercio);
            if (comercio == null || comercio.Estado == false)
            {
                return BadRequest("El comercio no existe o está inactivo.");
            }

            // Estado == true significa aprobada, Estado == false con MotivoRechazo != null es rechazada
            var reservaAprobada = await _context.Reserva
                                            .AsNoTracking()
                                            .Where(x => x.ID_Usuario == resenia.ID_Usuario &&
                                                       x.ID_Comercio == resenia.ID_Comercio &&
                                                       x.Estado == true)
                                            .FirstOrDefaultAsync();

            if (reservaAprobada == null)
            {
                return BadRequest("No tienes una reserva aprobada en este comercio para poder dejar una reseña.");
            }

            // Solo puede crear una nueva si la anterior fue rechazada (Estado == false y MotivoRechazo != null)
            var reseniaExistente = await _context.Resenia
                                            .AsNoTracking()
                                            .Where(x => x.ID_Usuario == resenia.ID_Usuario &&
                                                       x.ID_Comercio == resenia.ID_Comercio &&
                                                       (x.Estado == true || (x.Estado == false && x.MotivoRechazo == null)))
                                            .FirstOrDefaultAsync();

            if (reseniaExistente != null)
            {
                return BadRequest("Ya tienes una reseña aprobada o pendiente para este comercio. Solo puedes crear una nueva si la anterior fue rechazada.");
            }

            resenia.FechaCreacion = DateTime.Now;
            resenia.Estado = false; // Pendiente de aprobación
            resenia.MotivoRechazo = null;

            _context.Resenia.Add(resenia);
            await _context.SaveChangesAsync();

            return Ok("Reseña creada correctamente y está pendiente de aprobación.");
        }
        #endregion

        #region // DELETE: api/resenias/eliminar/{id}
        [HttpDelete]
        [Route("eliminar/{id}")]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var resenia = await _context.Resenia.FindAsync(id);

            if (resenia == null)
            {
                return NotFound("Reseña no encontrada");
            }

            _context.Resenia.Remove(resenia);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        #endregion

        private bool ReseniaExists(int id)
        {
            return (_context.Resenia?
                            .AsNoTracking()
                            .Any(e => e.ID_Resenia == id)).GetValueOrDefault();
        }
    }
}
