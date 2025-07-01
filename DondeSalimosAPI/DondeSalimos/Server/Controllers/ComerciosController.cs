using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DondeSalimos.Server.Data;
using DondeSalimos.Shared.Modelos;

namespace DondeSalimos.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ComerciosController : ControllerBase
    {
        private readonly Contexto _context;

        public ComerciosController(Contexto context)
        {
            _context = context;
        }

        #region // GET: api/comercios/listado
        [HttpGet]
        [Route("listado")]
        public async Task<ActionResult<IEnumerable<Comercio>>> GetShops()
        {
            if (_context.Comercio == null)
            {
                return NotFound();
            }

            return await _context.Comercio
                                .Include(x => x.TipoComercio)
                                .Include(x => x.Usuario)
                                .ToListAsync();
        }
        #endregion

        #region // GET: api/comercios/buscarIdComercio/{id}
        [HttpGet] //("{id:int}", Name = "GetIdComercio")]
        [Route("buscarIdComercio/{id}")]
        public async Task<ActionResult<Comercio>> GetShopById(int id)
        {
            var comercioId = await _context.Comercio.Where(x => x.ID_Comercio == id)
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
        [HttpGet] //("{comercio}")]
        [Route("buscarNombreComercio/{comercio}")]
        public async Task<ActionResult<List<Comercio>>> GetShopByName(string comercio)
        {
            var comercioNombre = await _context.Comercio.Where(x => x.Nombre.ToLower().Contains(comercio))
                                            .Include(x => x.TipoComercio)
                                            .Include(x => x.Usuario)
                                            .ToListAsync();

            if (comercioNombre == null)
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
            var comercioNombre = await _context.Comercio.Where(x => x.ID_Usuario == usuarioId)
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
            catch (DbUpdateConcurrencyException ex)
            {
                if (!ShopExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw ex;
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
            if (_context.Comercio == null)
            {
                return Problem("Entity set 'Contexto.Comercio'  is null.");
            }

            try
            {
                _context.Comercio.Add(comercio);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }

            //return CreatedAtAction("GetIdComercio", new { id = comercio.ID_Comercio }, comercio);
            return Ok("Comercio creado correctamente");
        }
        #endregion

        #region // DELETE: api/comercios/eliminar/{id}
        [HttpDelete] //("{id}")]
        [Route("eliminar/{id}")]
        public async Task<IActionResult> DeleteShop(int id)
        {
            if (_context.Comercio == null)
            {
                return NotFound();
            }

            var comercio = await _context.Comercio.FindAsync(id);

            if (comercio == null)
            {
                return NotFound();
            }

            try
            {
                _context.Comercio.Remove(comercio);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return NoContent();
        }
        #endregion

        private bool ShopExists(int id)
        {
            return (_context.Comercio?.Any(e => e.ID_Comercio == id)).GetValueOrDefault();
        }
    }
}
