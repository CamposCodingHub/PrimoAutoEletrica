using System;
using System.IO;
using Xunit;

namespace PrimoAutoEletrica.Tests.Services
{
    /// <summary>
    /// C1.1.5: residual Interacao:Controle:BaseConhecimentoControl (Owner em Window fechada).
    /// Source-level guards — PASS funcional real = UiSmoke App EXE.
    /// </summary>
    public class BaseConhecimentoOwnerHarnessRegressionTests
    {
        [Fact]
        public void BaseConhecimento_UsesWindowOwnerHelper_NotRawOwnerAssign()
        {
            var src = Locate("UserControls", "BaseConhecimentoControl.xaml.cs");
            var text = File.ReadAllText(src);

            Assert.Contains("using PrimoAutoEletrica.Helpers;", text, StringComparison.Ordinal);
            Assert.Contains("WindowOwnerHelper.ConfigureOwner", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Owner = Window.GetWindow(this)", text, StringComparison.Ordinal);
        }

        [Fact]
        public void Ferramentas_UsesWindowOwnerHelper_NotRawOwnerAssign()
        {
            var src = Locate("UserControls", "FerramentasControl.xaml.cs");
            var text = File.ReadAllText(src);

            Assert.Contains("using PrimoAutoEletrica.Helpers;", text, StringComparison.Ordinal);
            Assert.Contains("WindowOwnerHelper.ConfigureOwner", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Owner = Window.GetWindow(this)", text, StringComparison.Ordinal);
        }

        [Fact]
        public void Supervisor_DisposeDrainsDispatcher_AndSkipsSmokeHosts()
        {
            var src = Locate("Services", "UiSmokeTestService.Types.cs");
            var text = File.ReadAllText(src);

            Assert.Contains("DispatcherPriority.ContextIdle", text, StringComparison.Ordinal);
            Assert.Contains("IsSmokeHostWindow", text, StringComparison.Ordinal);

            var handleIdx = text.IndexOf("private void HandleManagedDialogs", StringComparison.Ordinal);
            Assert.True(handleIdx >= 0);
            var nativeIdx = text.IndexOf("private static void HandleNativeDialogs", StringComparison.Ordinal);
            Assert.True(nativeIdx > handleIdx);
            var handleBlock = text.Substring(handleIdx, nativeIdx - handleIdx);
            Assert.Contains("IsSmokeHostWindow(window)", handleBlock, StringComparison.Ordinal);
        }

        [Fact]
        public void ExerciseInteractionSurface_EnsuresHostOperationalBeforeClick()
        {
            var src = Locate("Services", "UiSmokeTestService.Helpers.cs");
            var text = File.ReadAllText(src);

            Assert.Contains("EnsureHostWindowOperational", text, StringComparison.Ordinal);

            var exerciseIdx = text.IndexOf("private void ExerciseInteractionSurface", StringComparison.Ordinal);
            Assert.True(exerciseIdx >= 0);
            var captureIdx = text.IndexOf("private List<ButtonDescriptor> CaptureButtonDescriptors", StringComparison.Ordinal);
            Assert.True(captureIdx > exerciseIdx);
            var block = text.Substring(exerciseIdx, captureIdx - exerciseIdx);
            Assert.Contains("EnsureHostWindowOperational(surface.HostWindow", block, StringComparison.Ordinal);
            Assert.Contains("RaiseButtonClick(targetButton)", block, StringComparison.Ordinal);

            var ensurePos = block.IndexOf("EnsureHostWindowOperational(surface.HostWindow", StringComparison.Ordinal);
            var raisePos = block.IndexOf("RaiseButtonClick(targetButton)", StringComparison.Ordinal);
            Assert.True(ensurePos >= 0 && raisePos > ensurePos);
        }

        private static string Locate(string folder, string fileName)
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "PrimoAutoEletrica", folder, fileName);
                if (File.Exists(candidate)) return candidate;
                candidate = Path.Combine(dir.FullName, folder, fileName);
                if (File.Exists(candidate)) return candidate;
                dir = dir.Parent;
            }
            throw new FileNotFoundException($"Nao encontrou {folder}/{fileName}");
        }
    }
}
