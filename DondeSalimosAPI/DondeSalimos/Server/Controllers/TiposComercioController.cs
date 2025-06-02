using DondeSalimos.Server.Data;
using DondeSalimos.Shared.Modelos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DondeSalimos.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TiposComercioController : ControllerBase
    {
        private readonly Contexto _context;

        public TiposComercioController(Contexto context)
        {
            _context = context;
        }

        #region // GET: api/tiposComercio/listado
        [HttpGet]
        [Route("listado")]
        public async Task<ActionResult<List<TipoComercio>>> GetTypeShops()
        {
            if (_context.TipoComercio == null)
            {
                return NotFound();
            }

            return await _context.TipoComercio.ToListAsync();
        }
        #endregion

        #region // GET: api/tiposComercio/buscarIdTipoComercio/{id}
        [HttpGet] //("{id:int}", Name = "GetIdTipoComercio")]
        [Route("buscarIdTipoComercio/{id}")]
        public async Task<ActionResult<TipoComercio>> GetIdTypeShop(int id)
        {
            var tipoComercioId = await _context.TipoComercio.Where(x => x.ID_TipoComercio == id)
                                                .FirstOrDefaultAsync();

            if (tipoComercioId == null)
            {
                return NotFound("Tipo de comercio no encontrado");
            }

            return tipoComercioId;
        }
        #endregion

        #region // GET: api/tiposComercio/buscarNombreTipoComercio/{tipoComercio}
        [HttpGet] //("{tipoComercio}")]
        [Route("buscarNombreTipoComercio/{tipoComercio}")]
        public async Task<ActionResult<List<TipoComercio>>> GetTypeShopByName(string tipoComercio)
        {
            var nombreTipoComercio = await _context.TipoComercio.Where(x => x.Descripcion.ToLower().Contains(tipoComercio))
                                                            .ToListAsync();

            if (nombreTipoComercio == null)
            {
                return NotFound("Tipo de comercio no encontrado");
            }

            return nombreTipoComercio;
        }
        #endregion

        #region // PUT: api/tiposComercio/actualizar/{id}
        [HttpPut] //("{id}")]
        [Route("actualizar/{id}")]
        public async Task<IActionResult> PutTypeShop(int id, TipoComercio tipoComercio)
        {
            if (id != tipoComercio.ID_TipoComercio)
            {
                return BadRequest();
            }

            _context.Entry(tipoComercio).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!TypeShopExists(id))
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

        #region // POST: api/tiposComercio/crear
        [HttpPost]
        [Route("crear")]
        public async Task<ActionResult> PostTypeShop(TipoComercio tipoComercio)
        {
            if (_context.TipoComercio == null)
            {
                return Problem("Entity set 'Contexto.TipoComercio'  is null.");
            }

            try
            {
                _context.TipoComercio.Add(tipoComercio);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }

            //return new CreatedAtRouteResult("GetIdTipoComercio", new { id = tipoComercio.ID_TipoComercio }, tipoComercio);
            return Ok("Tipo de comercio creado correctamente");
        }
        #endregion

        #region // DELETE: api/tiposComercio/eliminar/{id}
        [HttpDelete]//("{id}")]
        [Route("eliminar/{id}")]
        public async Task<IActionResult> DeleteTypeShop(int id)
        {
            var tipoComercio = await _context.TipoComercio.FindAsync(id);

            if (tipoComercio == null)
            {
                return NotFound("Tipo de comercio no encontrado");
            }

            try
            {
                _context.TipoComercio.Remove(tipoComercio);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return NoContent();
        }
        #endregion

        private bool TypeShopExists(int id)
        {
            return _context.TipoComercio.Any(e => e.ID_TipoComercio == id);
        }
    }
}
