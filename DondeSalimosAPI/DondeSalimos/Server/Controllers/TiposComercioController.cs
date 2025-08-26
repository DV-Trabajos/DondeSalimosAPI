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
            return await _context.TipoComercio
                                    .AsNoTracking()
                                    .ToListAsync();
        }
        #endregion

        #region // GET: api/tiposComercio/buscarIdTipoComercio/{id}
        [HttpGet] //("{id:int}", Name = "GetIdTipoComercio")]
        [Route("buscarIdTipoComercio/{id}")]
        public async Task<ActionResult<TipoComercio>> GetIdTypeShop(int id)
        {
            var tipoComercioId = await _context.TipoComercio
                                                    .AsNoTracking()
                                                    .Where(x => x.ID_TipoComercio == id)
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
            var nombreTipoComercio = await _context.TipoComercio
                                                        .AsNoTracking()
                                                        .Where(x => x.Descripcion.ToLower().Contains(tipoComercio))
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
            catch (DbUpdateConcurrencyException)
            {
                if (!TypeShopExists(id))
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

        #region // POST: api/tiposComercio/crear
        [HttpPost]
        [Route("crear")]
        public async Task<ActionResult> PostTypeShop(TipoComercio tipoComercio)
        {
            _context.TipoComercio.Add(tipoComercio);
            await _context.SaveChangesAsync();

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

            _context.TipoComercio.Remove(tipoComercio);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        #endregion

        private bool TypeShopExists(int id)
        {
            return _context.TipoComercio
                             .AsNoTracking()
                             .Any(e => e.ID_TipoComercio == id);
        }
    }
}
