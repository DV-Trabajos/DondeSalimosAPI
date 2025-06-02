using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth;
using Google.Apis.Auth.OAuth2;

namespace DondeSalimos.Server.Services
{
    public class FirebaseService
    {
        private readonly FirebaseAuth _firebaseAuth;
        private readonly string _apiKey;
        private readonly string _clientId;
        private readonly string _projectId;

        public FirebaseService(IConfiguration configuration)
        {
            _apiKey = configuration["Firebase:ApiKey"];
            _clientId = configuration["Firebase:ClientId"];
            _projectId = configuration["Firebase:ProjectId"];

            // Inicializa Firebase Admin SDK si aún no está inicializado
            if (FirebaseApp.DefaultInstance == null)
            {
                FirebaseApp.Create(new AppOptions()
                {
                    ProjectId = _projectId,
                    Credential = GoogleCredential.FromJson(configuration["Firebase:Credentials"])
                });
            }

            _firebaseAuth = FirebaseAuth.DefaultInstance;
        }

        // Clase personalizada para devolver la información del token
        public class AuthTokenInfo
        {
            public string Uid { get; set; }
            public Dictionary<string, object> Claims { get; set; }
            public string CustomToken { get; set; }
            public DateTime ExpirationTime { get; set; }
            public string Provider { get; set; }
        }

        // Crear usuario en Firebase
        public async Task<UserRecord> CreateUserAsync(string email, string password, string displayName)
        {
            var userArgs = new UserRecordArgs()
            {
                Email = email,
                Password = password,
                DisplayName = displayName,
                EmailVerified = false,
                Disabled = false
            };

            return await _firebaseAuth.CreateUserAsync(userArgs);
        }

        // Actualizar usuario en Firebase
        public async Task<UserRecord> UpdateUserAsync(string uid, string email = null, string displayName = null, bool? disabled = null)
        {
            var userArgs = new UserRecordArgs()
            {
                Uid = uid
            };

            if (email != null)
                userArgs.Email = email;

            if (displayName != null)
                userArgs.DisplayName = displayName;

            if (disabled.HasValue)
                userArgs.Disabled = disabled.Value;

            return await _firebaseAuth.UpdateUserAsync(userArgs);
        }

        // Eliminar usuario en Firebase
        public async Task DeleteUserAsync(string uid)
        {
            await _firebaseAuth.DeleteUserAsync(uid);
        }

        // Obtener usuario por UID
        public async Task<UserRecord> GetUserAsync(string uid)
        {
            return await _firebaseAuth.GetUserAsync(uid);
        }

        // Obtener usuario por email
        public async Task<UserRecord> GetUserByEmailAsync(string email)
        {
            return await _firebaseAuth.GetUserByEmailAsync(email);
        }

        // Verificar si un usuario existe en Firebase
        public async Task<bool> UserExistsAsync(string uid)
        {
            try
            {
                await _firebaseAuth.GetUserAsync(uid);
                return true;
            }
            catch (FirebaseAuthException ex)
            {
                if (ex.AuthErrorCode == AuthErrorCode.UserNotFound)
                {
                    return false;
                }
                throw;
            }
        }

        // Verificar token de Google
        public async Task<AuthTokenInfo> VerifyGoogleTokenAsync(string googleIdToken)
        {
            try
            {
                // OPCIÓN 1: Verificar directamente con Firebase Admin SDK
                try
                {
                    // Verificar el token con Firebase Admin SDK
                    var decodedToken = await _firebaseAuth.VerifyIdTokenAsync(googleIdToken);

                    // Obtener el proveedor de autenticación
                    string provider = "unknown";
                    if (decodedToken.Claims.TryGetValue("firebase", out var firebaseValue) &&
                        firebaseValue is Dictionary<string, object> firebaseDict &&
                        firebaseDict.TryGetValue("sign_in_provider", out var providerValue))
                    {
                        provider = providerValue.ToString();
                    }

                    // Devolver la información del token
                    return new AuthTokenInfo
                    {
                        Uid = decodedToken.Uid,
                        Claims = decodedToken.Claims.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                        CustomToken = await _firebaseAuth.CreateCustomTokenAsync(decodedToken.Uid),
                        ExpirationTime = DateTimeOffset.FromUnixTimeSeconds(decodedToken.ExpirationTimeSeconds).UtcDateTime,
                        Provider = provider
                    };
                }
                catch (FirebaseAuthException ex)
                {
                    //throw new Exception("Error en la autenticación: Token de Google inválido", ex);

                    // Si falla la verificación con Firebase, intentar con Google API
                }

                // OPCIÓN 2: Verificar con Google API
                // Esta opción es útil si el token fue emitido directamente por Google (no por Firebase)
                var validationSettings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _clientId }
                };

                var payload = await GoogleJsonWebSignature.ValidateAsync(googleIdToken, validationSettings);

                // Buscar o crear usuario en Firebase basado en el token de Google
                UserRecord userRecord;

                try
                {
                    // Intentar obtener el usuario por email
                    userRecord = await _firebaseAuth.GetUserByEmailAsync(payload.Email);

                    // Crear claims para el token personalizado
                    var claims = new Dictionary<string, object>
                    {
                        { "email", payload.Email },
                        { "name", payload.Name },
                        { "picture", payload.Picture }
                    };

                    // Crear un token personalizado de Firebase para el usuario
                    string customToken = await _firebaseAuth.CreateCustomTokenAsync(userRecord.Uid, claims);

                    // Devolver la información del token
                    return new AuthTokenInfo
                    {
                        Uid = userRecord.Uid,
                        Claims = claims,
                        CustomToken = customToken,
                        ExpirationTime = DateTime.UtcNow.AddHours(1),
                        Provider = "google.com"
                    };
                }
                catch (FirebaseAuthException ex)
                {
                    throw new Exception("Error en la autenticación: Token de Google inválido", ex);
                }
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                {
                    Console.Error.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }

                throw new Exception("Error en la autenticación: Token de Google inválido", ex);
            }
        }

        // Crear token personalizado
        public async Task<string> CreateCustomTokenAsync(string uid, Dictionary<string, object> claims = null)
        {
            return await _firebaseAuth.CreateCustomTokenAsync(uid, claims);
        }
    }
}
