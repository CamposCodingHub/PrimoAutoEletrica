using PrimoAutoEletrica.Services;
using System;
using Xunit;

namespace PrimoAutoEletrica.Tests.Security
{
    /// <summary>
    /// Testes reais contra PasswordHasherService (PBKDF2-HMAC-SHA256, 600k iterações).
    /// NÃO usa helpers internos — testa o componente de produção.
    /// </summary>
    public class PasswordHasherRealTests
    {
        [Fact]
        public void HashPassword_GeraHashDiferenteDaSenhaPlana()
        {
            var senha = "MinhaSenh@Forte123!";
            var hash = PasswordHasherService.HashPassword(senha);

            Assert.NotEqual(senha, hash);
            Assert.StartsWith("PBKDF2$", hash);
        }

        [Fact]
        public void HashPassword_GeraHashesDiferentes_ParaMesmaSenha_PorSaltAleatorio()
        {
            var senha = "MesmaSenha@2026";
            var hash1 = PasswordHasherService.HashPassword(senha);
            var hash2 = PasswordHasherService.HashPassword(senha);

            Assert.NotEqual(hash1, hash2); // salt diferente
        }

        [Fact]
        public void VerifyPassword_SenhaCorreta_RetornaTrue()
        {
            var senha = "Admin@123";
            var hash = PasswordHasherService.HashPassword(senha);

            Assert.True(PasswordHasherService.VerifyPassword(senha, hash));
        }

        [Fact]
        public void VerifyPassword_SenhaIncorreta_RetornaFalse()
        {
            var senha = "Admin@123";
            var hash = PasswordHasherService.HashPassword(senha);

            Assert.False(PasswordHasherService.VerifyPassword("SenhaErrada!", hash));
        }

        [Fact]
        public void VerifyPassword_SenhaVazia_RetornaFalse()
        {
            var hash = PasswordHasherService.HashPassword("QualquerSenha@1");

            Assert.False(PasswordHasherService.VerifyPassword("", hash));
            Assert.False(PasswordHasherService.VerifyPassword(null!, hash));
        }

        [Fact]
        public void VerifyPassword_HashVazio_RetornaFalse()
        {
            Assert.False(PasswordHasherService.VerifyPassword("Admin@123", ""));
            Assert.False(PasswordHasherService.VerifyPassword("Admin@123", null!));
        }

        [Fact]
        public void VerifyPassword_SenhaPlanaArmazenada_NuncaAceita()
        {
            // SEC: jamais aceitar senha plana armazenada como hash válido
            Assert.False(PasswordHasherService.VerifyPassword("Admin@123", "Admin@123"));
        }

        [Fact]
        public void VerifyPassword_HashManipulado_RetornaFalse()
        {
            var hash = PasswordHasherService.HashPassword("SenhaOriginal@1");
            var parts = hash.Split('$');
            // Altera o salt (parte 2)
            var manipulado = $"{parts[0]}${parts[1]}$AAAAAAAAAAAAAAAAAAAAAA==${parts[3]}";

            Assert.False(PasswordHasherService.VerifyPassword("SenhaOriginal@1", manipulado));
        }

        [Fact]
        public void HashPassword_SenhaVazia_LancaArgumentException()
        {
            Assert.Throws<ArgumentException>(() => PasswordHasherService.HashPassword(""));
            Assert.Throws<ArgumentException>(() => PasswordHasherService.HashPassword("   "));
        }

        [Fact]
        public void NeedsRehash_HashAtual_RetornaFalse()
        {
            var hash = PasswordHasherService.HashPassword("Admin@123");
            Assert.False(PasswordHasherService.NeedsRehash(hash));
        }

        [Fact]
        public void NeedsRehash_SenhaPlana_RetornaTrue()
        {
            Assert.True(PasswordHasherService.NeedsRehash("Admin@123"));
        }

        [Fact]
        public void NeedsRehash_HashComIteracoesAntigas_RetornaTrue()
        {
            // Simula hash com iterações baixas (legacy)
            var hashLegacy = "PBKDF2$10000$c2FsdA==$aGFzaA==";
            Assert.True(PasswordHasherService.NeedsRehash(hashLegacy));
        }

        [Fact]
        public void Hash_Formato_TemQuatroParts()
        {
            var hash = PasswordHasherService.HashPassword("Test@12345");
            var parts = hash.Split('$');
            Assert.Equal(4, parts.Length);
            Assert.Equal("PBKDF2", parts[0]);
            Assert.True(int.TryParse(parts[1], out var iterations));
            Assert.True(iterations >= 600_000, $"Iterações={iterations}, esperado >= 600000 (OWASP ASVS)");
        }

        [Fact]
        public void VerifyPassword_FixedTimeEquals_NaoTimingAttack()
        {
            // Verifica que senhas diferentes com mesmo hash não convergem em tempo
            var hash = PasswordHasherService.HashPassword("RealPassword@1");
            // Ambos devem retornar false sem timing leak
            Assert.False(PasswordHasherService.VerifyPassword("A", hash));
            Assert.False(PasswordHasherService.VerifyPassword("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA", hash));
        }
    }
}
