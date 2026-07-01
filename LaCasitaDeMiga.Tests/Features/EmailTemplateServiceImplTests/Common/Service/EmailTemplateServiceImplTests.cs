using Xunit;
using Microsoft.Extensions.Configuration;
using LaCasitaDeMiga.Features.Common.services.MailService;
using LaCasitaDeMiga.Features.Common.services.MailService.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LaCasitaDeMiga.Tests.Features.Common.Services.MailService {
    public class EmailTemplateServiceImplTests {
        // ⚙️ Función ayudante: Configura las variables del appsettings en memoria para Brevo
        private IConfiguration CreateMockConfiguration(string? apiKey) {
            var inMemorySettings = new Dictionary<string, string?>();

            if (apiKey != null) {
                inMemorySettings.Add("Brevo:ApiKey", apiKey);
            }

            return new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }

        // =========================================================================
        // 🧪 PRUEBAS: SendTemplateEmailAsync
        // =========================================================================

        [Fact]
        public async Task SendTemplateEmailAsync_WhenApiKeyIsMissing_ShouldThrowException() {
            // ARRANGE: Configuramos el appsettings totalmente vacío (sin ApiKey)
            var configWithoutKey = CreateMockConfiguration(apiKey: null);
            var service = new EmailTemplateServiceImpl(configWithoutKey);

            // ACT & ASSERT: Verificamos que salte la primera cláusula de guarda
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                service.SendTemplateEmailAsync("test@correo.com", EEmailTemplate.FORGOT_EMAIL, new { USER_NAME = "Roberto" })
            );

            Assert.Equal("La API Key de Brevo no está configurada correctamente en el nuevo servicio.", exception.Message);
        }

        [Fact]
        public async Task SendTemplateEmailAsync_WhenApiKeyIsInvalid_ShouldThrowExceptionWithBrevoError() {
            // ARRANGE: Ponemos una ApiKey falsa. El servicio intentará pegarle a Brevo pero el servidor real responderá con error (401 Unauthorized)
            var configWithBadKey = CreateMockConfiguration(apiKey: "CLAVE_FALSA_12345");
            var service = new EmailTemplateServiceImpl(configWithBadKey);

            // ACT & ASSERT: Evaluamos que capture el código de error HTTP fallido y dispare la excepción formateada
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                service.SendTemplateEmailAsync("test@correo.com", EEmailTemplate.FORGOT_EMAIL, new { USER_NAME = "Roberto" })
            );

            // Verificamos que el mensaje comience con el formato esperado del catch
            Assert.StartsWith("Error en la API de Brevo (Servicio Plantillas):", exception.Message);
        }

        // =========================================================================
        // 🧪 PRUEBAS: SendOrderConfirmationEmailAsync
        // =========================================================================

        [Fact]
        public async Task SendOrderConfirmationEmailAsync_Always_ShouldThrowNotImplementedException() {
            // ARRANGE
            var config = CreateMockConfiguration(apiKey: "Cualquiera");
            var service = new EmailTemplateServiceImpl(config);

            // ACT & ASSERT: Valida que el método lance estrictamente la excepción de "No Implementado"
            await Assert.ThrowsAsync<NotImplementedException>(() =>
                service.SendOrderConfirmationEmailAsync(null!)
            );
        }
    }
}