using System;
using System.IO;
using Xunit;
using PrimoAutoEletrica.Services;

namespace PrimoAutoEletrica.Tests;

public sealed class PathSecurityHelperTests
{
    [Fact]
    public void IsUnderRoot_AcceptsChildAndRejectsSiblingEscape()
    {
        var root = Path.Combine(Path.GetTempPath(), "primo-path-root-" + Guid.NewGuid().ToString("N"));
        var outside = Path.Combine(Path.GetTempPath(), "primo-path-out-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(outside);
        try
        {
            var inside = Path.Combine(root, "Backups", "ok.db");
            Directory.CreateDirectory(Path.GetDirectoryName(inside)!);
            File.WriteAllText(inside, "ok");
            File.WriteAllText(Path.Combine(outside, "CANARY_OUTSIDE.txt"), "secret");

            Assert.True(PathSecurityHelper.IsUnderRoot(inside, root));
            Assert.False(PathSecurityHelper.IsUnderRoot(Path.Combine(outside, "CANARY_OUTSIDE.txt"), root));

            var escaped = Path.Combine(root, "..", Path.GetFileName(outside), "CANARY_OUTSIDE.txt");
            Assert.Throws<UnauthorizedAccessException>(() =>
                PathSecurityHelper.RequireUnderAnyRoot(escaped, root));
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
            try { Directory.Delete(outside, true); } catch { }
        }
    }

    [Theory]
    [InlineData("..")]
    [InlineData("../")]
    [InlineData("..\\")]
    [InlineData("..\\..\\")]
    [InlineData("../../")]
    public void RequireUnderAnyRoot_RejectsDotDotVariants(string relativeEscape)
    {
        var root = Path.Combine(Path.GetTempPath(), "primo-path-dot-" + Guid.NewGuid().ToString("N"));
        var outside = Path.Combine(Path.GetTempPath(), "primo-path-dot-out-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(outside);
        File.WriteAllText(Path.Combine(outside, "CANARY_OUTSIDE.txt"), "x");
        try
        {
            var candidate = Path.Combine(root, relativeEscape, Path.GetFileName(outside), "CANARY_OUTSIDE.txt");
            Assert.Throws<UnauthorizedAccessException>(() =>
                PathSecurityHelper.RequireUnderAnyRoot(candidate, root));
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
            try { Directory.Delete(outside, true); } catch { }
        }
    }

    [Fact]
    public void OpenUri_RejectsNonHttpSchemes()
    {
        Assert.Throws<UnauthorizedAccessException>(() => SecureProcessLauncher.OpenUri("file:///C:/Windows/notepad.exe"));
        Assert.Throws<UnauthorizedAccessException>(() => SecureProcessLauncher.OpenUri("cmd.exe"));
    }

    [Fact]
    public void OpenWhatsAppLink_RejectsNonWaMe()
    {
        Assert.Throws<UnauthorizedAccessException>(() =>
            SecureProcessLauncher.OpenWhatsAppLink("https://evil.example/wa.me/5511999999999"));
    }
}
