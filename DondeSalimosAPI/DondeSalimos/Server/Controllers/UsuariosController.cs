using DondeSalimos.Server.Data;
using DondeSalimos.Server.Services;
using DondeSalimos.Shared.Modelos;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens; 
using System.IdentityModel.Tokens.Jwt; 
using System.Security.Claims; 
using System.Text; 

namespace DondeSalimos.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        private readonly Contexto _context;
        private readonly FirebaseService _firebaseService;
        private readonly IConfiguration _configuration;

        private static readonly HashSet<string> PalabrasOfensivas = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Palabras ofensivas comunes
            "puto", "puta", "hijo de puta", "hdp", "mierda", "carajo", "concha",
            "pelotudo", "boludo", "idiota", "imbecil", "estupido", "tarado","culo", "pene",
            "gay", "maricon", "trolo", "puto", "pendejo", "gilipollas","pene",
            "nazi", "hitler", "racista", "terrorista", "admin", "administrador",
            "moderador", "soporte", "staff", "oficial"
        };

        // Método helper para validar palabras ofensivas
        private bool ContienePalabrasOfensivas(string texto)
        {
            if (string.IsNullOrEmpty(texto))
                return false;

            var textoLower = texto.ToLower();

            foreach (var palabra in PalabrasOfensivas)
            {
                if (textoLower.Contains(palabra.ToLower()))
                {
                    return true;
                }
            }

            return false;
        }
        public UsuariosController(Contexto context, FirebaseService firebaseService, IConfiguration configuration)       
        {
            _context = context;
            _firebaseService = firebaseService;
            _configuration = configuration; 
        }

        #region // GET: api/usuarios/listado
        [HttpGet]
        [Route("listado")]
        public async Task<ActionResult<List<Usuario>>> GetUsers()
        {
            return await _context.Usuario
                                    .AsNoTracking()
                                    .Include(x => x.RolUsuario)
                                    .ToListAsync();
        }
        #endregion

        #region // GET: api/usuarios/buscarIdUsuario/{id}
        [HttpGet]
        [Route("buscarIdUsuario/{id}")]
        public async Task<ActionResult<Usuario>> GetUserById(int id)
        {
            var usuarioId = await _context.Usuario
                                            .AsNoTracking()
                                            .Where(x => x.ID_Usuario == id)
                                            .Include(x => x.RolUsuario)
                                            .FirstOrDefaultAsync();

            if (usuarioId == null)
            {
                return NotFound("Usuario no encontrado");
            }

            return usuarioId;
        }
        #endregion

        #region // GET: api/usuarios/buscarNombreUsuario/{usuario}
        [HttpGet]
        [Route("buscarNombreUsuario/{usuario}")]
        public async Task<ActionResult<List<Usuario>>> GetUserByName(string usuario)
        {
            var filter = usuario.ToLower();
            var usuarioNombre = await _context.Usuario
                                                .AsNoTracking()
                                                .Where(x => x.NombreUsuario.ToLower().Contains(filter))
                                                .Include(x => x.RolUsuario)
                                                .ToListAsync();

            if (usuarioNombre.Any())
            {
                return NotFound("Usuario no encontrado");
            }

            return usuarioNombre;
        }
        #endregion

        #region // GET: api/usuarios/buscarEmail/{email}
        [HttpGet]
        [Route("buscarEmail/{email}")]
        public async Task<ActionResult<Usuario>> GetUsuarioByEmail(string email)
        {
            var usuario = await _context.Usuario
                                        .AsNoTracking()
                                        .Where(x => x.Correo.ToLower() == email.ToLower())
                                        .FirstOrDefaultAsync();

            if (usuario == null)
            {
                return NotFound("Usuario no encontrado");
            }

            return usuario;
        }
        #endregion

        #region // PUT: api/usuarios/actualizar/{id}
        [HttpPut]
        [Route("actualizar/{id}")]
        public async Task<IActionResult> PutUser(int id, Usuario usuarioDto)
        {
            var usuario = await _context.Usuario.FindAsync(id);

            try
            {
                if (usuario == null)
                {
                    return NotFound("Usuario no encontrado");
                }

                if (ContienePalabrasOfensivas(usuarioDto.NombreUsuario))
                {
                    return BadRequest("El nombre de usuario contiene palabras no permitidas.");
                }

                // Verificar si el nombre de usuario ya existe (excluyendo el usuario actual)
                var existingUser = await _context.Usuario.FirstOrDefaultAsync(x => x.NombreUsuario == usuarioDto.NombreUsuario && x.ID_Usuario != id);

                if (existingUser != null)
                {
                    return BadRequest("El nombre de usuario ya está en uso");
                }

                // Solo actualizar en Firebase si el correo o nombre de usuario cambiaron
                if (!string.IsNullOrEmpty(usuario.Uid) &&
                    (usuario.Correo != usuarioDto.Correo || usuario.NombreUsuario != usuarioDto.NombreUsuario))
                {
                    await _firebaseService.UpdateUserAsync(
                        usuario.Uid,
                        usuarioDto.Correo,
                        usuarioDto.NombreUsuario);
                }
                

                // Actualizar en la base de datos
                usuario.NombreUsuario = usuarioDto.NombreUsuario ?? usuario.NombreUsuario;
                usuario.Correo = usuarioDto.Correo ?? usuario.Correo;
                usuario.Telefono = usuarioDto.Telefono ?? usuario.Telefono;
                usuario.Estado = usuarioDto.Estado;

                if (usuarioDto.ID_RolUsuario > 0)
                {
                    usuario.ID_RolUsuario = usuarioDto.ID_RolUsuario;
                }

                _context.Entry(usuario).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al actualizar usuario: {ex.Message}");
            }
        }
        #endregion

        #region // PUT: api/usuarios/desactivar/{id}
        [HttpPut]
        [Route("desactivar/{id}")]
        public async Task<IActionResult> DesactivarUsuario(int id)
        {
            var usuario = await _context.Usuario.FindAsync(id);

            if (usuario == null)
            {
                return NotFound("Usuario no encontrado");
            }

            try
            {
                // Desactivar en Firebase
                if (!string.IsNullOrEmpty(usuario.Uid))
                {
                    await _firebaseService.UpdateUserAsync(
                        usuario.Uid,
                        disabled: true);
                }

                // Desactivar en la base de datos
                usuario.Estado = false;

                _context.Entry(usuario).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al desactivar usuario: {ex.Message}");
            }
        }
        #endregion

        #region // PUT: api/usuarios/cambiarEstado/{id}
        [HttpPut]
        [Route("cambiarEstado/{id}")]
        public async Task<IActionResult> CambiarEstadoUsuario(int id, [FromBody] changeStatusRequest request)
        {
            var usuario = await _context.Usuario.FindAsync(id);

            if (usuario == null)
            {
                return NotFound("Usuario no encontrado");
            }

            try
            {
                // Actualizar en Firebase si es necesario
                if (!string.IsNullOrEmpty(usuario.Uid))
                {
                    await _firebaseService.UpdateUserAsync(
                        usuario.Uid,
                        disabled: !request.Estado); // Si estado=true, disabled=false en Firebase
                }

                // Actualizar en la base de datos
                usuario.Estado = request.Estado;

                _context.Entry(usuario).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al cambiar estado de usuario: {ex.Message}");
            }
        }
        #endregion

        #region // POST: api/usuarios/iniciarSesionConGoogle
        [HttpPost("iniciarSesionConGoogle")]
        public async Task<ActionResult<SignInWithGoogleResponse>> SignInWithGoogle(SignInWithGoogleRequest request)
        {
            try
            {
                // Verificar que el token de Google sea válido en Firebase
                var firebase = await _firebaseService.VerifyGoogleTokenAsync(request.IdToken);

                //Verificar si el token es válido
                if (!firebase.IsValidGoogleToken)
                {
                    return BadRequest(new SignInWithGoogleResponse
                    {
                        Usuario = null,
                        ExisteUsuario = false,
                        Mensaje = firebase.Mensaje ?? "Token de Google inválido"
                    });
                }

                //Sino existe en Firebase es porque debe registrarse
                if (!firebase.UserExistsInFirebase)
                {
                    return BadRequest(new SignInWithGoogleResponse
                    {
                        Usuario = null,
                        ExisteUsuario = false,
                        Mensaje = "Usuario no existe, debe registrarse"
                    });
                }

                // Obtener información del usuario de Firebase en nuestra BD
                var firebaseUid = firebase.FirebaseUser.Uid;
                var usuario     = await _context.Usuario
                                                .Include(u => u.RolUsuario)
                                                .FirstOrDefaultAsync(x => x.Uid == firebaseUid);
                var jwtToken = GenerateJwtToken(usuario);
                return Ok(new SignInWithGoogleResponse
                {
                    Usuario = usuario,
                    ExisteUsuario = true,
                    Mensaje = "Inicio de sesión exitoso",
                    JwtToken = jwtToken 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new SignInWithGoogleResponse
                {
                    Usuario = null,
                    ExisteUsuario = false,
                    Mensaje = $"Error interno del servidor: {ex.Message}"
                });
            }
        }
        #endregion

        #region // POST: api/usuarios/registrarseConGoogle
        [HttpPost("registrarseConGoogle")]
        public async Task<ActionResult<SignUpWithGoogleResponse>> SignUpWithGoogle(SignUpWithGoogleRequest request)
        {
            try
            {
                // Verificar que el token de Google sea válido en Firebase
                var firebase = await _firebaseService.VerifyGoogleTokenAsync(request.IdToken);

                //Si existe debe iniciar sesión
                if (firebase.UserExistsInFirebase)
                {
                    return StatusCode(StatusCodes.Status409Conflict, new SignUpWithGoogleResponse
                    {
                        Usuario = null,
                        Mensaje = "Usuario existente, debe iniciar sesión"
                    });
                }

                // Crear usuario en Firebase (Admin SDK: creás cuenta básica por email)
                string email = (string)firebase.Claims.GetValueOrDefault("email");
                string displayName = (string)firebase.Claims.GetValueOrDefault("name", email);
                string photoUrl = (string)firebase.Claims.GetValueOrDefault("picture");
                bool emailVerified = (bool)firebase.Claims.GetValueOrDefault("email_verified", false);

                // El usuario no existe, crearlo
                var userArgs = new UserRecordArgs
                {
                    Email = email,
                    DisplayName = displayName,
                    PhotoUrl = photoUrl,
                    EmailVerified = emailVerified,
                    Disabled = false
                };

                UserRecord userRecord = await _firebaseService.CreateUserWithGoogleAsync(userArgs);

                // Crear nuevo usuario
                var nuevoUsuario = new Usuario
                {
                    NombreUsuario = email.Split('@')[0], // Usar usuario del email
                    Correo = email,
                    //Telefono = string.Empty, // Se puede actualizar después en el perfil
                    ID_RolUsuario = request.RolUsuario,
                    Uid = userRecord.Uid,
                    Estado = true,
                    FechaCreacion = DateTime.Now
                };

                // Guardar en la base de datos
                _context.Usuario.Add(nuevoUsuario);
                await _context.SaveChangesAsync();

                var jwtToken = GenerateJwtToken(nuevoUsuario);
                return Ok(new SignUpWithGoogleResponse
                {
                    Usuario = nuevoUsuario,
                    Mensaje = "Inicio de sesión exitoso",

                    JwtToken = jwtToken
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al registrarse con Google: {ex.Message}");
            }
        }
        #endregion

        #region // DELETE: api/usuarios/eliminar/{id}
        [HttpDelete]
        [Route("eliminar/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var usuario = await _context.Usuario.FindAsync(id);



            if (usuario == null)
            {
                return NotFound("Usuario no encontrado");
            }

            var uid = usuario.Uid;

            try
            {
                // 1. ELIMINAR RESEÑAS CREADAS POR EL USUARIO
                var reseniasUsuario = await _context.Resenia
                    .Where(r => r.ID_Usuario == id)
                    .ToListAsync();

                if (reseniasUsuario.Any())
                {
                    _context.Resenia.RemoveRange(reseniasUsuario);
                    await _context.SaveChangesAsync();
                }

                // 2. ELIMINAR RESERVAS DEL USUARIO
                var reservasUsuario = await _context.Reserva
                    .Where(r => r.ID_Usuario == id)
                    .ToListAsync();

                if (reservasUsuario.Any())
                {
                    _context.Reserva.RemoveRange(reservasUsuario);
                    await _context.SaveChangesAsync();
                }

                // 3. SI ES DUEÑO DE COMERCIO, ELIMINAR COMERCIOS Y SUS DEPENDENCIAS
                var comerciosUsuario = await _context.Comercio
                    .Where(c => c.ID_Usuario == id)
                    .ToListAsync();

                if (comerciosUsuario.Any())
                {
                    foreach (var comercio in comerciosUsuario)
                    {
                        // 3.1. Eliminar reservas del comercio
                        var reservasComercio = await _context.Reserva
                            .Where(r => r.ID_Comercio == comercio.ID_Comercio)
                            .ToListAsync();

                        if (reservasComercio.Any())
                        {
                            _context.Reserva.RemoveRange(reservasComercio);
                            await _context.SaveChangesAsync();
                        }

                        // 3.2. Eliminar publicidades del comercio
                        var publicidadesComercio = await _context.Publicidad
                            .Where(p => p.ID_Comercio == comercio.ID_Comercio)
                            .ToListAsync();

                        if (publicidadesComercio.Any())
                        {
                            _context.Publicidad.RemoveRange(publicidadesComercio);
                            await _context.SaveChangesAsync();
                        }

                        // 3.3. Eliminar reseñas del comercio
                        var reseniasComercio = await _context.Resenia
                            .Where(r => r.ID_Comercio == comercio.ID_Comercio)
                            .ToListAsync();

                        if (reseniasComercio.Any())
                        {
                            _context.Resenia.RemoveRange(reseniasComercio);
                            await _context.SaveChangesAsync();
                        }

                        // 3.4. Eliminar el comercio
                        _context.Comercio.Remove(comercio);
                        await _context.SaveChangesAsync();
                    }
                }

                // 4. ELIMINAR EL USUARIO DE LA BASE DE DATOS
                _context.Usuario.Remove(usuario);
                await _context.SaveChangesAsync();

                // 5. ELIMINAR EN FIREBASE
                if (!string.IsNullOrEmpty(uid))
                {
                    await _firebaseService.DeleteUserAsync(uid);
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error completo al eliminar usuario: {ex}");
                return StatusCode(400, $"Error al eliminar usuario: {ex.Message}");
            }
        }
        #endregion


        private string GenerateJwtToken(Usuario usuario)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, usuario.ID_Usuario.ToString()),
                    new Claim(ClaimTypes.Email, usuario.Correo),
                    new Claim(ClaimTypes.Role, usuario.ID_RolUsuario.ToString())
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature
                )
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

       
        [HttpPost("login-test")]
        public async Task<ActionResult> LoginTest([FromBody] LoginTestRequest request)
        {
            try
            {
                var usuario = await _context.Usuario
                                            .Include(u => u.RolUsuario)
                                            .FirstOrDefaultAsync(x => x.Correo == request.Email);

                if (usuario == null)
                {
                    return NotFound(new { mensaje = "Usuario no encontrado" });
                }

                var jwtToken = GenerateJwtToken(usuario);

                return Ok(new
                {
                    token = jwtToken,
                    usuario = usuario,
                    mensaje = "Token generado correctamente (TEST)"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
        public class SignInWithGoogleRequest
        {
            public string IdToken { get; set; }
        }

        public class SignUpWithGoogleRequest
        {
            public string IdToken { get; set; }

            public int RolUsuario { get; set; } = 1;
        }

        public class SignInWithGoogleResponse
        {
            public Usuario Usuario { get; set; }
            public Boolean ExisteUsuario { get; set; }
            public string Mensaje { get; set; }

            public string JwtToken { get; set; } 
        }

        public class SignUpWithGoogleResponse
        {
            public Usuario Usuario { get; set; }
            public string Mensaje { get; set; }

            public string JwtToken { get; set; } 
        }
        
        public class LoginTestRequest
        {
            public string Email { get; set; }
        }

        public class changeStatusRequest
        {
            public bool Estado { get; set; }
        }

    }

}
