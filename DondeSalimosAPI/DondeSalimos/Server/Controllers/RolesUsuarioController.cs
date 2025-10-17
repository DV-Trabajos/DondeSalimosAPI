using DondeSalimos.Server.Data;
using DondeSalimos.Shared.Modelos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DondeSalimos.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RolesUsuarioController : ControllerBase
    {
        private readonly Contexto _context;

        public RolesUsuarioController(Contexto context)
        {
            _context = context;
        }

        #region // GET: api/rolesUsuario/listado
        [HttpGet]
        [Route("listado")]
        public async Task<ActionResult<List<RolUsuario>>> GetUserRoles()
        {
            return await _context.RolUsuario
                                    .AsNoTracking()
                                    .ToListAsync();
        }
        #endregion

        #region // GET: api/rolesUsuario/buscarIdRolUsuario/{id}
        [HttpGet] //("{id:int}", Name = "GetIdUsuario")]
        [Route("buscarIdRolUsuario/{id}")]
        public async Task<ActionResult<RolUsuario>> GetIdRol(int id)
        {
            var rolId = await _context.RolUsuario
                                        .AsNoTracking()
                                        .Where(x => x.ID_RolUsuario == id)
                                        .FirstOrDefaultAsync();

            if (rolId == null)
            {
                return NotFound("Rol no encontrado");
            }

            return rolId;
        }
        #endregion

        #region // GET: api/rolesUsuario/buscarNombreRolUsuario/{rolUsuario}
        [HttpGet] //("{rolUsuario}")]
        [Route("buscarNombreRolUsuario/{rolUsuario}")]
        public async Task<ActionResult<List<RolUsuario>>> GetUserRolByName(string rolUsuario)
        {
            var nombreRolUsuario = await _context.RolUsuario
                                                    .AsNoTracking()
                                                    .Where(x => x.Descripcion.ToLower().Contains(rolUsuario))
                                                    .ToListAsync();

            if (nombreRolUsuario == null)
            {
                return NotFound("Rol de usuario no encontrado");
            }

            return nombreRolUsuario;
        }
        #endregion

        #region // PUT: api/rolesUsuario/actualizar/{id}
        [HttpPut] //("{id}")]
        [Route("actualizar/{id}")]
        public async Task<IActionResult> PutUserRol(int id, RolUsuario rolUsuario)
        {
            if (id != rolUsuario.ID_RolUsuario)
            {
                return BadRequest();
            }

            _context.Entry(rolUsuario).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserRolExists(id))
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

        #region // POST: api/rolesUsuario/crear
        [HttpPost]
        [Route("crear")]
        public async Task<ActionResult> PostUserRol(RolUsuario rolUsuario)
        {
            _context.RolUsuario.Add(rolUsuario);
            await _context.SaveChangesAsync();

            //return new CreatedAtRouteResult("GetIdUsuario", new { id = usuario.ID_Usuario }, usuario);
            return Ok("Rol usuario creado correctamente");
        }
        #endregion

        #region // DELETE: api/rolesUsuario/eliminar/{id}
        [HttpDelete]//("{id}")]
        [Route("eliminar/{id}")]
        public async Task<IActionResult> DeleteUserRol(int id)
        {
            var rolUsuario = await _context.RolUsuario.FindAsync(id);

            if (rolUsuario == null)
            {
                return NotFound("Rol usuario no encontrado");
            }

            try
            {
                _context.RolUsuario.Remove(rolUsuario);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(400, $"Error al eliminar rol: {ex.Message}");
            }            
        }
        #endregion

        private bool UserRolExists(int id)
        {
            return _context.RolUsuario
                           .AsNoTracking()
                           .Any(e => e.ID_RolUsuario == id);
        }
    }
}
