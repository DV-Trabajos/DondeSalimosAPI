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
    public class ComerciosController : ControllerBase
    {
        private readonly Contexto _context;

        public ComerciosController(Contexto context)
        {
            _context = context;
        }

        #region // GET: api/comercios/listado
        [AllowAnonymous] // Hacer público para explorar bares
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
        [AllowAnonymous] // Hacer público para ver detalles de bares
        [HttpGet] //("{id:int}", Name = "GetIdComercio")]
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
        [AllowAnonymous] // Hacer público para búsquedas
        [HttpGet] //("{comercio}")]
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

            if (!comercioNombre.Any()) // Corregir la lógica de NotFound
            {
                return NotFound("Comercio no encontrado");
            }

            return comercioNombre;
        }
        #endregion

        #region // GET: api/comercios/buscarComerciosPorUsuario/{usuarioId}
        [HttpGet] //("{comercio}")]
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

        #region // PUT: api/comercios/actualizar/{id}
        [HttpPut] //("{id}")]
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
            var comercioCUIT = await _context.Comercio
                                                .AsNoTracking()
                                                .Where(x => x.NroDocumento.Contains(comercio.NroDocumento))
                                                .Include(x => x.TipoComercio)
                                                .Include(x => x.Usuario)
                                                .ToListAsync();

            if (comercioCUIT.Any()) // Corregir la lógica de BadRequest
            {
                return BadRequest("Existe comercio con el mismo CUIT");
            }

            _context.Comercio.Add(comercio);
            await _context.SaveChangesAsync();

            //return CreatedAtAction("GetIdComercio", new { id = comercio.ID_Comercio }, comercio);
            return Ok("Comercio creado correctamente");
        }
        #endregion

        #region // DELETE: api/comercios/eliminar/{id}
        [HttpDelete] //("{id}")]
        [Route("eliminar/{id}")]
        public async Task<IActionResult> DeleteShop(int id)
        {
            var comercio = await _context.Comercio.FindAsync(id);

            if (comercio == null)
            {
                return NotFound("Comercio no encontrado");
            }

            _context.Comercio.Remove(comercio);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        #endregion

        private bool ShopExists(int id)
        {
            return (_context.Comercio?
                            .AsNoTracking()
                            .Any(e => e.ID_Comercio == id)).GetValueOrDefault();
        }
    }
}
