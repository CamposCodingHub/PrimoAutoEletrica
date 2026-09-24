from pathlib import Path
import re

# 1) Move OS window methods out of nested class
p = Path('PrimoAutoEletrica/Views/OrdemServicoWindow.xaml.cs')
t = p.read_text(encoding='utf-8')
start = t.find('        private OrdemServico? SincronizarOrdemDaTelaParaDocumento')
if start < 0:
    raise SystemExit('methods not found')
# find end of EnviarDocs method: after start, find that method and its closing brace at depth 0 for method
idx = t.find('        private void EnviarDocsWhatsAppOsButton_Click', start)
if idx < 0:
    raise SystemExit('EnviarDocs not found')
pos = t.find('{', idx)
depth = 0
while pos < len(t):
    ch = t[pos]
    if ch == '{':
        depth += 1
    elif ch == '}':
        depth -= 1
        if depth == 0:
            pos += 1
            break
    pos += 1
end = pos
methods = t[start:end]
t2 = t[:start] + t[end:]
nested = t2.find('    public sealed class OrdemServicoItemEditor')
if nested < 0:
    raise SystemExit('nested class not found')
if 'using PrimoAutoEletrica.Services;' not in t2:
    t2 = 'using PrimoAutoEletrica.Services;\n' + t2
    nested = t2.find('    public sealed class OrdemServicoItemEditor')
t3 = t2[:nested] + methods + '\n\n' + t2[nested:]
p.write_text(t3, encoding='utf-8')
print('MOVED OS methods')

# 2) Fix GerarLaudoEletrico GerarDocumento call - add incluirAssinatura: false
p2 = Path('PrimoAutoEletrica/Services/DocumentoPdfService.cs')
t = p2.read_text(encoding='utf-8')
# find signature of GerarDocumento
sig = re.search(r'(private|public).{0,40}GerarDocumento\(([^)]*)\)', t)
print('GerarDocumento params:', sig.group(2) if sig else None)
# In GerarLaudoEletrico method, the return GerarDocumento( ... new[] { ... }); needs 4th arg
# Replace the specific closing of laudo call: look for LAUDO TECNICO block
laudo_start = t.find('LAUDO TECNICO - AUTO ELETRICA')
if laudo_start < 0:
    raise SystemExit('laudo title missing')
# from return GerarDocumento before title
ret = t.rfind('return GerarDocumento', 0, laudo_start)
# find matching close of this call - semicolon after the call
# naive: find "Observacoes" section end then }); 
obs = t.find('"Observacoes"', laudo_start)
close = t.find('});', obs)
print('close snippet', repr(t[close-40:close+5]))
if 'incluirAssinatura' not in t[ret:close+3]:
    t = t[:close] + '}, incluirAssinatura: false);' + t[close+3:]
    # wait that replaced }); with }, incluir...); but we had }); meaning ) closes GerarDocumento and ; ends
    # original: return GerarDocumento( title, path, new[] { ... });
    # so });  is: } closes array, ) closes GerarDocumento, ; ends statement
    # replacing }); with }, incluirAssinatura: false);  gives: } closes array, , incluir... ) closes, ; 
    # BUT we used '}, incluirAssinatura: false);' replacing '});' - the } from original is consumed... 
    # t[close] starts at };  - close = index of }); 
    # t[:close] + '}, incluir...' + t[close+3:] 
    # if close points to }); then close+3 skips }); 
    # result: ...array contents + '}, incluirAssinatura: false);'
    # Missing the closing of array! Original ends with: }  )  ;
    # Pattern at close is }); so first char is }
    p2.write_text(t, encoding='utf-8')
    print('Patched incluirAssinatura')
else:
    print('already has incluirAssinatura')

# 3) Fix CommercialDocumentActions
p3 = Path('PrimoAutoEletrica/Services/CommercialDocumentActions.cs')
text = p3.read_text(encoding='utf-8')
new_method = '''        public static void AplicarGarantiaPadraoSeVazia(OrdemServico ordem)
        {
            if (ordem == null || ordem.GarantiaValidaAte.HasValue)
            {
                return;
            }

            var dias = 90;
            var tpl = "Servico com garantia padrao de {DiasGarantia} dias.";
            try
            {
                var svc = App.Services.GetService(typeof(SystemConfigurationService)) as SystemConfigurationService;
                if (svc != null)
                {
                    var conf = svc.LoadOrCreate(App.RuntimeAppDataPath);
                    dias = conf.DefaultWarrantyDays;
                    if (!string.IsNullOrWhiteSpace(conf.MessageTemplateGarantia))
                    {
                        tpl = conf.MessageTemplateGarantia;
                    }
                }
            }
            catch
            {
            }

            if (dias <= 0)
            {
                return;
            }

            var baseDate = ordem.DataEntrega ?? ordem.DataConclusao ?? DateTime.Now;
            ordem.GarantiaValidaAte = baseDate.Date.AddDays(dias);
            if (string.IsNullOrWhiteSpace(ordem.GarantiaObservacoes))
            {
                ordem.GarantiaObservacoes = tpl.Replace("{DiasGarantia}", dias.ToString());
            }
        }'''
text2, n = re.subn(
    r'        public static void AplicarGarantiaPadraoSeVazia\(OrdemServico ordem\)\s*\{.*?\n        \}',
    new_method,
    text,
    count=1,
    flags=re.S,
)
print('garantia replacements', n)
p3.write_text(text2, encoding='utf-8')

# 4) Fix AutoEletrica Guid?
p4 = Path('PrimoAutoEletrica/UserControls/AutoEletricaTecnicaControl.xaml.cs')
ae = p4.read_text(encoding='utf-8')
# VeiculoId is Guid? on prontuario
ae2 = ae.replace(
    '.FirstOrDefault(v => v.Id == _snapshot.Prontuario.VeiculoId);',
    '.FirstOrDefault(v => _snapshot.Prontuario.VeiculoId.HasValue && v.Id == _snapshot.Prontuario.VeiculoId.Value);'
)
# ObterPorId ClienteId - check if Guid or Guid?
veic = Path('PrimoAutoEletrica/Models/Veiculo.cs').read_text(encoding='utf-8')
print([l.strip() for l in veic.splitlines() if 'ClienteId' in l][:5])
if ae2 != ae:
    p4.write_text(ae2, encoding='utf-8')
    print('Fixed AutoEletrica VeiculoId compare')
else:
    print('AutoEletrica pattern not found; dumping snippet')
    i = ae.find('EnviarLaudoWhatsAppButton_Click')
    print(ae[i:i+700])

print('DONE fixes')
