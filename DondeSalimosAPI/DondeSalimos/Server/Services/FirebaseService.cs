using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth;
using Google.Apis.Auth.OAuth2;
using NuGet.Common;

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
        public async Task<AuthTokenInfo> VerifyGoogleTokenAsync(string token)
        {
            // Intentar primero con Firebase
            try
            {
                Console.WriteLine("Intentando verificar como token de Firebase...");
                var decodedToken = await _firebaseAuth.VerifyIdTokenAsync(token);

                Console.WriteLine($"✓ Token de Firebase verificado exitosamente para UID: {decodedToken.Uid}");

                // Obtener información del proveedor
                string provider = "firebase";
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
            catch (FirebaseAuthException firebaseEx)
            {
                // Si falla Firebase, intentar con Google
                try
                {
                    var validationSettings = new GoogleJsonWebSignature.ValidationSettings
                    {
                        // Aceptar cualquier audience de Google
                        Audience = new[] {
                        _clientId,
                        "620861653759-n7la2q029vi8vjl2r53v2g18lo8s0rlh.apps.googleusercontent.com" // El client ID que vimos en el error
                        }
                    };

                    var payload = await GoogleJsonWebSignature.ValidateAsync(token, validationSettings);

                    // Buscar o crear usuario en Firebase basado en el token de Google
                    UserRecord userRecord;

                    try
                    {
                        // Intentar obtener el usuario por email
                        userRecord = await _firebaseAuth.GetUserByEmailAsync(payload.Email);
                    }
                    catch (FirebaseAuthException)
                    {
                        // El usuario no existe, crearlo
                        var userArgs = new UserRecordArgs
                        {
                            Email = payload.Email,
                            DisplayName = payload.Name,
                            PhotoUrl = payload.Picture,
                            EmailVerified = payload.EmailVerified
                        };

                        userRecord = await _firebaseAuth.CreateUserAsync(userArgs);
                    }

                    // Crear claims para el token personalizado
                    var claims = new Dictionary<string, object>
                    {
                        { "email", payload.Email },
                        { "name", payload.Name ?? "" },
                        { "picture", payload.Picture ?? "" },
                        { "email_verified", payload.EmailVerified }
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
                catch (Exception googleEx)
                {
                    throw new Exception("Token inválido: No se pudo verificar ni con Firebase ni con Google", googleEx);
                }
            }
        }
    }
}
