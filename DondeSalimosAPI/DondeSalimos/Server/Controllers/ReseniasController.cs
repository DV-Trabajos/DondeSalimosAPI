using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DondeSalimos.Server.Data;
using DondeSalimos.Shared.Modelos;

namespace DondeSalimos.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReseniasController : ControllerBase
    {
        private readonly Contexto _context;

        public ReseniasController(Contexto context)
        {
            _context = context;
        }

        #region // GET: api/resenias/listado
        [HttpGet]
        [Route("listado")]
        public async Task<ActionResult<List<Resenia>>> GetReviews()
        {
            if (_context.Resenia == null)
            {
                return NotFound();
            }

            return await _context.Resenia
                                .Include(x => x.Comercio)
                                .ToListAsync();
        }
        #endregion

        #region // GET: api/resenias/buscarIdResenia/{id}
        [HttpGet] //("{id:int}", Name = "GetIdResenia")]
        [Route("buscarIdResenia/{id}")]
        public async Task<ActionResult<Resenia>> GetIdReview(int id)
        {
            if (_context.Resenia == null)
            {
                return NotFound();
            }

            var resenia = await _context.Resenia.Where(x => x.ID_Resenia == id)
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
        [HttpGet] //("{nombreComercio}")]
        [Route("buscarNombreComercio/{comercio}")]
        public async Task<ActionResult<List<Resenia>>> GetReviewByShopName(string comercio)
        {
            return await _context.Resenia.Where(x => x.Comercio.Nombre.ToLower().Contains(comercio))
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
            if (_context.Resenia == null)
            {
                return Problem("Entity set 'Contexto.Resenia'  is null.");
            }

            try
            {
                _context.Resenia.Add(resenia);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw ex;
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
            if (_context.Resenia == null)
            {
                return NotFound();
            }

            var resenia = await _context.Resenia.FindAsync(id);

            if (resenia == null)
            {
                return NotFound();
            }

            try
            {
                _context.Resenia.Remove(resenia);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return NoContent();
        }
        #endregion

        private bool ReseniaExists(int id)
        {
            return (_context.Resenia?.Any(e => e.ID_Resenia == id)).GetValueOrDefault();
        }
    }
}
