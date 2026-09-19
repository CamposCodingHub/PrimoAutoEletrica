using System.Data.Common;
using PrimoAutoEletrica.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PrimoAutoEletrica.Services.Catalogo
{
    /// <summary>
    /// Fase B: frota canônica + vínculo N:N peça↔veículo (foco auto elétrica).
    /// </summary>
    public sealed class CatalogoVeiculoService
    {
        // Oficina auto eletrica tambem troca rolamento de alternador/partida (SKF/IKRO).
        // Exclui apenas freio puro (pastilha/disco), nao rolamentos.
        private static readonly HashSet<string> MarcasPecaMecanica = new(StringComparer.OrdinalIgnoreCase)
        {
        };

        private static readonly HashSet<string> CategoriasExcluidas = new(StringComparer.OrdinalIgnoreCase)
        {
            "Freios", "Pastilha", "Disco"
        };

        private static readonly string[] MarcasAutoEletricaPreferidas =
        {
            "DNI", "BOSCH", "NGK", "UETA", "HELLA", "VALEO", "MAGNETI", "MAHLE", "GAUSS", "SKF", "IKRO"
        };

        private readonly DatabaseService _databaseService;
        private readonly LoggerService _logger;
        private bool _ensureRan;

        public CatalogoVeiculoService(DatabaseService? databaseService = null, LoggerService? logger = null)
        {
            _databaseService = databaseService ?? global::PrimoAutoEletrica.App.Database;
            _logger = logger ?? global::PrimoAutoEletrica.App.Logger;
        }

        public void EnsureSeedAndLinks(bool forcarRelink = false)
        {
            if (_ensureRan && !forcarRelink)
            {
                return;
            }

            try
            {
                var inseridos = SeedFrotaSeVazia();
                var links = VincularPecasAutoEletrica(forcarRelink || inseridos > 0);
                _logger.LogInfo($"Catalogo veiculos: seed +{inseridos} veiculos, +{links} vinculos peca↔veiculo.");
                _ensureRan = true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Falha ao preparar catalogo de veiculos/aplicacao.", ex);
            }
        }

        public List<string> ObterMarcas()
        {
            EnsureSeedAndLinks();
            using var connection = _databaseService.GetConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT DISTINCT Marca
                FROM CatalogoVeiculos
                WHERE Ativo = 1
                ORDER BY Marca COLLATE NOCASE;";
            var list = new List<string>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                list.Add(reader.GetString(0));
            }

            return list;
        }

        public List<string> ObterModelos(string? marca)
        {
            EnsureSeedAndLinks();
            using var connection = _databaseService.GetConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            if (string.IsNullOrWhiteSpace(marca))
            {
                command.CommandText = @"
                    SELECT DISTINCT Modelo
                    FROM CatalogoVeiculos
                    WHERE Ativo = 1
                    ORDER BY Modelo COLLATE NOCASE;";
            }
            else
            {
                command.CommandText = @"
                    SELECT DISTINCT Modelo
                    FROM CatalogoVeiculos
                    WHERE Ativo = 1 AND upper(trim(Marca)) = upper(trim(@Marca))
                    ORDER BY Modelo COLLATE NOCASE;";
                command.Parameters.AddWithValue("@Marca", marca.Trim());
            }

            var list = new List<string>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                list.Add(reader.GetString(0));
            }

            return list;
        }

        public List<CatalogoVeiculo> BuscarVeiculos(string? marca, string? modelo, int? ano, string? motor)
        {
            EnsureSeedAndLinks();
            using var connection = _databaseService.GetConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, Marca, Modelo, AnoInicial, AnoFinal, Motor, Aliases, Segmento, Ativo, DataCriacao
                FROM CatalogoVeiculos
                WHERE Ativo = 1
                  AND (@Marca = '' OR upper(trim(Marca)) = upper(trim(@Marca)))
                  AND (@Modelo = '' OR upper(trim(Modelo)) = upper(trim(@Modelo))
                       OR instr(upper(Aliases), upper(@Modelo)) > 0)
                  AND (@Ano = 0 OR (AnoInicial <= @Ano AND AnoFinal >= @Ano))
                  AND (@Motor = '' OR instr(upper(Motor), upper(@Motor)) > 0
                       OR instr(upper(Aliases), upper(@Motor)) > 0)
                ORDER BY Marca, Modelo, AnoInicial, Motor;";
            command.Parameters.AddWithValue("@Marca", marca?.Trim() ?? string.Empty);
            command.Parameters.AddWithValue("@Modelo", modelo?.Trim() ?? string.Empty);
            command.Parameters.AddWithValue("@Ano", ano ?? 0);
            command.Parameters.AddWithValue("@Motor", motor?.Trim() ?? string.Empty);

            var list = new List<CatalogoVeiculo>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                list.Add(MaterializarVeiculo(reader));
            }

            return list;
        }

        public HashSet<Guid> ObterPecaIdsPorVeiculo(string? marcaVeiculo, string? modelo, int? ano, string? motor)
        {
            EnsureSeedAndLinks();
            var veiculos = BuscarVeiculos(marcaVeiculo, modelo, ano, motor);
            if (veiculos.Count == 0)
            {
                return new HashSet<Guid>();
            }

            var ids = veiculos.Select(v => v.Id).ToList();
            using var connection = _databaseService.GetConnection();
            connection.Open();
            var set = new HashSet<Guid>();
            foreach (var chunk in Chunk(ids, 80))
            {
                using var command = connection.CreateCommand();
                var parms = new List<string>();
                for (var i = 0; i < chunk.Count; i++)
                {
                    var name = "@V" + i;
                    parms.Add(name);
                    command.Parameters.AddWithValue(name, chunk[i].ToString());
                }

                command.CommandText = $@"
                    SELECT DISTINCT CatalogoPecaId
                    FROM CatalogoPecaVeiculos
                    WHERE CatalogoVeiculoId IN ({string.Join(",", parms)});";
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    if (Guid.TryParse(reader.GetString(0), out var pecaId))
                    {
                        set.Add(pecaId);
                    }
                }
            }

            return set;
        }

        public int ContarVinculos()
        {
            using var connection = _databaseService.GetConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM CatalogoPecaVeiculos;";
            return Convert.ToInt32(command.ExecuteScalar());
        }

        public int ContarVeiculos()
        {
            using var connection = _databaseService.GetConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM CatalogoVeiculos WHERE Ativo = 1;";
            return Convert.ToInt32(command.ExecuteScalar());
        }

        private int SeedFrotaSeVazia()
        {
            using var connection = _databaseService.GetConnection();
            connection.Open();

            // Insere apenas veiculos que ainda nao existem (permite expandir frota em updates).
            var existentes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var listCmd = connection.CreateCommand())
            {
                listCmd.CommandText = @"
                    SELECT Marca || '|' || Modelo || '|' || AnoInicial || '|' || AnoFinal || '|' || IFNULL(Motor,'')
                    FROM CatalogoVeiculos;";
                using var reader = listCmd.ExecuteReader();
                while (reader.Read())
                {
                    existentes.Add(reader.GetString(0));
                }
            }

            var inseridos = 0;
            using var tx = connection.BeginTransaction();
            foreach (var row in SeedRows)
            {
                var key = $"{row.Marca}|{row.Modelo}|{row.AnoInicial}|{row.AnoFinal}|{row.Motor}";
                if (existentes.Contains(key))
                {
                    continue;
                }

                var id = Guid.NewGuid();
                using var command = connection.CreateCommand();
                command.Transaction = tx;
                command.CommandText = @"
                    INSERT INTO CatalogoVeiculos
                    (Id, Marca, Modelo, AnoInicial, AnoFinal, Motor, Aliases, Segmento, Ativo, DataCriacao)
                    VALUES
                    (@Id, @Marca, @Modelo, @AnoInicial, @AnoFinal, @Motor, @Aliases, @Segmento, 1, @DataCriacao);";
                command.Parameters.AddWithValue("@Id", id.ToString());
                command.Parameters.AddWithValue("@Marca", row.Marca);
                command.Parameters.AddWithValue("@Modelo", row.Modelo);
                command.Parameters.AddWithValue("@AnoInicial", row.AnoInicial);
                command.Parameters.AddWithValue("@AnoFinal", row.AnoFinal);
                command.Parameters.AddWithValue("@Motor", row.Motor);
                command.Parameters.AddWithValue("@Aliases", row.Aliases);
                var segmentoPesado =
                    row.Marca.Contains("Mercedes", StringComparison.OrdinalIgnoreCase) ||
                    row.Marca.Equals("Volvo", StringComparison.OrdinalIgnoreCase) ||
                    row.Marca.Equals("Scania", StringComparison.OrdinalIgnoreCase) ||
                    row.Marca.Equals("Iveco", StringComparison.OrdinalIgnoreCase) ||
                    row.Marca.Equals("MAN", StringComparison.OrdinalIgnoreCase) ||
                    row.Marca.Equals("DAF", StringComparison.OrdinalIgnoreCase) ||
                    row.Marca.Equals("Hino", StringComparison.OrdinalIgnoreCase) ||
                    row.Marca.Equals("Isuzu", StringComparison.OrdinalIgnoreCase) ||
                    row.Marca.Equals("Foton", StringComparison.OrdinalIgnoreCase) ||
                    row.Marca.Equals("Shacman", StringComparison.OrdinalIgnoreCase) ||
                    row.Marca.Equals("Sinotruk", StringComparison.OrdinalIgnoreCase) ||
                    row.Marca.Equals("Agrale", StringComparison.OrdinalIgnoreCase) ||
                    row.Modelo.Contains("Constellation", StringComparison.OrdinalIgnoreCase) ||
                    row.Modelo.Contains("Delivery", StringComparison.OrdinalIgnoreCase) ||
                    row.Modelo.Contains("Worker", StringComparison.OrdinalIgnoreCase) ||
                    row.Modelo.Contains("Meteor", StringComparison.OrdinalIgnoreCase) ||
                    row.Modelo.Contains("Cargo", StringComparison.OrdinalIgnoreCase) ||
                    row.Modelo.Contains("Accelo", StringComparison.OrdinalIgnoreCase) ||
                    row.Modelo.Contains("Atego", StringComparison.OrdinalIgnoreCase) ||
                    row.Modelo.Contains("Actros", StringComparison.OrdinalIgnoreCase) ||
                    row.Modelo.Contains("Sprinter", StringComparison.OrdinalIgnoreCase) ||
                    row.Modelo.Contains("Daily", StringComparison.OrdinalIgnoreCase) ||
                    row.Modelo.Contains("Tector", StringComparison.OrdinalIgnoreCase) ||
                    row.Modelo.Contains("Ducato", StringComparison.OrdinalIgnoreCase) ||
                    row.Modelo.Contains("Master", StringComparison.OrdinalIgnoreCase) ||
                    row.Modelo.Contains("Boxer", StringComparison.OrdinalIgnoreCase);
                command.Parameters.AddWithValue("@Segmento", segmentoPesado ? "Pesado" : "Leve");
                command.Parameters.AddWithValue("@DataCriacao", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                command.ExecuteNonQuery();
                existentes.Add(key);
                inseridos++;
            }

            tx.Commit();
            return inseridos;
        }

        private int VincularPecasAutoEletrica(bool forcarRelink)
        {
            using var connection = _databaseService.GetConnection();
            connection.Open();

            if (!forcarRelink)
            {
                using var existing = connection.CreateCommand();
                existing.CommandText = "SELECT COUNT(*) FROM CatalogoPecaVeiculos;";
                if (Convert.ToInt32(existing.ExecuteScalar()) > 0)
                {
                    return 0;
                }
            }
            else
            {
                using var clear = connection.CreateCommand();
                clear.CommandText = "DELETE FROM CatalogoPecaVeiculos;";
                clear.ExecuteNonQuery();
            }

            var veiculos = new List<CatalogoVeiculo>();
            using (var vcmd = connection.CreateCommand())
            {
                vcmd.CommandText = @"
                    SELECT Id, Marca, Modelo, AnoInicial, AnoFinal, Motor, Aliases, Segmento, Ativo, DataCriacao
                    FROM CatalogoVeiculos WHERE Ativo = 1;";
                using var reader = vcmd.ExecuteReader();
                while (reader.Read())
                {
                    veiculos.Add(MaterializarVeiculo(reader));
                }
            }

            var pecas = new List<(Guid Id, string Marca, string Nome, string Descricao, string Aplicacao, string VeiculoAplicacao, string Categoria, string Obs)>();
            using (var pcmd = connection.CreateCommand())
            {
                pcmd.CommandText = @"
                    SELECT Id, Marca, Nome, Descricao, Aplicacao, VeiculoAplicacao, Categoria, ObservacoesTecnicas
                    FROM CatalogoPecas
                    WHERE Ativo = 1;";
                using var reader = pcmd.ExecuteReader();
                while (reader.Read())
                {
                    pecas.Add((
                        Guid.Parse(reader.GetString(0)),
                        reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                        reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                        reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                        reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                        reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                        reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                        reader.IsDBNull(7) ? string.Empty : reader.GetString(7)
                    ));
                }
            }

            var links = 0;
            using var tx = connection.BeginTransaction();
            foreach (var peca in pecas)
            {
                if (!EhPecaAutoEletrica(peca.Marca, peca.Categoria, peca.Nome))
                {
                    continue;
                }

                var texto = NormalizarTexto($"{peca.Nome} {peca.Descricao} {peca.Aplicacao} {peca.VeiculoAplicacao} {peca.Obs}");
                var alvos = ResolverVeiculosParaTexto(texto, veiculos);
                foreach (var veiculo in alvos)
                {
                    using var ins = connection.CreateCommand();
                    ins.Transaction = tx;
                    ins.CommandText = @"
                        INSERT OR IGNORE INTO CatalogoPecaVeiculos
                        (CatalogoPecaId, CatalogoVeiculoId, Fonte, DataVinculo)
                        VALUES (@PecaId, @VeiculoId, @Fonte, @Data);";
                    ins.Parameters.AddWithValue("@PecaId", peca.Id.ToString());
                    ins.Parameters.AddWithValue("@VeiculoId", veiculo.Id.ToString());
                    ins.Parameters.AddWithValue("@Fonte", "auto-eletrica-linker");
                    ins.Parameters.AddWithValue("@Data", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                    links += ins.ExecuteNonQuery();
                }
            }

            tx.Commit();
            return links;
        }

        private static bool EhPecaAutoEletrica(string marca, string categoria, string nome)
        {
            if (MarcasPecaMecanica.Contains(marca ?? string.Empty))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(categoria) && CategoriasExcluidas.Contains(categoria))
            {
                return false;
            }

            var n = (nome ?? string.Empty).ToLowerInvariant();
            if (n.Contains("pastilha") || n.Contains("disco de freio"))
            {
                return false;
            }

            if (MarcasAutoEletricaPreferidas.Any(m => string.Equals(m, marca, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            // Categorias tipicas de auto eletrica
            var cat = (categoria ?? string.Empty).ToLowerInvariant();
            if (cat.Contains("sensor") || cat.Contains("rele") || cat.Contains("igni") || cat.Contains("inje") ||
                cat.Contains("modulo") || cat.Contains("chave") || cat.Contains("ilumin") || cat.Contains("bateria") ||
                cat.Contains("altern") || cat.Contains("partida") || cat.Contains("eletr"))
            {
                return true;
            }

            return n.Contains("rele") || n.Contains("sensor") || n.Contains("vela") || n.Contains("bobina") ||
                   n.Contains("altern") || n.Contains("partida") || n.Contains("modulo") || n.Contains("chicote") ||
                   n.Contains("farol") || n.Contains("lampada") || n.Contains("buzina") || n.Contains("injetor") ||
                   n.Contains("rolamento") || n.Contains("bearing");
        }

        private static List<CatalogoVeiculo> ResolverVeiculosParaTexto(string textoNorm, List<CatalogoVeiculo> veiculos)
        {
            var matches = new List<CatalogoVeiculo>();
            var universal = textoNorm.Contains(" uso geral") || textoNorm.Contains("universal") ||
                            textoNorm.Contains("auxiliares universais") || textoNorm.Contains("aplicacao geral");

            foreach (var veiculo in veiculos)
            {
                var tokens = BuildTokens(veiculo);
                if (tokens.Any(t => t.Length >= 3 && textoNorm.Contains(t)))
                {
                    matches.Add(veiculo);
                }
            }

            if (matches.Count > 0)
            {
                return matches.DistinctBy(v => v.Id).ToList();
            }

            // Marca sem modelo: liga a todos da marca
            var marcasHit = veiculos
                .Select(v => v.Marca)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(m => textoNorm.Contains(NormalizarTexto(m)))
                .ToList();

            if (marcasHit.Count > 0)
            {
                return veiculos
                    .Where(v => marcasHit.Any(m => string.Equals(m, v.Marca, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            // Pecas universais: liga a frota inteira (oficinas precisam achar rele/sensor generico por carro)
            if (universal)
            {
                return veiculos;
            }

            return matches;
        }

        private static IEnumerable<string> BuildTokens(CatalogoVeiculo veiculo)
        {
            yield return NormalizarTexto(veiculo.Modelo);
            foreach (var alias in (veiculo.Aliases ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                yield return NormalizarTexto(alias);
            }
        }

        private static string NormalizarTexto(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var formD = value.Trim().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(formD.Length);
            foreach (var ch in formD)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(char.ToLowerInvariant(ch));
                }
            }

            var s = sb.ToString().Normalize(NormalizationForm.FormC);
            s = Regex.Replace(s, @"[^a-z0-9\s\-\.]", " ");
            s = Regex.Replace(s, @"\s+", " ").Trim();
            return " " + s + " ";
        }

        private static CatalogoVeiculo MaterializarVeiculo(DbDataReader reader)
        {
            return new CatalogoVeiculo
            {
                Id = Guid.Parse(reader.GetString(0)),
                Marca = reader.GetString(1),
                Modelo = reader.GetString(2),
                AnoInicial = reader.GetInt32(3),
                AnoFinal = reader.GetInt32(4),
                Motor = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                Aliases = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                Segmento = reader.IsDBNull(7) ? "Leve" : reader.GetString(7),
                Ativo = !reader.IsDBNull(8) && reader.GetInt32(8) == 1,
                DataCriacao = reader.IsDBNull(9)
                    ? DateTime.Now
                    : DateTime.TryParse(reader.GetString(9), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt) ? dt : DateTime.Now
            };
        }

        private static List<List<T>> Chunk<T>(List<T> source, int size)
        {
            var list = new List<List<T>>();
            for (var i = 0; i < source.Count; i += size)
            {
                list.Add(source.GetRange(i, Math.Min(size, source.Count - i)));
            }

            return list;
        }

        private sealed record SeedRow(string Marca, string Modelo, int AnoInicial, int AnoFinal, string Motor, string Aliases);

        private static readonly SeedRow[] SeedRows =
        {
            new("Hyundai", "HB20", 2012, 2019, "1.0", "HB20,HB 20"),
            new("Hyundai", "HB20", 2012, 2019, "1.6", "HB20"),
            new("Hyundai", "HB20", 2020, 2026, "1.0", "HB20,HB20S"),
            new("Hyundai", "HB20S", 2013, 2026, "1.6", "HB20S"),
            new("Hyundai", "Creta", 2017, 2026, "1.6", "Creta"),
            new("Hyundai", "Creta", 2022, 2026, "1.0 Turbo", "Creta"),
            new("Hyundai", "Tucson", 2010, 2026, "2.0", "Tucson"),
            new("Hyundai", "ix35", 2010, 2015, "2.0", "ix35"),
            new("Hyundai", "HR", 2013, 2026, "2.5", "HR"),
            new("Volkswagen", "Gol", 2009, 2023, "1.0", "Gol,G5,G6,G7,G8"),
            new("Volkswagen", "Gol", 2009, 2023, "1.6", "Gol"),
            new("Volkswagen", "Voyage", 2009, 2023, "1.6", "Voyage"),
            new("Volkswagen", "Polo", 2018, 2026, "1.0 TSI", "Polo"),
            new("Volkswagen", "Virtus", 2018, 2026, "1.6", "Virtus"),
            new("Volkswagen", "T-Cross", 2019, 2026, "1.0 TSI", "T-Cross,TCross"),
            new("Volkswagen", "Nivus", 2020, 2026, "1.0 TSI", "Nivus"),
            new("Volkswagen", "Saveiro", 2010, 2026, "1.6", "Saveiro"),
            new("Volkswagen", "Fox", 2010, 2021, "1.6", "Fox"),
            new("Volkswagen", "Up", 2014, 2021, "1.0", "Up,up!"),
            new("Volkswagen", "Jetta", 2011, 2018, "2.0", "Jetta"),
            new("Volkswagen", "Amarok", 2010, 2026, "2.0", "Amarok"),
            new("Chevrolet", "Onix", 2013, 2019, "1.0", "Onix"),
            new("Chevrolet", "Onix", 2013, 2019, "1.4", "Onix"),
            new("Chevrolet", "Onix", 2020, 2026, "1.0 Turbo", "Onix,Onix Plus"),
            new("Chevrolet", "Prisma", 2013, 2019, "1.4", "Prisma"),
            new("Chevrolet", "Tracker", 2020, 2026, "1.0 Turbo", "Tracker"),
            new("Chevrolet", "Spin", 2013, 2026, "1.8", "Spin"),
            new("Chevrolet", "S10", 2012, 2026, "2.8", "S10,S-10"),
            new("Chevrolet", "Montana", 2011, 2026, "1.2", "Montana"),
            new("Chevrolet", "Cruze", 2012, 2016, "1.8", "Cruze"),
            new("Chevrolet", "Cruze", 2017, 2022, "1.4 Turbo", "Cruze"),
            new("Chevrolet", "Cobalt", 2012, 2020, "1.8", "Cobalt"),
            new("Fiat", "Argo", 2017, 2026, "1.0", "Argo"),
            new("Fiat", "Argo", 2017, 2026, "1.3", "Argo"),
            new("Fiat", "Cronos", 2018, 2026, "1.3", "Cronos"),
            new("Fiat", "Mobi", 2016, 2026, "1.0", "Mobi"),
            new("Fiat", "Strada", 2014, 2026, "1.4", "Strada"),
            new("Fiat", "Toro", 2016, 2026, "1.8", "Toro"),
            new("Fiat", "Uno", 2010, 2021, "1.0", "Uno"),
            new("Fiat", "Palio", 2010, 2017, "1.0", "Palio"),
            new("Fiat", "Siena", 2010, 2016, "1.4", "Siena"),
            new("Fiat", "Pulse", 2021, 2026, "1.0 Turbo", "Pulse"),
            new("Fiat", "Fastback", 2022, 2026, "1.0 Turbo", "Fastback"),
            new("Fiat", "Fiorino", 2014, 2026, "1.4", "Fiorino"),
            new("Fiat", "Ducato", 2010, 2026, "2.3", "Ducato"),
            new("Toyota", "Corolla", 2015, 2019, "2.0", "Corolla"),
            new("Toyota", "Corolla", 2020, 2026, "1.8 Hybrid", "Corolla"),
            new("Toyota", "Hilux", 2016, 2026, "2.8", "Hilux"),
            new("Toyota", "Yaris", 2018, 2026, "1.5", "Yaris"),
            new("Toyota", "Etios", 2013, 2021, "1.5", "Etios"),
            new("Toyota", "SW4", 2016, 2026, "2.8", "SW4,SW-4,Fortuner"),
            new("Honda", "Civic", 2012, 2021, "2.0", "Civic"),
            new("Honda", "City", 2015, 2026, "1.5", "City"),
            new("Honda", "Fit", 2015, 2021, "1.5", "Fit"),
            new("Honda", "HR-V", 2016, 2026, "1.5", "HR-V,HRV"),
            new("Honda", "WR-V", 2017, 2023, "1.5", "WR-V,WRV"),
            new("Honda", "CR-V", 2012, 2026, "1.5", "CR-V,CRV"),
            new("Ford", "Ka", 2014, 2021, "1.0", "Ka,New Ka"),
            new("Ford", "Ka", 2014, 2021, "1.5", "Ka"),
            new("Ford", "EcoSport", 2013, 2021, "1.5", "EcoSport,Ecosport"),
            new("Ford", "Ranger", 2013, 2026, "2.2", "Ranger"),
            new("Ford", "Ranger", 2013, 2026, "3.2", "Ranger"),
            new("Ford", "Territory", 2021, 2026, "1.5", "Territory"),
            new("Ford", "Focus", 2014, 2019, "2.0", "Focus"),
            new("Renault", "Sandero", 2015, 2026, "1.0", "Sandero"),
            new("Renault", "Sandero", 2015, 2026, "1.6", "Sandero"),
            new("Renault", "Logan", 2014, 2026, "1.0", "Logan"),
            new("Renault", "Kwid", 2017, 2026, "1.0", "Kwid"),
            new("Renault", "Duster", 2015, 2026, "1.6", "Duster"),
            new("Renault", "Oroch", 2016, 2026, "1.6", "Oroch"),
            new("Renault", "Captur", 2017, 2022, "1.6", "Captur"),
            new("Renault", "Master", 2014, 2026, "2.3", "Master"),
            new("Jeep", "Renegade", 2015, 2026, "1.8", "Renegade"),
            new("Jeep", "Compass", 2017, 2026, "2.0", "Compass"),
            new("Jeep", "Commander", 2021, 2026, "1.3 Turbo", "Commander"),
            new("Nissan", "Kicks", 2016, 2026, "1.6", "Kicks"),
            new("Nissan", "Versa", 2012, 2026, "1.6", "Versa"),
            new("Nissan", "Frontier", 2014, 2026, "2.3", "Frontier"),
            new("Nissan", "March", 2012, 2020, "1.0", "March"),
            new("Nissan", "Sentra", 2014, 2022, "2.0", "Sentra"),
            new("Peugeot", "208", 2014, 2026, "1.6", "208"),
            new("Peugeot", "2008", 2015, 2026, "1.6", "2008"),
            new("Peugeot", "Boxer", 2012, 2026, "2.2", "Boxer"),
            new("Citroen", "C3", 2013, 2026, "1.2", "C3"),
            new("Citroen", "C4 Cactus", 2018, 2026, "1.6", "C4 Cactus,Cactus"),
            new("Citroen", "Jumper", 2012, 2026, "2.2", "Jumper"),
            new("Mitsubishi", "L200", 2013, 2026, "2.4", "L200,Triton"),
            new("Mitsubishi", "ASX", 2011, 2022, "2.0", "ASX"),
            new("Mitsubishi", "Pajero", 2010, 2021, "3.2", "Pajero"),
            new("Chery", "Tiggo 5X", 2019, 2026, "1.5", "Tiggo 5X,Tiggo5X"),
            new("Chery", "Arrizo 6", 2020, 2026, "1.5", "Arrizo 6"),
            new("Caoa Chery", "Tiggo 7", 2019, 2026, "1.5", "Tiggo 7"),
            new("Caoa Chery", "Tiggo 8", 2021, 2026, "1.6", "Tiggo 8"),
            new("BYD", "Dolphin", 2023, 2026, "Eletrico", "Dolphin"),
            new("BYD", "Yuan Plus", 2023, 2026, "Eletrico", "Yuan Plus,Yuan"),
            new("BYD", "Song Plus", 2023, 2026, "Hybrid", "Song Plus"),
            new("Kia", "Sportage", 2011, 2026, "2.0", "Sportage"),
            new("Kia", "Cerato", 2010, 2021, "1.6", "Cerato"),
            new("Kia", "Bongo", 2012, 2026, "2.5", "Bongo"),
            new("Ram", "Rampage", 2023, 2026, "2.0", "Rampage"),
            new("Ram", "2500", 2012, 2026, "6.7", "2500,Ram 2500"),
            new("Volkswagen", "Delivery", 2010, 2026, "4.5", "Delivery,VW Delivery"),
            new("Volkswagen", "Delivery Express", 2018, 2026, "2.3", "Delivery Express"),
            new("Volkswagen", "Worker", 2008, 2020, "Worker", "Worker"),
            new("Volkswagen", "Constellation", 2010, 2026, "17.280", "Constellation,VW Constellation"),
            new("Volkswagen", "Constellation", 2010, 2026, "24.280", "Constellation"),
            new("Volkswagen", "Meteor", 2020, 2026, "29.520", "Meteor"),
            new("Mercedes-Benz", "Accelo", 2012, 2026, "815", "Accelo,MB Accelo"),
            new("Mercedes-Benz", "Atego", 2010, 2026, "1719", "Atego,MB Atego"),
            new("Mercedes-Benz", "Atego", 2010, 2026, "2426", "Atego"),
            new("Mercedes-Benz", "Actros", 2012, 2026, "2651", "Actros,MB Actros"),
            new("Mercedes-Benz", "Axor", 2010, 2022, "2544", "Axor"),
            new("Mercedes-Benz", "Sprinter", 2012, 2026, "415", "Sprinter"),
            new("Mercedes-Benz", "Sprinter", 2012, 2026, "515", "Sprinter"),
            new("Volvo", "VM", 2012, 2026, "270", "VM,Volvo VM"),
            new("Volvo", "FH", 2013, 2026, "460", "FH,Volvo FH"),
            new("Volvo", "FH", 2013, 2026, "540", "FH"),
            new("Volvo", "FMX", 2015, 2026, "500", "FMX"),
            new("Scania", "P-Series", 2012, 2026, "P310", "Scania P,P-Series"),
            new("Scania", "G-Series", 2012, 2026, "G410", "Scania G,G-Series"),
            new("Scania", "R-Series", 2012, 2026, "R450", "Scania R,R-Series"),
            new("Scania", "S-Series", 2018, 2026, "S500", "Scania S,S-Series"),
            new("Iveco", "Daily", 2012, 2026, "35S14", "Daily,Iveco Daily"),
            new("Iveco", "Daily", 2012, 2026, "70C17", "Daily"),
            new("Iveco", "Tector", 2012, 2026, "9-190", "Tector"),
            new("Iveco", "Tector", 2012, 2026, "17-280", "Tector"),
            new("Iveco", "Hi-Way", 2013, 2026, "480", "Hi-Way,HiWay,Hi Way"),
            new("Iveco", "Hi-Road", 2014, 2026, "440", "Hi-Road,HiRoad"),
            new("Ford", "Cargo", 2010, 2026, "816", "Cargo,Ford Cargo"),
            new("Ford", "Cargo", 2010, 2026, "2429", "Cargo"),
            new("Ford", "Cargo", 2015, 2026, "2842", "Cargo"),
            new("MAN", "TGX", 2014, 2026, "29.480", "TGX,MAN TGX"),
            new("MAN", "TGS", 2014, 2026, "TGS", "TGS"),
            new("DAF", "XF", 2015, 2026, "XF105", "DAF XF,XF"),
            new("DAF", "CF", 2015, 2026, "CF85", "DAF CF,CF"),
            new("Hino", "500", 2012, 2026, "GH", "Hino 500"),
            new("Isuzu", "NPR", 2012, 2026, "NPR", "NPR,Isuzu NPR"),
            new("Isuzu", "FTR", 2012, 2026, "FTR", "FTR"),
            new("Foton", "Aumark", 2015, 2026, "3.5", "Aumark"),
            new("Foton", "Auman", 2016, 2026, "Auman", "Auman"),
            new("JAC", "V260", 2018, 2026, "2.0", "V260"),
            new("JAC", "N35", 2019, 2026, "N35", "N35"),
            new("Shacman", "X3000", 2018, 2026, "X3000", "Shacman,X3000"),
            new("Sinotruk", "Howo", 2016, 2026, "Howo", "Howo,Sinotruk"),
            new("Mercedes-Benz", "OF-1721", 2012, 2026, "OF", "OF-1721,OF 1721,chassis MB"),
            new("Mercedes-Benz", "O-500", 2012, 2026, "O500", "O-500,O500"),
            new("Volkswagen", "17.230 OD", 2014, 2026, "17.230", "17.230 OD,VW Onibus"),
            new("Volvo", "B270F", 2014, 2026, "B270", "B270F"),
            new("Agrale", "MA 8.0", 2015, 2026, "MA", "Agrale MA,MA 8.0"),
            new("Marcopolo", "Torino", 2012, 2026, "Torino", "Torino"),
            new("Marcopolo", "Ideale", 2014, 2026, "Ideale", "Ideale"),
            new("Caio", "Apache", 2012, 2026, "Apache", "Apache Vip"),
            new("Toyota", "Land Cruiser", 2010, 2026, "4.5", "Land Cruiser,LandCruiser"),
            new("Toyota", "Hiace", 2012, 2026, "2.8", "Hiace"),
            new("Ford", "Transit", 2014, 2026, "2.2", "Transit"),
            new("Ford", "F-350", 2012, 2026, "6.2", "F-350,F350"),
            new("Ford", "F-4000", 2010, 2026, "F4000", "F-4000,F4000"),
            new("Chevrolet", "Silverado", 2014, 2026, "5.3", "Silverado"),
            new("Chevrolet", "D-Max", 2015, 2026, "2.5", "D-Max,DMax"),
            new("Nissan", "Navara", 2015, 2026, "2.3", "Navara"),
            new("Mitsubishi", "Canter", 2012, 2026, "Canter", "Canter"),
            new("Hyundai", "HD78", 2012, 2026, "HD78", "HD78,HD 78"),
            new("Hyundai", "HD80", 2014, 2026, "HD80", "HD80"),
            new("MAZDA", "BT-50", 2012, 2022, "3.2", "BT-50,BT50"),
            new("Suzuki", "Jimny", 2012, 2026, "1.5", "Jimny"),
            new("BMW", "X1", 2012, 2026, "2.0", "X1"),
            new("BMW", "320i", 2012, 2026, "2.0", "320i"),
            new("Audi", "A3", 2013, 2026, "1.4", "A3"),
            new("Audi", "Q3", 2013, 2026, "1.4", "Q3"),
            new("Mercedes-Benz", "Classe A", 2013, 2026, "1.6", "Classe A,A200"),
            new("Mercedes-Benz", "Classe C", 2012, 2026, "1.6", "Classe C,C180")
        };
    }
}
