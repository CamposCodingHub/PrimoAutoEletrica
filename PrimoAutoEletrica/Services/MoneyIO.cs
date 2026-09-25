using System;
using System.Data;
using System.Data.Common;

namespace PrimoAutoEletrica.Services
{
    /// <summary>
    /// Modo de persistência monetária no banco SQLite.
    /// LegacyReal: colunas armazenam valores reais/decimais (pré-migração).
    /// CentsV1: colunas armazenam centavos inteiros INTEGER (pós-migração).
    /// </summary>
    public enum MoneyPersistenceMode
    {
        LegacyReal = 0,
        CentsV1 = 1
    }

    /// <summary>
    /// Abstração oficial e unificada para I/O monetário no PRIMOX Workshop.
    /// Garante que valores financeiros sejam lidos e gravados com total exatidão matemática,
    /// sem resíduos de ponto flutuante IEEE 754 e com tratamento controlado de NULL.
    /// </summary>
    public static class MoneyIO
    {
        /// <summary>
        /// Modo de persistência ativo. Por padrão configurado como CentsV1 para o novo padrão,
        /// podendo ser comutado para LegacyReal em ambientes que ainda utilizam schema antigo.
        /// </summary>
        public static MoneyPersistenceMode DefaultMode { get; set; } = MoneyPersistenceMode.CentsV1;

        /// <summary>
        /// Lê um valor monetário de campo NOT NULL.
        /// Lança InvalidOperationException se o banco contiver DBNull inesperado em campo obrigatório.
        /// </summary>
        public static decimal LerMoeda(DbDataReader reader, int index, MoneyPersistenceMode? mode = null)
        {
            if (reader.IsDBNull(index))
                throw new InvalidOperationException($"Violação de integridade: Coluna monetária no índice {index} retornou DBNull em campo NOT NULL.");

            var activeMode = mode ?? DefaultMode;
            var val = reader.GetValue(index);

            if (activeMode == MoneyPersistenceMode.CentsV1)
            {
                if (val is long l) return l / 100m;
                if (val is int i) return i / 100m;
                if (val is short s) return s / 100m;
                if (val is byte b) return b / 100m;
                if (val is double d) return Convert.ToDecimal(d) / 100m;
                if (val is float f) return Convert.ToDecimal(f) / 100m;
                if (val is decimal m) return m / 100m;

                var str = Convert.ToString(val);
                if (string.IsNullOrWhiteSpace(str)) return 0m;
                if (str.Contains('.') || str.Contains(','))
                {
                    if (decimal.TryParse(str, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsedDec))
                        return parsedDec / 100m;
                    return 0m;
                }

                return Convert.ToInt64(val) / 100m;
            }

            return Convert.ToDecimal(val);
        }

        /// <summary>
        /// Lê um valor monetário de campo NULLABLE.
        /// Retorna null se o valor no banco for DBNull.
        /// </summary>
        public static decimal? LerMoedaNullable(DbDataReader reader, int index, MoneyPersistenceMode? mode = null)
        {
            if (reader.IsDBNull(index))
                return null;

            var activeMode = mode ?? DefaultMode;
            var val = reader.GetValue(index);

            if (activeMode == MoneyPersistenceMode.CentsV1)
            {
                if (val is long l) return l / 100m;
                if (val is int i) return i / 100m;
                if (val is short s) return s / 100m;
                if (val is byte b) return b / 100m;
                if (val is double d) return Convert.ToDecimal(d) / 100m;
                if (val is float f) return Convert.ToDecimal(f) / 100m;
                if (val is decimal m) return m / 100m;

                var str = Convert.ToString(val);
                if (string.IsNullOrWhiteSpace(str)) return null;
                if (str.Contains('.') || str.Contains(','))
                {
                    if (decimal.TryParse(str, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsedDec))
                        return parsedDec / 100m;
                    return 0m;
                }

                return Convert.ToInt64(val) / 100m;
            }

            return Convert.ToDecimal(val);
        }

        /// <summary>
        /// Grava um valor monetário obrigatório em um comando SQL.
        /// Em CentsV1, converte decimal -> long cents via AwayFromZero e adiciona parâmetro Int64.
        /// Em LegacyReal, adiciona parâmetro Decimal.
        /// </summary>
        public static void GravarMoeda(DbCommand command, string paramName, decimal value, MoneyPersistenceMode? mode = null)
        {
            var activeMode = mode ?? DefaultMode;
            var param = command.CreateParameter();
            param.ParameterName = paramName;

            if (activeMode == MoneyPersistenceMode.CentsV1)
            {
                long cents = MoneyCents.FromDecimal(value).Cents;
                param.DbType = DbType.Int64;
                param.Value = cents;
            }
            else
            {
                param.DbType = DbType.Decimal;
                param.Value = value;
            }

            command.Parameters.Add(param);
        }

        /// <summary>
        /// Grava um valor monetário opcional (anulável) em um comando SQL.
        /// </summary>
        public static void GravarMoedaNullable(DbCommand command, string paramName, decimal? value, MoneyPersistenceMode? mode = null)
        {
            var activeMode = mode ?? DefaultMode;
            var param = command.CreateParameter();
            param.ParameterName = paramName;

            if (!value.HasValue)
            {
                param.Value = DBNull.Value;
                param.DbType = (activeMode == MoneyPersistenceMode.CentsV1) ? DbType.Int64 : DbType.Decimal;
            }
            else
            {
                if (activeMode == MoneyPersistenceMode.CentsV1)
                {
                    long cents = MoneyCents.FromDecimal(value.Value).Cents;
                    param.DbType = DbType.Int64;
                    param.Value = cents;
                }
                else
                {
                    param.DbType = DbType.Decimal;
                    param.Value = value.Value;
                }
            }

            command.Parameters.Add(param);
        }

        /// <summary>
        /// Converte o resultado de agregação SQL (SUM, AVG, MIN, MAX).
        /// Em CentsV1, o SUM/MIN/MAX no banco retorna centavos inteiros (long), que devem ser divididos por 100m.
        /// </summary>
        public static decimal ConverterAgregacao(object? scalarResult, MoneyPersistenceMode? mode = null)
        {
            if (scalarResult == null || scalarResult == DBNull.Value)
                return 0m;

            var activeMode = mode ?? DefaultMode;
            if (activeMode == MoneyPersistenceMode.CentsV1)
            {
                if (scalarResult is long l) return l / 100m;
                if (scalarResult is int i) return i / 100m;
                if (scalarResult is short s) return s / 100m;
                if (scalarResult is byte b) return b / 100m;
                if (scalarResult is double d) return Convert.ToDecimal(d) / 100m;
                if (scalarResult is float f) return Convert.ToDecimal(f) / 100m;
                if (scalarResult is decimal m) return m / 100m;

                var str = Convert.ToString(scalarResult);
                if (string.IsNullOrWhiteSpace(str)) return 0m;
                if (str.Contains('.') || str.Contains(','))
                {
                    if (decimal.TryParse(str, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsedDec))
                        return parsedDec / 100m;
                    return 0m;
                }

                return Convert.ToInt64(scalarResult) / 100m;
            }

            return Convert.ToDecimal(scalarResult);
        }

        /// <summary>
        /// Prepara um valor de filtro para cláusulas WHERE / BETWEEN.
        /// Retorna long cents se CentsV1, ou decimal se LegacyReal.
        /// </summary>
        public static object PrepararFiltro(decimal value, MoneyPersistenceMode? mode = null)
        {
            var activeMode = mode ?? DefaultMode;
            if (activeMode == MoneyPersistenceMode.CentsV1)
            {
                return MoneyCents.FromDecimal(value).Cents;
            }
            return value;
        }

        /// <summary>
        /// Detecta o modo de persistência inspecionando o PRAGMA user_version do banco de dados SQLite.
        /// user_version >= 1 indica schema em centavos (CentsV1).
        /// user_version == 0 indica schema original em reais (LegacyReal).
        /// </summary>
        public static MoneyPersistenceMode DetectMode(DbConnection connection)
        {
            try
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "PRAGMA user_version;";
                var result = cmd.ExecuteScalar();
                int version = Convert.ToInt32(result);
                return version >= 1 ? MoneyPersistenceMode.CentsV1 : MoneyPersistenceMode.LegacyReal;
            }
            catch
            {
                return MoneyPersistenceMode.LegacyReal;
            }
        }
    }
}
