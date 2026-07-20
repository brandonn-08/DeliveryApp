using System.Linq;

namespace DeliveryApi.Models
{
    public static class PasswordValidator
    {
        // Regla: mínimo 2 caracteres especiales (no letras ni números)
        public static bool EsValida(string password, out string mensajeError)
        {
            mensajeError = string.Empty;

            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            {
                mensajeError = "La contraseña debe tener al menos 6 caracteres.";
                return false;
            }

            int especiales = password.Count(c => !char.IsLetterOrDigit(c));

            if (especiales < 2)
            {
                mensajeError = "La contraseña debe tener al menos 2 caracteres especiales (ej: @, ., #, -, _).";
                return false;
            }

            return true;
        }
    }
}