using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PrimoAutoEletrica.Models
{
    public class ProdutoImportado : INotifyPropertyChanged
    {
        private string _codigo = string.Empty;
        private string _codigoBarras = string.Empty;
        private string _nome = string.Empty;
        private string _ncm = string.Empty;
        private string _cfop = string.Empty;
        private decimal _quantidade;
        private decimal _valorUnitario;
        private decimal _valorTotal;
        private string _unidadeMedida = string.Empty;
        private StatusImportacao _status;
        private string _motivoIgnorado = string.Empty;
        private Guid? _produtoExistenteId;
        private bool _selecionadoParaImportacao = true;
        private string _acaoPlanejada = AcoesPlanejadas.CriarNovo;
        private string _categoriaSugerida = string.Empty;
        private decimal _margemAplicada = 200m;
        private decimal _precoVendaSugerido;
        private string _produtoVinculadoReferencia = string.Empty;
        private string _observacaoConferencia = string.Empty;
        private string _produtoSnapshotAnterior = string.Empty;
        private string _produtoSnapshotPosterior = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Codigo
        {
            get => _codigo;
            set => SetField(ref _codigo, value);
        }

        public string CodigoBarras
        {
            get => _codigoBarras;
            set => SetField(ref _codigoBarras, value);
        }

        public string Nome
        {
            get => _nome;
            set => SetField(ref _nome, value);
        }

        public string NCM
        {
            get => _ncm;
            set => SetField(ref _ncm, value);
        }

        public string CFOP
        {
            get => _cfop;
            set => SetField(ref _cfop, value);
        }

        public decimal Quantidade
        {
            get => _quantidade;
            set => SetField(ref _quantidade, value);
        }

        public decimal ValorUnitario
        {
            get => _valorUnitario;
            set => SetField(ref _valorUnitario, value);
        }

        public decimal ValorTotal
        {
            get => _valorTotal;
            set => SetField(ref _valorTotal, value);
        }

        public string UnidadeMedida
        {
            get => _unidadeMedida;
            set => SetField(ref _unidadeMedida, value);
        }

        public StatusImportacao Status
        {
            get => _status;
            set
            {
                if (SetField(ref _status, value))
                {
                    OnPropertyChanged(nameof(StatusDescricao));
                    OnPropertyChanged(nameof(TemPendencia));
                }
            }
        }

        public string MotivoIgnorado
        {
            get => _motivoIgnorado;
            set
            {
                if (SetField(ref _motivoIgnorado, value))
                {
                    OnPropertyChanged(nameof(TemPendencia));
                }
            }
        }

        public Guid? ProdutoExistenteId
        {
            get => _produtoExistenteId;
            set
            {
                if (SetField(ref _produtoExistenteId, value))
                {
                    OnPropertyChanged(nameof(IsNovo));
                    OnPropertyChanged(nameof(IsAtualizado));
                }
            }
        }

        public bool SelecionadoParaImportacao
        {
            get => _selecionadoParaImportacao;
            set
            {
                if (SetField(ref _selecionadoParaImportacao, value))
                {
                    OnPropertyChanged(nameof(TemPendencia));
                }
            }
        }

        public string AcaoPlanejada
        {
            get => _acaoPlanejada;
            set
            {
                if (SetField(ref _acaoPlanejada, value))
                {
                    OnPropertyChanged(nameof(TemPendencia));
                }
            }
        }

        public string CategoriaSugerida
        {
            get => _categoriaSugerida;
            set
            {
                if (SetField(ref _categoriaSugerida, value))
                {
                    OnPropertyChanged(nameof(TemPendencia));
                }
            }
        }

        public decimal MargemAplicada
        {
            get => _margemAplicada;
            set
            {
                if (SetField(ref _margemAplicada, value))
                {
                    RecalcularPrecoVenda();
                }
            }
        }

        public decimal PrecoVendaSugerido
        {
            get => _precoVendaSugerido;
            set
            {
                if (SetField(ref _precoVendaSugerido, value))
                {
                    OnPropertyChanged(nameof(TemPendencia));
                }
            }
        }

        public string ProdutoVinculadoReferencia
        {
            get => _produtoVinculadoReferencia;
            set
            {
                if (SetField(ref _produtoVinculadoReferencia, value))
                {
                    OnPropertyChanged(nameof(TemPendencia));
                }
            }
        }

        public string ObservacaoConferencia
        {
            get => _observacaoConferencia;
            set
            {
                if (SetField(ref _observacaoConferencia, value))
                {
                    OnPropertyChanged(nameof(TemPendencia));
                }
            }
        }

        public string ProdutoSnapshotAnterior
        {
            get => _produtoSnapshotAnterior;
            set => SetField(ref _produtoSnapshotAnterior, value);
        }

        public string ProdutoSnapshotPosterior
        {
            get => _produtoSnapshotPosterior;
            set => SetField(ref _produtoSnapshotPosterior, value);
        }

        public bool IsNovo => ProdutoExistenteId == null;
        public bool IsAtualizado => ProdutoExistenteId != null;

        public string StatusDescricao => Status switch
        {
            StatusImportacao.Pendente => "Pendente",
            StatusImportacao.Novo => "Novo",
            StatusImportacao.Atualizado => "Atualizado",
            StatusImportacao.Duplicado => "Duplicado",
            StatusImportacao.Ignorado => "Ignorado",
            StatusImportacao.Erro => "Erro",
            _ => "Pendente"
        };

        public bool TemPendencia =>
            SelecionadoParaImportacao &&
            (Status == StatusImportacao.Erro ||
             (!string.IsNullOrWhiteSpace(MotivoIgnorado) && Status != StatusImportacao.Ignorado));

        public void RecalcularPrecoVenda()
        {
            if (ValorUnitario <= 0)
            {
                PrecoVendaSugerido = 0;
                return;
            }

            var fator = 1m + (MargemAplicada / 100m);
            PrecoVendaSugerido = Math.Round(ValorUnitario * fator, 2, MidpointRounding.AwayFromZero);
        }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public static class AcoesPlanejadas
    {
        public const string CriarNovo = "Criar novo";
        public const string AtualizarExistente = "Atualizar existente";
        public const string Ignorar = "Ignorar";

        public static readonly IReadOnlyList<string> Todas = new[]
        {
            CriarNovo,
            AtualizarExistente,
            Ignorar
        };
    }

    public enum StatusImportacao
    {
        Pendente,
        Novo,
        Atualizado,
        Duplicado,
        Ignorado,
        Erro
    }
}
