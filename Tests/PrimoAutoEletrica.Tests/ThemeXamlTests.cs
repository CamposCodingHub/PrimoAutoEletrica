using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace PrimoAutoEletrica.Tests
{
    public class ThemeXamlTests
    {
        private static readonly XNamespace XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";
        private static readonly Regex ResourceReferenceRegex = new(
            @"\{(?<Kind>StaticResource|DynamicResource)\s+(?<Key>(?!\{)[^,\}\s]+)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private readonly string _projectRoot;
        private readonly string _applicationRoot;
        private readonly string _themesPath;
        private readonly string _appXamlPath;

        public ThemeXamlTests()
        {
            _projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
            _applicationRoot = Path.Combine(_projectRoot, "PrimoAutoEletrica");
            _themesPath = Path.Combine(_applicationRoot, "Themes");
            _appXamlPath = Path.Combine(_applicationRoot, "App.xaml");
        }

        [Fact]
        public void ThemeDirectory_And_AppXaml_ShouldExist()
        {
            Assert.True(Directory.Exists(_themesPath), $"Theme directory not found: {_themesPath}");
            Assert.True(File.Exists(_appXamlPath), $"App.xaml not found: {_appXamlPath}");
        }

        [Fact]
        public void ThemeXamlFiles_ShouldBeValidXml()
        {
            foreach (var filePath in GetValidationFiles())
            {
                var document = LoadXaml(filePath);
                Assert.NotNull(document.Root);
            }
        }

        [Fact]
        public void MergedDictionarySources_ShouldResolveToExistingFiles()
        {
            var missingSources = new List<string>();

            foreach (var filePath in GetValidationFiles())
            {
                var document = LoadXaml(filePath);
                foreach (var source in GetMergedDictionarySources(document))
                {
                    var resolvedPath = ResolveDictionarySource(filePath, source);
                    if (!File.Exists(resolvedPath))
                    {
                        missingSources.Add($"{ToRelativePath(filePath)} -> {source}");
                    }
                }
            }

            Assert.True(
                missingSources.Count == 0,
                "Merged dictionaries missing on disk: " + string.Join("; ", missingSources));
        }

        [Fact]
        public void ThemeFiles_ShouldNotContainDuplicateExplicitKeys()
        {
            var duplicateKeys = new List<string>();

            foreach (var filePath in GetValidationFiles())
            {
                var explicitKeys = GetExplicitResourceKeys(LoadXaml(filePath));
                var duplicatesInFile = explicitKeys
                    .GroupBy(key => key, StringComparer.Ordinal)
                    .Where(group => group.Count() > 1)
                    .Select(group => group.Key)
                    .OrderBy(key => key, StringComparer.Ordinal)
                    .ToList();

                if (duplicatesInFile.Count > 0)
                {
                    duplicateKeys.Add($"{ToRelativePath(filePath)} -> {string.Join(", ", duplicatesInFile)}");
                }
            }

            Assert.True(
                duplicateKeys.Count == 0,
                "Duplicate x:Key values found: " + string.Join("; ", duplicateKeys));
        }

        [Fact]
        public void ColorDictionaries_ShouldExposeTheSameExplicitKeys()
        {
            var lightPath = Path.Combine(_themesPath, "Colors.Light.xaml");
            var darkPath = Path.Combine(_themesPath, "Colors.Dark.xaml");

            var lightKeys = GetExplicitResourceKeys(LoadXaml(lightPath)).ToHashSet(StringComparer.Ordinal);
            var darkKeys = GetExplicitResourceKeys(LoadXaml(darkPath)).ToHashSet(StringComparer.Ordinal);

            var missingInDark = lightKeys.Except(darkKeys, StringComparer.Ordinal).OrderBy(key => key, StringComparer.Ordinal).ToList();
            var missingInLight = darkKeys.Except(lightKeys, StringComparer.Ordinal).OrderBy(key => key, StringComparer.Ordinal).ToList();

            var differences = new List<string>();
            if (missingInDark.Count > 0)
            {
                differences.Add("Missing in Colors.Dark.xaml: " + string.Join(", ", missingInDark));
            }

            if (missingInLight.Count > 0)
            {
                differences.Add("Missing in Colors.Light.xaml: " + string.Join(", ", missingInLight));
            }

            Assert.True(
                differences.Count == 0,
                "Color dictionaries expose different keys. " + string.Join("; ", differences));
        }

        [Fact]
        public void ThemeResourceReferences_ShouldResolveWithinApplicationResources()
        {
            var documents = GetValidationFiles()
                .ToDictionary(path => path, LoadXaml, StringComparer.OrdinalIgnoreCase);

            var availableKeys = documents.Values
                .SelectMany(GetExplicitResourceKeys)
                .ToHashSet(StringComparer.Ordinal);

            var missingReferences = new List<string>();

            foreach (var entry in documents)
            {
                foreach (var reference in GetResourceReferences(entry.Value))
                {
                    if (!availableKeys.Contains(reference.Key))
                    {
                        missingReferences.Add($"{ToRelativePath(entry.Key)} -> {reference.Context} => {reference.Kind} {reference.Key}");
                    }
                }
            }

            Assert.True(
                missingReferences.Count == 0,
                "Missing resource references detected: " + string.Join("; ", missingReferences));
        }

        private IEnumerable<string> GetValidationFiles()
        {
            yield return _appXamlPath;

            foreach (var themeFile in Directory.GetFiles(_themesPath, "*.xaml").OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                yield return themeFile;
            }
        }

        private static XDocument LoadXaml(string filePath)
        {
            return XDocument.Load(filePath, LoadOptions.SetLineInfo);
        }

        private static IEnumerable<string> GetExplicitResourceKeys(XDocument document)
        {
            return document
                .Descendants()
                .Attributes(XamlNamespace + "Key")
                .Select(attribute => attribute.Value.Trim())
                .Where(value => !string.IsNullOrWhiteSpace(value));
        }

        private static IEnumerable<string> GetMergedDictionarySources(XDocument document)
        {
            return document
                .Descendants()
                .Where(element => element.Name.LocalName == "ResourceDictionary")
                .Attributes("Source")
                .Select(attribute => attribute.Value.Trim())
                .Where(value => !string.IsNullOrWhiteSpace(value));
        }

        private static IEnumerable<(string Kind, string Key, string Context)> GetResourceReferences(XDocument document)
        {
            foreach (var attribute in document.Descendants().Attributes())
            {
                foreach (Match match in ResourceReferenceRegex.Matches(attribute.Value))
                {
                    yield return (
                        match.Groups["Kind"].Value,
                        match.Groups["Key"].Value,
                        $"{attribute.Parent?.Name.LocalName}.{attribute.Name.LocalName}");
                }
            }

            foreach (var element in document.Descendants().Where(node => !node.HasElements && !string.IsNullOrWhiteSpace(node.Value)))
            {
                foreach (Match match in ResourceReferenceRegex.Matches(element.Value))
                {
                    yield return (
                        match.Groups["Kind"].Value,
                        match.Groups["Key"].Value,
                        $"{element.Name.LocalName}.Value");
                }
            }
        }

        private string ResolveDictionarySource(string ownerFilePath, string source)
        {
            var normalizedSource = source.Replace('/', Path.DirectorySeparatorChar);
            var ownerDirectory = Path.GetDirectoryName(ownerFilePath) ?? _applicationRoot;

            var localCandidate = Path.GetFullPath(Path.Combine(ownerDirectory, normalizedSource));
            if (File.Exists(localCandidate))
            {
                return localCandidate;
            }

            return Path.GetFullPath(Path.Combine(_applicationRoot, normalizedSource));
        }

        private string ToRelativePath(string path)
        {
            return Path.GetRelativePath(_projectRoot, path);
        }
    }
}
