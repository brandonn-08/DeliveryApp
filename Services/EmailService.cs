using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace DeliveryApi.Services
{
    // 🚨 Usamos una Interfaz para aplicar Programación Orientada a Objetos
    public interface IEmailService
    {
        Task SendVerificationEmailAsync(string toEmail, string code);
        Task SendUsernameRecoveryEmailAsync(string toEmail, string name, string dni);
    }

    public class EmailService : IEmailService
    {
        // Pon aquí tu correo de Gmail real
        private readonly string _emailFrom = "brandoncacuango56@gmail.com";

        // Pega aquí las 16 letras que te dio Google (sin espacios)
        private readonly string _appPassword = "oabjgeerltbqxeny";

        public async Task SendVerificationEmailAsync(string toEmail, string code)
        {
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(_emailFrom));
            email.To.Add(MailboxAddress.Parse(toEmail));

            // Asunto del correo
            email.Subject = "🚀 Código de Verificación - FastDelivery";

            // Cuerpo del correo usando HTML para que se vea súper profesional
            var builder = new BodyBuilder
            {
                HtmlBody = $@"
                    <div style='font-family: Arial, sans-serif; padding: 20px; text-align: center; max-width: 500px; margin: auto; border: 1px solid #e5e7eb; border-radius: 15px;'>
                        <h2 style='color: #dc2626; margin-bottom: 5px;'>FastDelivery Tabacundo</h2>
                        <p style='color: #4b5563;'>Tu código de seguridad para verificar tu cuenta es:</p>
                        <h1 style='background-color: #f3f4f6; padding: 15px; letter-spacing: 8px; color: #1f2937; border-radius: 10px; font-size: 32px;'>{code}</h1>
                        <p style='color: #9ca3af; font-size: 12px;'>Este código es confidencial y expirará en 5 minutos.</p>
                    </div>"
            };
            email.Body = builder.ToMessageBody();

            // Lógica de conexión SMTP con MailKit
            using var smtp = new SmtpClient();
            try
            {
                // Conectamos a los servidores de Google de forma segura
                await smtp.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(_emailFrom, _appPassword);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error enviando correo: {ex.Message}");
                throw;
            }
        }

        public async Task SendUsernameRecoveryEmailAsync(string toEmail, string name, string dni)
        {
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(_emailFrom));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = "🔑 Recuperación de Usuario - FastDelivery";

            var builder = new BodyBuilder
            {
                HtmlBody = $@"
                    <div style='font-family: Arial, sans-serif; padding: 20px; text-align: center; max-width: 500px; margin: auto; border: 1px solid #e5e7eb; border-radius: 15px;'>
                        <h2 style='color: #dc2626; margin-bottom: 5px;'>FastDelivery Tabacundo</h2>
                        <p style='color: #4b5563;'>Hola {name}, tu identificador de usuario (DNI) es:</p>
                        <h1 style='background-color: #f3f4f6; padding: 15px; letter-spacing: 4px; color: #1f2937; border-radius: 10px; font-size: 26px;'>{dni}</h1>
                        <p style='color: #9ca3af; font-size: 12px;'>Usa este identificador junto a tu contraseña para iniciar sesión.</p>
                    </div>"
            };
            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            try
            {
                await smtp.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(_emailFrom, _appPassword);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error enviando correo: {ex.Message}");
                throw;
            }
        }
    }
}