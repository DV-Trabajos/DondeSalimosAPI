using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DondeSalimos.Server.Data;
using DondeSalimos.Shared.Modelos;

namespace DondeSalimos.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PublicidadesController : ControllerBase
    {
        private readonly Contexto _context;

        public PublicidadesController(Contexto context)
        {
            _context = context;
        }

        #region // GET: api/publicidades/listado
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
        [HttpGet] //("{id:int}", Name = "GetIdPublicidad")]
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
        [HttpGet] //("{nombreComercio}")]
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
        [HttpPut] //("{id}")]
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

        #region // POST: api/publicidades/crear
        [HttpPost]
        [Route("crear")]
        public async Task<ActionResult<Publicidad>> PostAdvertising(Publicidad publicidad)
        {
            _context.Publicidad.Add(publicidad);
            await _context.SaveChangesAsync();

            //return CreatedAtAction("GetIdPublicidad", new { id = publicidad.ID_Publicidad }, publicidad);
            return Ok("Publicidad creada correctamente");
        }
        #endregion

        #region // DELETE: api/publicidades/eliminar/{id}
        [HttpDelete] //("{id}")]
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

        private bool PublicidadExists(int id)
        {
            return (_context.Publicidad?
                            .AsNoTracking()
                            .Any(e => e.ID_Publicidad == id)).GetValueOrDefault();
        }
    }
}
