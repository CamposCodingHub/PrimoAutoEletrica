using System;
using System.IO;
using System.Xml;
using PrimoAutoEletrica.Services;
using Xunit;

namespace PrimoAutoEletrica.Tests;

public sealed class SecureXmlLoaderTests : IDisposable
{
    private readonly string _tempRoot;

    public SecureXmlLoaderTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"primo-secure-xml-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempRoot);
    }

    [Fact]
    public void Load_DeveBloquearDtdEEntidadesExternas()
    {
        var path = Path.Combine(_tempRoot, "malicioso.xml");
        File.WriteAllText(path, """
            <!DOCTYPE foo [
              <!ENTITY xxe SYSTEM "file:///c:/windows/win.ini">
            ]>
            <foo>&xxe;</foo>
            """);

        Assert.Throws<XmlException>(() => SecureXmlLoader.Load(path));
    }

    [Fact]
    public void Load_DeveLerXmlValido()
    {
        var path = Path.Combine(_tempRoot, "valido.xml");
        File.WriteAllText(path, "<nfe><produto codigo=\"ABC\" /></nfe>");

        var document = SecureXmlLoader.Load(path);

        Assert.Equal("nfe", document.Root?.Name.LocalName);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
        catch
        {
            // Cleanup failure should not hide the assertion result.
        }
    }
}
