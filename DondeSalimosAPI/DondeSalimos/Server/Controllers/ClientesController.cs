using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DondeSalimos.Server.Data;
using DondeSalimos.Shared.Modelos;

namespace DondeSalimos.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientesController : ControllerBase
    {
        private readonly Contexto _context;

        public ClientesController(Contexto context)
        {
            _context = context;
        }

        #region // GET: api/clientes
        [HttpGet]
        public async Task<ActionResult<List<Cliente>>> GetClientes()
        {
            if (_context.Cliente == null)
            {
                return NotFound();
            }

            return await _context.Cliente
                                .Include(x => x.Usuario)
                                .ToListAsync();
        }
        #endregion

        #region // GET: api/clientes/{id}
        [HttpGet("{id:int}", Name = "GetIdCliente")]
        public async Task<ActionResult<Cliente>> GetIdCliente(int id)
        {
            if (_context.Cliente == null)
            {
                return NotFound();
            }

            var cliente = await _context.Cliente.Where(x => x.ID_Cliente == id)
                                                .Include(x => x.Usuario)
                                                .FirstOrDefaultAsync();

            if (cliente == null)
            {
                return NotFound();
            }

            return cliente;
        }
        #endregion

        #region // GET: api/clientes/{nombreCliente}
        [HttpGet("{nombreCliente}")]
        public async Task<ActionResult<List<Cliente>>> GetUserCliente(string nombreCliente)
        {
            return await _context.Cliente
                                    .Where(x => x.Nombre.ToLower().Contains(nombreCliente))
                                    .Include(x => x.Usuario)
                                    .ToListAsync();
        }
        #endregion

        #region // PUT: api/clientes/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCliente(int id, Cliente cliente)
        {
            if (id != cliente.ID_Cliente)
            {
                return BadRequest();
            }

            _context.Entry(cliente).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ClienteExists(id))
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

        #region // POST: api/clientes
        [HttpPost]
        public async Task<ActionResult<Cliente>> PostCliente(Cliente cliente)
        {
            if (_context.Cliente == null)
            {
                return Problem("Entity set 'Contexto.Cliente'  is null.");
            }

            try
            {
                _context.Cliente.Add(cliente);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return CreatedAtAction("GetIdCliente", new { id = cliente.ID_Cliente }, cliente);
        }
        #endregion

        #region // DELETE: api/clientes/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCliente(int id)
        {
            if (_context.Cliente == null)
            {
                return NotFound();
            }

            var cliente = await _context.Cliente.FindAsync(id);

            if (cliente == null)
            {
                return NotFound();
            }

            try
            {
                _context.Cliente.Remove(cliente);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return NoContent();
        }
        #endregion

        private bool ClienteExists(int id)
        {
            return _context.Cliente.Any(x => x.ID_Cliente == id);
        }
    }
}
