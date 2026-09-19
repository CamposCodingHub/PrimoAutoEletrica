# Fase B expandida — frota leve + pesada + internacional

## Correcao UI (2026-09-19)
- Combos Marca/Modelo vazios: `CarregarDados` agora chama `AtualizarCombosVeiculo` no Loaded.
- Dropdown cortado: MaxDropDownHeight + filtro so apos fechar popup.

## Frota
- ~171 veiculos (leve + pesada + internacional)
- ~51 pesados (MB Actros/Atego/Accelo/Sprinter, Volvo FH/VM, Scania, Iveco Daily/Tector, VW Constellation/Delivery, Ford Cargo, MAN, DAF, Hino, Isuzu, Foton, onibus...)
- Marcas no filtro: 37+

## Pecas / vinculos
- ~27k vinculos peca↔veiculo
- Inclui SKF/IKRO/rolamentos (ex.: rolamento de alternador por veiculo)
- Ex.: HB20 2014 ~147 pecas (31 rolamentos); Actros 2018 ~215 pecas (39 rolamentos)

## Re-seed
`python Scripts\_expand_fase_b_frota.py`
