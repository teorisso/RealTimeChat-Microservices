using AuthService.Configuration;
using AuthService.Data;
using AuthService.DTOs;
using AuthService.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Shared.DTOs;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace AuthService.Services
{
    public class AuthServiceImpl : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly JwtSettings _jwtSettings;

        public AuthServiceImpl(AppDbContext context, IOptions<JwtSettings> jwtSettings)
        {
            _context = context;
            _jwtSettings = jwtSettings.Value;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            // Validar fortaleza de contraseña
            var passwordValidation = ValidatePasswordStrength(request.Password);
            if (!passwordValidation.IsValid)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = passwordValidation.ErrorMessage
                };
            }

            // Verificar si el email ya existe
            var existingUser = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (existingUser != null)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "El email ya está registrado"
                };
            }

            // Hash de la contraseña con BCrypt
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Crear nuevo usuario
            var usuario = new Usuario
            {
                Nombre = request.Nombre,
                Email = request.Email,
                PasswordHash = passwordHash,
                FechaRegistro = DateTime.UtcNow,
                Activo = true
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            // Generar tokens
            var accessToken = GenerateAccessToken(usuario);
            var refreshToken = GenerateRefreshToken();

            // Guardar refresh token
            usuario.RefreshToken = refreshToken;
            usuario.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
            await _context.SaveChangesAsync();

            return new AuthResponse
            {
                Success = true,
                Message = "Usuario registrado exitosamente",
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
                UserInfo = new UsuarioDto
                {
                    Id = usuario.Id.ToString(),
                    Nombre = usuario.Nombre,
                    Email = usuario.Email
                }
            };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (usuario == null)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Credenciales inválidas"
                };
            }

            // Verificar si el usuario está activo
            if (!usuario.Activo)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "La cuenta está desactivada"
                };
            }

            // Verificar contraseña con BCrypt
            var passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, usuario.PasswordHash);
            if (!passwordValid)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Credenciales inválidas"
                };
            }

            // Generar tokens
            var accessToken = GenerateAccessToken(usuario);
            var refreshToken = GenerateRefreshToken();

            // Actualizar refresh token
            usuario.RefreshToken = refreshToken;
            usuario.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
            await _context.SaveChangesAsync();

            return new AuthResponse
            {
                Success = true,
                Message = "Login exitoso",
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
                UserInfo = new UsuarioDto
                {
                    Id = usuario.Id.ToString(),
                    Nombre = usuario.Nombre,
                    Email = usuario.Email
                }
            };
        }

        public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

            if (usuario == null || usuario.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Refresh token inválido o expirado"
                };
            }

            // Generar nuevos tokens
            var accessToken = GenerateAccessToken(usuario);
            var newRefreshToken = GenerateRefreshToken();

            usuario.RefreshToken = newRefreshToken;
            usuario.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
            await _context.SaveChangesAsync();

            return new AuthResponse
            {
                Success = true,
                Message = "Token renovado exitosamente",
                AccessToken = accessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
                UserInfo = new UsuarioDto
                {
                    Id = usuario.Id.ToString(),
                    Nombre = usuario.Nombre,
                    Email = usuario.Email
                }
            };
        }

        public async Task<bool> RevokeTokenAsync(int userId)
        {
            var usuario = await _context.Usuarios.FindAsync(userId);
            if (usuario == null)
            {
                return false;
            }

            usuario.RefreshToken = null;
            usuario.RefreshTokenExpiryTime = null;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<Usuario?> GetUserByIdAsync(int userId)
        {
            return await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Id == userId && u.Activo);
        }

        public async Task<bool> UpdateProfileAsync(int userId, UpdateProfileRequest request)
        {
            var usuario = await _context.Usuarios.FindAsync(userId);
            if (usuario == null)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(request.Nombre))
            {
                usuario.Nombre = request.Nombre;
            }

            if (request.AvatarUrl != null)
            {
                usuario.AvatarUrl = request.AvatarUrl;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Usuario>> GetAllUsersAsync()
        {
            return await _context.Usuarios
                .Where(u => u.Activo)
                .ToListAsync();
        }

        // MÉTODOS PRIVADOS PARA GENERACIÓN DE TOKENS

        private string GenerateAccessToken(Usuario usuario)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.Nombre),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private (bool IsValid, string ErrorMessage) ValidatePasswordStrength(string password)
        {
            if (password.Length < 8)
            {
                return (false, "La contraseña debe tener al menos 8 caracteres");
            }

            if (!Regex.IsMatch(password, @"[A-Z]"))
            {
                return (false, "La contraseña debe contener al menos una letra mayúscula");
            }

            if (!Regex.IsMatch(password, @"[a-z]"))
            {
                return (false, "La contraseña debe contener al menos una letra minúscula");
            }

            if (!Regex.IsMatch(password, @"[0-9]"))
            {
                return (false, "La contraseña debe contener al menos un número");
            }

            if (!Regex.IsMatch(password, @"[!@#$%^&*(),.?""':{}|<>]"))
            {
                return (false, "La contraseña debe contener al menos un carácter especial");
            }

            return (true, string.Empty);
        }
    }
}
