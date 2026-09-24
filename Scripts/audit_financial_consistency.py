import sqlite3

def audit_financials():
    conn = sqlite3.connect(r'C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto_operacional.db')
    c = conn.cursor()
    
    # 1. ContasReceber
    cr_count = c.execute("SELECT count(*) FROM ContasReceber").fetchone()[0]
    cr_pago = c.execute("SELECT count(*), coalesce(sum(Valor),0) FROM ContasReceber WHERE Status='Pago' OR DataPagamento IS NOT NULL").fetchone()
    cr_pend = c.execute("SELECT count(*), coalesce(sum(Valor),0) FROM ContasReceber WHERE Status!='Pago' AND DataPagamento IS NULL").fetchone()
    
    # 2. ContasPagar
    cp_count = c.execute("SELECT count(*) FROM ContasPagar").fetchone()[0]
    cp_pago = c.execute("SELECT count(*), coalesce(sum(Valor),0) FROM ContasPagar WHERE Status='Pago' OR DataPagamento IS NOT NULL").fetchone()
    cp_pend = c.execute("SELECT count(*), coalesce(sum(Valor),0) FROM ContasPagar WHERE Status!='Pago' AND DataPagamento IS NULL").fetchone()
    
    # 3. MovimentacoesFinanceiras
    mf_count = c.execute("SELECT count(*) FROM MovimentacoesFinanceiras").fetchone()[0]
    entradas = c.execute("SELECT coalesce(sum(Valor),0) FROM MovimentacoesFinanceiras WHERE Tipo='Receita' OR Tipo='Entrada'").fetchone()[0]
    saidas = c.execute("SELECT coalesce(sum(Valor),0) FROM MovimentacoesFinanceiras WHERE Tipo='Despesa' OR Tipo='Saida'").fetchone()[0]
    
    # 4. OrdensServico financeiro
    os_count = c.execute("SELECT count(*) FROM OrdensServico").fetchone()[0]
    os_com_cliente = c.execute("SELECT count(*) FROM OrdensServico WHERE ClienteId IS NOT NULL AND ClienteId != ''").fetchone()[0]
    os_com_veiculo = c.execute("SELECT count(*) FROM OrdensServico WHERE VeiculoId IS NOT NULL AND VeiculoId != ''").fetchone()[0]
    
    conn.close()
    
    print("=== AUDITORIA FINANCEIRA OPERACIONAL ===")
    print(f"ContasReceber: Total={cr_count} | Pagas={cr_pago[0]} (R$ {cr_pago[1]:,.2f}) | Pendentes={cr_pend[0]} (R$ {cr_pend[1]:,.2f})")
    print(f"ContasPagar:   Total={cp_count} | Pagas={cp_pago[0]} (R$ {cp_pago[1]:,.2f}) | Pendentes={cp_pend[0]} (R$ {cp_pend[1]:,.2f})")
    print(f"Movimentacoes: Total={mf_count} | Entradas=R$ {entradas:,.2f} | Saidas=R$ {saidas:,.2f} | Saldo=R$ {entradas - saidas:,.2f}")
    print(f"OrdensServico: Total={os_count} | Com ClienteId={os_com_cliente} ({os_com_cliente/os_count*100:.1f}%) | Com VeiculoId={os_com_veiculo} ({os_com_veiculo/os_count*100:.1f}%)")

if __name__ == "__main__":
    audit_financials()
