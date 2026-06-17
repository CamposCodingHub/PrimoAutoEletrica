using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace PrimoAutoEletrica.Services
{
    public static class SecureXmlLoader
    {
        private const long DefaultMaxFileBytes = 20L * 1024L * 1024L;

        public static XDocument Load(string filePath, LoadOptions loadOptions = LoadOptions.None, long maxFileBytes = DefaultMaxFileBytes)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("Informe o caminho do arquivo XML.", nameof(filePath));
            }

            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists)
            {
                throw new FileNotFoundException($"Arquivo nao encontrado: {filePath}", filePath);
            }

            if (fileInfo.Length > maxFileBytes)
            {
                throw new InvalidOperationException($"Arquivo XML excede o limite seguro de {maxFileBytes / 1024 / 1024} MB.");
            }

            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersFromEntities = 0,
                MaxCharactersInDocument = maxFileBytes * 4
            };

            using var stream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan);
            using var reader = XmlReader.Create(stream, settings);
            return XDocument.Load(reader, loadOptions);
        }
    }
}
