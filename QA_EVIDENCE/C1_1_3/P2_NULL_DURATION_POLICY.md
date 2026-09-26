# C1.1.3 NULL duration policy

Field: Agendamentos.DuracaoEstimada / DuracaoReal
Type SQLite: TEXT nullable
Domain: TimeSpan (non-nullable)

NULL meaning: duration not recorded yet (legacy/QA inserts with HoraInicio/HoraTermino only).
Mapping: NULL → TimeSpan.Zero
Justification: existing code already treats DuracaoEstimada == Zero as unset and falls back to 2h (DeterminarDataPrevisaoOrdemServico). DuracaoReal already mapped NULL→Zero before C1.1.3.

NOT used: arbitrary money zeroing; NOT converting required FKs.
