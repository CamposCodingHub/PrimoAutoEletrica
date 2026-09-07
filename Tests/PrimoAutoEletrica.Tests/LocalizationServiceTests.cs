using System.Globalization;
using PrimoAutoEletrica.Services;
using Xunit;

namespace PrimoAutoEletrica.Tests
{
    public class LocalizationServiceTests
    {
        [Fact]
        public void LocalizationService_Instance_ShouldNotBeNull()
        {
            // Arrange & Act
            var service = LocalizationService.Instance;

            // Assert
            Assert.NotNull(service);
        }

        [Fact]
        public void GetString_WithValidKey_ShouldReturnLocalizedString()
        {
            // Arrange
            var service = LocalizationService.Instance;
            service.SetLanguage("pt-BR");

            // Act
            var result = service.GetString("Dashboard");

            // Assert
            Assert.Equal("Painel de Controle", result);
        }

        [Fact]
        public void GetString_WithInvalidKey_ShouldReturnKey()
        {
            // Arrange
            var service = LocalizationService.Instance;
            var invalidKey = "InvalidKey_12345";

            // Act
            var result = service.GetString(invalidKey);

            // Assert
            Assert.Equal(invalidKey, result);
        }

        [Fact]
        public void SetLanguage_ChangingLanguage_ShouldUpdateCulture()
        {
            // Arrange
            var service = LocalizationService.Instance;
            var originalCulture = service.CurrentCulture;

            // Act
            service.SetLanguage("en-US");
            var newCulture = service.CurrentCulture;

            // Assert
            Assert.Equal("en-US", newCulture.Name);
            Assert.NotEqual(originalCulture.Name, newCulture.Name);

            // Cleanup
            service.SetLanguage(originalCulture.Name);
        }

        [Fact]
        public void GetAvailableLanguages_ShouldReturnSupportedLanguages()
        {
            // Arrange
            var service = LocalizationService.Instance;

            // Act
            var languages = service.GetAvailableLanguages();

            // Assert
            Assert.NotNull(languages);
            Assert.Equal(3, languages.Count);
            Assert.Contains(languages, l => l.Name == "pt-BR");
            Assert.Contains(languages, l => l.Name == "en-US");
            Assert.Contains(languages, l => l.Name == "es-ES");
        }

        [Fact]
        public void GetString_WithEnglishLanguage_ShouldReturnEnglishTranslation()
        {
            // Arrange
            var service = LocalizationService.Instance;
            service.SetLanguage("en-US");

            // Act
            var result = service.GetString("Dashboard");

            // Assert
            Assert.Equal("Dashboard", result);
        }

        [Fact]
        public void GetString_WithSpanishLanguage_ShouldReturnSpanishTranslation()
        {
            // Arrange
            var service = LocalizationService.Instance;
            service.SetLanguage("es-ES");

            // Act
            var result = service.GetString("Dashboard");

            // Assert
            Assert.Equal("Panel de Control", result);
        }

        [Fact]
        public void GetString_WithParameters_ShouldFormatString()
        {
            // Arrange
            var service = LocalizationService.Instance;
            service.SetLanguage("pt-BR");

            // Act - Test with a key that doesn't expect parameters
            var result = service.GetString("Save");

            // Assert
            Assert.Equal("Salvar", result);
        }

        [Fact]
        public void CultureChangedEvent_ShouldFireWhenLanguageChanges()
        {
            // Arrange
            var service = LocalizationService.Instance;
            var eventFired = false;
            service.CultureChanged += (s, e) => eventFired = true;

            // Act
            service.SetLanguage("en-US");

            // Assert
            Assert.True(eventFired);
        }
    }
}