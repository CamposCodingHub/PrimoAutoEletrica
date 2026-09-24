# -*- coding: utf-8 -*-
"""Generate Phase B vehicle catalog structure for PrimoAutoEletrica."""
from pathlib import Path
import textwrap

ROOT = Path(r"C:\Projetos\PrimoAutoEletrica\PrimoAutoEletrica")

# Popular BR light vehicles for auto elétrica workshops (marca, modelo, ano_ini, ano_fim, motor, aliases)
VEICULOS = [
    ("Hyundai", "HB20", 2012, 2019, "1.0", "HB20,HB 20"),
    ("Hyundai", "HB20", 2012, 2019, "1.6", "HB20,HB 20"),
    ("Hyundai", "HB20", 2020, 2026, "1.0", "HB20,HB20S,HB 20"),
    ("Hyundai", "HB20S", 2013, 2026, "1.6", "HB20S"),
    ("Hyundai", "Creta", 2017, 2026, "1.6", "Creta"),
    ("Hyundai", "Creta", 2022, 2026, "1.0 Turbo", "Creta"),
    ("Hyundai", "i30", 2009, 2017, "1.8", "i30"),
    ("Volkswagen", "Gol", 2009, 2023, "1.0", "Gol,G5,G6,G7,G8"),
    ("Volkswagen", "Gol", 2009, 2023, "1.6", "Gol"),
    ("Volkswagen", "Voyage", 2009, 2023, "1.6", "Voyage"),
    ("Volkswagen", "Polo", 2018, 2026, "1.0 TSI", "Polo"),
    ("Volkswagen", "Virtus", 2018, 2026, "1.6", "Virtus"),
    ("Volkswagen", "T-Cross", 2019, 2026, "1.0 TSI", "T-Cross,TCross,T Cross"),
    ("Volkswagen", "Saveiro", 2010, 2026, "1.6", "Saveiro"),
    ("Volkswagen", "Fox", 2010, 2021, "1.6", "Fox"),
    ("Volkswagen", "Up", 2014, 2021, "1.0", "Up,up!"),
    ("Chevrolet", "Onix", 2013, 2019, "1.0", "Onix"),
    ("Chevrolet", "Onix", 2013, 2019, "1.4", "Onix"),
    ("Chevrolet", "Onix", 2020, 2026, "1.0 Turbo", "Onix,Onix Plus"),
    ("Chevrolet", "Prisma", 2013, 2019, "1.4", "Prisma"),
    ("Chevrolet", "Tracker", 2020, 2026, "1.0 Turbo", "Tracker"),
    ("Chevrolet", "Spin", 2013, 2026, "1.8", "Spin"),
    ("Chevrolet", "S10", 2012, 2026, "2.8", "S10,S-10"),
    ("Chevrolet", "Montana", 2011, 2026, "1.2", "Montana"),
    ("Fiat", "Argo", 2017, 2026, "1.0", "Argo"),
    ("Fiat", "Argo", 2017, 2026, "1.3", "Argo"),
    ("Fiat", "Cronos", 2018, 2026, "1.3", "Cronos"),
    ("Fiat", "Mobi", 2016, 2026, "1.0", "Mobi"),
    ("Fiat", "Strada", 2014, 2026, "1.4", "Strada"),
    ("Fiat", "Toro", 2016, 2026, "1.8", "Toro"),
    ("Fiat", "Uno", 2010, 2021, "1.0", "Uno"),
    ("Fiat", "Palio", 2010, 2017, "1.0", "Palio"),
    ("Fiat", "Siena", 2010, 2016, "1.4", "Siena"),
    ("Fiat", "Pulse", 2021, 2026, "1.0 Turbo", "Pulse"),
    ("Fiat", "Fastback", 2022, 2026, "1.0 Turbo", "Fastback"),
    ("Toyota", "Corolla", 2015, 2019, "2.0", "Corolla"),
    ("Toyota", "Corolla", 2020, 2026, "1.8 Hybrid", "Corolla"),
    ("Toyota", "Hilux", 2016, 2026, "2.8", "Hilux"),
    ("Toyota", "Yaris", 2018, 2026, "1.5", "Yaris"),
    ("Toyota", "Etios", 2013, 2021, "1.5", "Etios"),
    ("Honda", "Civic", 2012, 2021, "2.0", "Civic"),
    ("Honda", "City", 2015, 2026, "1.5", "City"),
    ("Honda", "Fit", 2015, 2021, "1.5", "Fit"),
    ("Honda", "HR-V", 2016, 2026, "1.5", "HR-V,HRV,HR V"),
    ("Honda", "WR-V", 2017, 2023, "1.5", "WR-V,WRV"),
    ("Ford", "Ka", 2014, 2021, "1.0", "Ka,New Ka"),
    ("Ford", "Ka", 2014, 2021, "1.5", "Ka"),
    ("Ford", "EcoSport", 2013, 2021, "1.5", "EcoSport,Ecosport"),
    ("Ford", "Ranger", 2013, 2026, "2.2", "Ranger"),
    ("Ford", "Territory", 2021, 2026, "1.5", "Territory"),
    ("Renault", "Sandero", 2015, 2026, "1.0", "Sandero"),
    ("Renault", "Sandero", 2015, 2026, "1.6", "Sandero"),
    ("Renault", "Logan", 2014, 2026, "1.0", "Logan"),
    ("Renault", "Kwid", 2017, 2026, "1.0", "Kwid"),
    ("Renault", "Duster", 2015, 2026, "1.6", "Duster"),
    ("Renault", "Oroch", 2016, 2026, "1.6", "Oroch"),
    ("Jeep", "Renegade", 2015, 2026, "1.8", "Renegade"),
    ("Jeep", "Compass", 2017, 2026, "2.0", "Compass"),
    ("Jeep", "Commander", 2021, 2026, "1.3 Turbo", "Commander"),
    ("Nissan", "Kicks", 2016, 2026, "1.6", "Kicks"),
    ("Nissan", "Versa", 2012, 2026, "1.6", "Versa"),
    ("Nissan", "Frontier", 2014, 2026, "2.3", "Frontier"),
    ("Peugeot", "208", 2014, 2026, "1.6", "208"),
    ("Peugeot", "2008", 2015, 2026, "1.6", "2008"),
    ("Citroen", "C3", 2013, 2026, "1.2", "C3"),
    ("Citroen", "C4 Cactus", 2018, 2026, "1.6", "C4 Cactus,Cactus"),
    ("Mitsubishi", "L200", 2013, 2026, "2.4", "L200,Triton"),
    ("Mitsubishi", "ASX", 2011, 2022, "2.0", "ASX"),
    ("Chery", "Tiggo 5X", 2019, 2026, "1.5", "Tiggo 5X,Tiggo5X"),
    ("Chery", "Arrizo 6", 2020, 2026, "1.5", "Arrizo 6,Arrizo6"),
    ("Caoa Chery", "Tiggo 7", 2019, 2026, "1.5", "Tiggo 7,Tiggo7"),
    ("BYD", "Dolphin", 2023, 2026, "Eletrico", "Dolphin"),
    ("BYD", "Yuan Plus", 2023, 2026, "Eletrico", "Yuan Plus,Yuan"),
    ("VW", "Jetta", 2011, 2018, "2.0", "Jetta"),  # alias marca will normalize
    ("Chevrolet", "Cruze", 2012, 2016, "1.8", "Cruze"),
    ("Chevrolet", "Cruze", 2017, 2022, "1.4 Turbo", "Cruze"),
    ("Fiat", "Grand Siena", 2012, 2021, "1.4", "Grand Siena,GrandSiena"),
    ("Fiat", "Idea", 2011, 2016, "1.4", "Idea"),
    ("Volkswagen", "Amarok", 2010, 2026, "2.0", "Amarok"),
    ("Toyota", "SW4", 2016, 2026, "2.8", "SW4,SW-4,Fortuner"),
    ("Honda", "CR-V", 2012, 2026, "1.5", "CR-V,CRV"),
    ("Ford", "Focus", 2014, 2019, "2.0", "Focus"),
    ("Renault", "Captur", 2017, 2022, "1.6", "Captur"),
    ("Nissan", "March", 2012, 2020, "1.0", "March"),
    ("Nissan", "Sentra", 2014, 2022, "2.0", "Sentra"),
]

# Normalize VW duplicate - change VW Jetta to Volkswagen
VEICULOS = [(("Volkswagen" if m == "VW" else m), mo, a, b, mot, al) for m, mo, a, b, mot, al in VEICULOS]

# Deduplicate
seen = set()
uniq = []
for v in VEICULOS:
    key = (v[0].lower(), v[1].lower(), v[2], v[3], v[4].lower())
    if key not in seen:
        seen.add(key)
        uniq.append(v)
VEICULOS = uniq

# --- Model CatalogoVeiculo.cs ---
(ROOT / "Models" / "CatalogoVeiculo.cs").write_text(textwrap.dedent('''\
using System;

namespace PrimoAutoEletrica.Models
{
    public sealed class CatalogoVeiculo
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Marca { get; set; } = string.Empty;
        public string Modelo { get; set; } = string.Empty;
        public int AnoInicial { get; set; }
        public int AnoFinal { get; set; }
        public string Motor { get; set; } = string.Empty;
        public string Aliases { get; set; } = string.Empty;
        public string Segmento { get; set; } = "Leve";
        public bool Ativo { get; set; } = true;
        public DateTime DataCriacao { get; set; } = DateTime.Now;

        public string Exibicao => string.IsNullOrWhiteSpace(Motor)
            ? $"{Marca} {Modelo} ({AnoInicial}-{AnoFinal})"
            : $"{Marca} {Modelo} {Motor} ({AnoInicial}-{AnoFinal})";
    }
}
'''), encoding='utf-8')

# --- Extend CatalogoPecaFiltro ---
(ROOT / "Models" / "CatalogoPecaFiltro.cs").write_text(textwrap.dedent('''\
using System;

namespace PrimoAutoEletrica.Models
{
    public class CatalogoPecaFiltro
    {
        public string Termo { get; set; } = string.Empty;
        public string Marca { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public string StatusRevisao { get; set; } = string.Empty;
        public bool SomentePendentes { get; set; }
        public string Aplicacao { get; set; } = string.Empty;
        public string Equivalentes { get; set; } = string.Empty;
        public string ModeloVeiculo { get; set; } = string.Empty;
        public string Ano { get; set; } = string.Empty;
        public string Motor { get; set; } = string.Empty;
        public string MarcaVeiculo { get; set; } = string.Empty;
        public Guid? CatalogoVeiculoId { get; set; }
        public bool SomenteAutoEletrica { get; set; } = true;
        public bool IncluirInativos { get; set; }
    }
}
'''), encoding='utf-8')

print(f"Wrote models, vehicles={len(VEICULOS)}")

# Write seed vehicle literals for C#
rows = []
for marca, modelo, ai, af, motor, aliases in VEICULOS:
    rows.append(
        f'            new("{marca}", "{modelo}", {ai}, {af}, "{motor}", "{aliases}"),'
    )
seed_array = "\n".join(rows)

service = r'''using Microsoft.Data.Sqlite;
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
        private static readonly HashSet<string> MarcasPecaMecanica = new(StringComparer.OrdinalIgnoreCase)
        {
            "SKF", "IKRO"
        };

        private static readonly HashSet<string> CategoriasExcluidas = new(StringComparer.OrdinalIgnoreCase)
        {
            "Rolamentos", "Freios", "Pastilha", "Disco"
        };

        private static readonly string[] MarcasAutoEletricaPreferidas =
        {
            "DNI", "BOSCH", "NGK", "UETA", "HELLA", "VALEO", "MAGNETI", "MAHLE", "GAUSS"
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
                var links = VincularPecasAutoEletrica(forcarRelink);
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
            using (var countCmd = connection.CreateCommand())
            {
                countCmd.CommandText = "SELECT COUNT(*) FROM CatalogoVeiculos;";
                var count = Convert.ToInt32(countCmd.ExecuteScalar());
                if (count > 0)
                {
                    return 0;
                }
            }

            var inseridos = 0;
            using var tx = connection.BeginTransaction();
            foreach (var row in SeedRows)
            {
                var id = Guid.NewGuid();
                using var command = connection.CreateCommand();
                command.Transaction = tx;
                command.CommandText = @"
                    INSERT INTO CatalogoVeiculos
                    (Id, Marca, Modelo, AnoInicial, AnoFinal, Motor, Aliases, Segmento, Ativo, DataCriacao)
                    VALUES
                    (@Id, @Marca, @Modelo, @AnoInicial, @AnoFinal, @Motor, @Aliases, 'Leve', 1, @DataCriacao);";
                command.Parameters.AddWithValue("@Id", id.ToString());
                command.Parameters.AddWithValue("@Marca", row.Marca);
                command.Parameters.AddWithValue("@Modelo", row.Modelo);
                command.Parameters.AddWithValue("@AnoInicial", row.AnoInicial);
                command.Parameters.AddWithValue("@AnoFinal", row.AnoFinal);
                command.Parameters.AddWithValue("@Motor", row.Motor);
                command.Parameters.AddWithValue("@Aliases", row.Aliases);
                command.Parameters.AddWithValue("@DataCriacao", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                command.ExecuteNonQuery();
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
            if (n.Contains("pastilha") || n.Contains("rolamento") || n.Contains("disco de freio"))
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
                   n.Contains("farol") || n.Contains("lampada") || n.Contains("buzina") || n.Contains("injetor");
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

        private static CatalogoVeiculo MaterializarVeiculo(SqliteDataReader reader)
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

        private readonly record SeedRow(string Marca, string Modelo, int AnoInicial, int AnoFinal, string Motor, string Aliases);

        private static readonly SeedRow[] SeedRows =
        {
''' + seed_array + r'''
        };
    }
}
'''

(ROOT / "Services" / "Catalogo" / "CatalogoVeiculoService.cs").write_text(service, encoding='utf-8')
print("Wrote CatalogoVeiculoService.cs")
