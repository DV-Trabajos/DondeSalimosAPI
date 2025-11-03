using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DondeSalimos.Server.Data;
using DondeSalimos.Shared.Modelos;
using Microsoft.AspNetCore.Authorization; // Agregar using para Authorization

namespace DondeSalimos.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Proteger el controlador por defecto
    public class ReseniasController : ControllerBase
    {
        private readonly Contexto _context;

        public ReseniasController(Contexto context)
        {
            _context = context;
        }

        #region // GET: api/resenias/listado
        [AllowAnonymous] // Hacer público para leer reseñas
        [HttpGet]
        [Route("listado")]
        public async Task<ActionResult<List<Resenia>>> GetReviews()
        {
            return await _context.Resenia
                                    .AsNoTracking()
                                    .Include(x => x.Comercio)
                                    .ToListAsync();
        }
        #endregion

        #region // GET: api/resenias/buscarIdResenia/{id}
        [AllowAnonymous] // Hacer público para ver detalles de reseñas
        [HttpGet] //("{id:int}", Name = "GetIdResenia")]
        [Route("buscarIdResenia/{id}")]
        public async Task<ActionResult<Resenia>> GetIdReview(int id)
        {
            var resenia = await _context.Resenia
                                            .AsNoTracking()
                                            .Where(x => x.ID_Resenia == id)
                                            .Include(x => x.Comercio)
                                            .FirstOrDefaultAsync();

            if (resenia == null)
            {
                return NotFound("Reseña no encontrada");
            }

            return resenia;
        }
        #endregion

        #region // GET: api/resenias/buscarNombreComercio/{comercio}
        [AllowAnonymous] // Hacer público para ver reseñas por comercio
        [HttpGet] //("{nombreComercio}")]
        [Route("buscarNombreComercio/{comercio}")]
        public async Task<ActionResult<List<Resenia>>> GetReviewByShopName(string comercio)
        {
            return await _context.Resenia
                                    .AsNoTracking()
                                    .Where(x => x.Comercio.Nombre.ToLower().Contains(comercio))
                                    .Include(x => x.Comercio)
                                    .ToListAsync();
        }
        #endregion

        #region // PUT: api/resenias/actualizar/{id}
        [HttpPut] //("{id}")]
        [Route("actualizar/{id}")]
        public async Task<IActionResult> PutReview(int id, Resenia resenia)
        {
            if (id != resenia.ID_Resenia)
            {
                return BadRequest();
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
            //Consultar si la reseña corresponde a un comercio que el usuario haya hecho una reserva
            var reservaUsuario = await _context.Reserva
                                            .AsNoTracking().Where(x => x.ID_Usuario == resenia.ID_Usuario &&
                                                                    x.ID_Comercio == resenia.ID_Comercio)
                                            .Include(x => x.Comercio)
                                            .Include(x => x.Usuario)
                                            .FirstOrDefaultAsync();

            if (reservaUsuario == null)
            {
                return BadRequest("El usuario no tiene una reserva en un comercio para dejar su comentario");
            }
            else
            {
                _context.Resenia.Add(resenia);
                await _context.SaveChangesAsync();
            }

            //return CreatedAtAction("GetIdResenia", new { id = resenia.ID_Resenia }, resenia);
            return Ok("Reseña creada correctamente");
        }
        #endregion

        #region // DELETE: api/resenias/eliminar/{id}
        [HttpDelete] //("{id}")]
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
