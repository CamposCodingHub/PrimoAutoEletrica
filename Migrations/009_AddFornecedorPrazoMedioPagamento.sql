-- Adiciona prazo medio estruturado de pagamento ao cadastro de fornecedores.
ALTER TABLE Fornecedores ADD COLUMN PrazoMedioPagamentoDias INTEGER NOT NULL DEFAULT 0;
