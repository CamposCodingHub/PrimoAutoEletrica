import sqlite3
p=r'C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto_operacional.db'
con=sqlite3.connect(p)
cur=con.cursor()
print('VendaItens tipos:', cur.execute('SELECT DISTINCT Tipo, COUNT(*) FROM VendaItens GROUP BY Tipo').fetchall())
print('OS itens tipos:', cur.execute('SELECT DISTINCT Tipo, COUNT(*) FROM OrdemServicoItens GROUP BY Tipo').fetchall())
q='''
SELECT COALESCE(NULLIF(vi.DescricaoItem,''), NULLIF(vi.ProdutoNome,''), '?') ref,
       SUM(CASE WHEN COALESCE(vi.Subtotal,0)>0 THEN vi.Subtotal ELSE vi.Quantidade*vi.PrecoUnitario - COALESCE(vi.Desconto,0) END) receita,
       SUM(vi.Quantidade*COALESCE(vi.CustoUnitario,0)) custo,
       COUNT(*) n
FROM VendaItens vi
JOIN Vendas v ON v.Id=vi.VendaId
WHERE date(v.Data) >= date('now','start of month')
  AND (LOWER(COALESCE(vi.Tipo,'produto')) LIKE 'servi%' OR LOWER(COALESCE(vi.Tipo,'')) = 'servico')
GROUP BY 1
ORDER BY (receita-custo) DESC
LIMIT 12
'''
print('top servi month:')
for r in cur.execute(q):
    print(r)
con.close()
