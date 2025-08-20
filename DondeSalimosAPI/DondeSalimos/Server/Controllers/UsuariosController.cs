using DondeSalimos.Server.Data;
using DondeSalimos.Server.Services;
using DondeSalimos.Shared.Modelos;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DondeSalimos.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        private readonly Contexto _context;
        private readonly FirebaseService _firebaseService;

        public UsuariosController(Contexto context, FirebaseService firebaseService)
        {
            _context = context;
            _firebaseService = firebaseService;
        }

        #region // GET: api/usuarios/listado
        [HttpGet]
        [Route("listado")]
        public async Task<ActionResult<List<Usuario>>> GetUsers()
        {
            if (_context.Usuario == null)
            {
                return NotFound();
            }

            return await _context.Usuario
                                .Include(x => x.RolUsuario)
                                .ToListAsync();
        }
        #endregion

        #region // GET: api/usuarios/buscarIdUsuario/{id}
        [HttpGet] //("{id:int}", Name = "GetUserById")]
        [Route("buscarIdUsuario/{id}")]
        public async Task<ActionResult<Usuario>> GetUserById(int id)
        {
            var usuarioId = await _context.Usuario.Where(x => x.ID_Usuario == id)
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
        [HttpGet] //("{usuario}")]
        [Route("buscarNombreUsuario/{usuario}")]
        public async Task<ActionResult<List<Usuario>>> GetUserByName(string usuario)
        {
            var usuarioNombre = await _context.Usuario.Where(x => x.NombreUsuario.ToLower().Contains(usuario))
                                        .Include(x => x.RolUsuario)
                                        .ToListAsync();

            if (usuarioNombre == null)
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
        [HttpPut] //("{id}")]
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

        #region // POST: api/usuarios/autenticacionConGoogle
        /*[HttpPost("autenticacionConGoogle")]
        public async Task<ActionResult<AuthenticationWithGoogleResponse>> AuthenticationWithGoogle(AuthenticationWithGoogleRequest request)
        {
            try
            {
                // Verificar el token de ID de Google con Firebase
                var firebaseToken = await _firebaseService.VerifyGoogleTokenAsync(request.IdToken);

                if (firebaseToken == null)
                {
                    return BadRequest("Token de Google inválido");
                }

                // Obtener información del usuario de Firebase
                var firebaseUid = firebaseToken.Uid;
                var email = firebaseToken.Claims.GetValueOrDefault("email")?.ToString();
                var nombre = firebaseToken.Claims.GetValueOrDefault("name")?.ToString();

                if (string.IsNullOrEmpty(email))
                {
                    return BadRequest("No se pudo obtener el email del usuario de Google");
                }

                // Verificar si el usuario ya existe en nuestra base de datos
                var usuarioExistente = await _context.Usuario
                                                    .Include(u => u.RolUsuario)
                                                    .FirstOrDefaultAsync(x => x.Uid == firebaseUid);

                if (usuarioExistente != null)
                {
                    // Usuario existente - Login normal
                    return Ok(new AuthenticationWithGoogleResponse
                    {
                        Usuario = usuarioExistente,
                        EsNuevoUsuario = false
                        //Token = await _firebaseService.CreateCustomTokenAsync(firebaseUid)
                    });
                }
                else
                {
                    // Verificar si ya existe un usuario con el mismo email pero diferente UID
                    var usuarioConMismoEmail = await _context.Usuario.FirstOrDefaultAsync(x => x.Correo == email);

                    if (usuarioConMismoEmail != null)
                    {
                        return BadRequest("Ya existe una cuenta registrada con este email");
                    }

                    // Crear nuevo usuario
                    var nuevoUsuario = new Usuario
                    {
                        NombreUsuario = email.Split('@')[0], // Usar usuario del email
                        Correo = email,
                        //Telefono = string.Empty, // Se puede actualizar después en el perfil
                        ID_RolUsuario = request.RolUsuario,
                        Uid = firebaseUid,
                        Estado = true,
                        FechaCreacion = DateTime.Now
                    };

                    // Guardar en la base de datos
                    _context.Usuario.Add(nuevoUsuario);
                    await _context.SaveChangesAsync();

                    // Devolver respuesta con nuevo usuario
                    return Ok(new AuthenticationWithGoogleResponse
                    {
                        Usuario = nuevoUsuario,
                        EsNuevoUsuario = true
                        //Token = await _firebaseService.CreateCustomTokenAsync(firebaseUid)
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al iniciar sesión con Google: {ex.Message}");
            }
        }*/
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

                return Ok(new SignInWithGoogleResponse
                {
                    Usuario = usuario,
                    ExisteUsuario = true,
                    Mensaje = "Inicio de sesión exitoso"
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

                //Crear usuario en firebase con proveedor de Google
                /*var userArgs = new UserRecordArgs
                {
                    Uid = firebase.Claims.GetValueOrDefault("uid", "")?.ToString(),
                    Email = firebase.Claims.GetValueOrDefault("email", "")?.ToString(),
                    DisplayName = firebase.Claims.GetValueOrDefault("name", "")?.ToString(),
                    PhotoUrl = firebase.Claims.GetValueOrDefault("picture", "")?.ToString(),
                    EmailVerified = (bool)firebase.Claims.GetValueOrDefault("email_verified", false), 
                    Disabled = false,
                    // IMPORTANTE: Agregar el proveedor de Google
                    ProviderData = new List<UserProvider>
                    {
                        new UserProvider
                        {
                            ProviderId = "google.com",
                            Uid = googlePayload.Subject, // Google UID
                            Email = googlePayload.Email,
                            DisplayName = googlePayload.Name,
                            PhotoUrl = googlePayload.Picture
                        }
                    }
                };*/

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
                    // Nota: vincular el proveedor 'google.com' se hace del lado cliente con Firebase Auth;
                    // el Admin SDK no "loguea" con Google ni crea la vinculación OIDC en este punto.
                };
                /*var userArgs = new UserRecordArgs
                {
                    Email = firebase.Claims.GetValueOrDefault("email", "")?.ToString(), //payload.Email,
                    DisplayName = firebase.Claims.GetValueOrDefault("name", "")?.ToString(), //payload.Name,
                    PhotoUrl = firebase.Claims.GetValueOrDefault("picture", "")?.ToString(), //payload.Picture,
                    EmailVerified = (bool)firebase.Claims.GetValueOrDefault("email_verified", false), //payload.EmailVerified
                };*/

                UserRecord userRecord = await _firebaseService.CreateUserWithGoogleAsync(userArgs);

                // Obtener información del usuario de Firebase
                //var firebaseUid = firebase.Uid;
                //var email       = firebase.Claims.GetValueOrDefault("email")?.ToString();
                //var nombre      = firebase.Claims.GetValueOrDefault("name")?.ToString();

                // Crear nuevo usuario
                var nuevoUsuario = new Usuario
                {
                    NombreUsuario = email.Split('@')[0], // Usar usuario del email
                    Correo = email,
                    //Telefono = string.Empty, // Se puede actualizar después en el perfil
                    //ID_RolUsuario = request.RolUsuario,
                    Uid = userRecord.Uid,
                    Estado = true,
                    FechaCreacion = DateTime.Now
                };

                // Guardar en la base de datos
                _context.Usuario.Add(nuevoUsuario);
                await _context.SaveChangesAsync();

                return Ok(new SignUpWithGoogleResponse
                {
                    Usuario = nuevoUsuario,
                    Mensaje = "Inicio de sesión exitoso"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al registrarse con Google: {ex.Message}");
            }
        }
        #endregion

        #region // POST: api/usuarios/desactivar/{id}
        [HttpPost]
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

        #region // DELETE: api/usuarios/eliminar/{id}
        [HttpDelete]//("{id}")]
        [Route("eliminar/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var usuario = await _context.Usuario.FindAsync(id);
            var uid = usuario.Uid;

            if (usuario == null)
            {
                return NotFound("Usuario no encontrado");
            }

            try
            {
                // Eliminar en la base de datos
                _context.Usuario.Remove(usuario);
                await _context.SaveChangesAsync();

                // Eliminar en Firebase
                if (!string.IsNullOrEmpty(uid))
                {
                    await _firebaseService.DeleteUserAsync(uid);
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(400, $"Error al eliminar usuario: {ex.Message}");
            }
        }
        #endregion

        public class AuthenticationWithGoogleRequest
        {
            public string IdToken { get; set; }

            public int RolUsuario { get; set; } = 1;
        }

        public class AuthenticationWithGoogleResponse
        {
            public Usuario Usuario { get; set; }
            public Boolean EsNuevoUsuario { get; set; }
        }

        public class SignInWithGoogleRequest
        {
            public string IdToken { get; set; }
        }

        public class SignUpWithGoogleRequest
        {
            public string IdToken { get; set; }
        }

        public class SignInWithGoogleResponse
        {
            public Usuario Usuario { get; set; }
            public Boolean ExisteUsuario { get; set; }
            public string Mensaje { get; set; }
        }

        public class SignUpWithGoogleResponse
        {
            public Usuario Usuario { get; set; }
            public string Mensaje { get; set; }
        }
    }
}
