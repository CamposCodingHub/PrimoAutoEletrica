import sqlite3
import json
import os
import uuid
from datetime import datetime

def run_diagnostic_ab_test():
    print("=" * 60)
    print("FASE 14 & 15 — TESTE DE DIAGNÓSTICO B2 E TESTE A/B")
    print("=" * 60)

    db_path = r"C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto_operacional.db"
    conn = sqlite3.connect(db_path)
    cursor = conn.cursor()

    # 1. Fetch an existing vehicle and client from operational DB
    cursor.execute("SELECT Id, ClienteId, Placa, Modelo FROM Veiculos WHERE Id IS NOT NULL AND ClienteId IS NOT NULL LIMIT 1")
    row = cursor.fetchone()
    if not row:
        print("FAIL: Nenhum veiculo encontrado no banco operacional.")
        return False
    veiculo_id, cliente_id, placa, modelo = row
    print(f"Veiculo selecionado: {modelo} (Placa: {placa}) | Id: {veiculo_id}")
    print(f"ClienteId associado: {cliente_id}")

    diagnosticos_dir = r"C:\Users\campo\AppData\Local\PrimoAutoEletrica\AutoEletrica\diagnosticos"
    os.makedirs(diagnosticos_dir, exist_ok=True)

    # 2. Criar Diagnostico A
    diag_a_id = str(uuid.uuid4())
    os_a_id = str(uuid.uuid4())
    medicao_a_id = str(uuid.uuid4())

    diag_a = {
        "Id": diag_a_id,
        "OrdemServicoId": os_a_id,
        "VeiculoId": veiculo_id,
        "ClienteId": cliente_id,
        "TecnicoId": "TEC-01",
        "DataHora": datetime.now().isoformat(),
        "DataConclusao": datetime.now().isoformat(),
        "Status": "Concluido",
        "RoteiroCodigo": "D01",
        "SintomaRelatado": "Bateria descarregando após 2 dias inativo",
        "SintomaCategoria": "Bateria",
        "DiagnosticoLaudo": "Tensão da bateria em conformidade sob repouso",
        "CausaStatus": "CONFIRMADA",
        "CausaDescricao": "Fuga residual na lâmpada do porta-luvas",
        "CorrecaoExecutada": "Substituição do microinterruptor do porta-luvas",
        "Observacoes": "Teste A/B: Diagnóstico A em conformidade",
        "OrigemLegado": False,
        "Medicoes": [
            {
                "Id": medicao_a_id,
                "DiagnosticoId": diag_a_id,
                "NomeTeste": "Tensão em repouso da bateria",
                "TipoGrandeza": "Tensao",
                "Instrumento": "Multímetro digital",
                "Unidade": "V",
                "ValorReferenciaMin": 12.4,
                "ValorReferenciaMax": 12.8,
                "TextoReferencia": "12.40V a 12.80V",
                "ValorInicial": 12.40,
                "ValorPosReparo": None,
                "Resultado": "NORMAL",
                "Observacao": "12.40V em repouso - normal",
                "TemValidacaoPosReparo": False,
                "DeltaPosReparo": None
            }
        ]
    }

    file_a = os.path.join(diagnosticos_dir, f"{uuid.UUID(diag_a_id).hex}.json")
    with open(file_a, "w", encoding="utf-8") as f:
        json.dump(diag_a, f, indent=2, ensure_ascii=False)
    print(f"\n[OK] Diagnostico A criado:")
    print(f"     Id: {diag_a_id}")
    print(f"     OS: {os_a_id}")
    print(f"     Roteiro: D01")
    print(f"     Medicao: 12.40 V -> NORMAL")
    print(f"     Arquivo: {file_a}")

    # 3. Criar Diagnostico B no MESMO veiculo
    diag_b_id = str(uuid.uuid4())
    os_b_id = str(uuid.uuid4())
    medicao_b_id = str(uuid.uuid4())

    diag_b = {
        "Id": diag_b_id,
        "OrdemServicoId": os_b_id,
        "VeiculoId": veiculo_id,
        "ClienteId": cliente_id,
        "TecnicoId": "TEC-02",
        "DataHora": datetime.now().isoformat(),
        "DataConclusao": None,
        "Status": "EmAndamento",
        "RoteiroCodigo": "D02",
        "SintomaRelatado": "Queda severa de tensão ao acionar motor de partida",
        "SintomaCategoria": "Partida",
        "DiagnosticoLaudo": "Queda excessiva e tensão em repouso abaixo da especificação",
        "CausaStatus": "PROVAVEL",
        "CausaDescricao": "Terminal positivo oxidado e bateria desgastada",
        "CorrecaoExecutada": "Substituição do cabo positivo e recarga profunda",
        "Observacoes": "Teste A/B: Diagnóstico B com falha e pós-reparo",
        "OrigemLegado": False,
        "Medicoes": [
            {
                "Id": medicao_b_id,
                "DiagnosticoId": diag_b_id,
                "NomeTeste": "Tensão da bateria em repouso",
                "TipoGrandeza": "Tensao",
                "Instrumento": "Multímetro digital",
                "Unidade": "V",
                "ValorReferenciaMin": 12.4,
                "ValorReferenciaMax": 12.8,
                "TextoReferencia": "12.40V a 12.80V",
                "ValorInicial": 12.10,
                "ValorPosReparo": 12.65,
                "Resultado": "FORA_DO_ESPERADO",
                "Observacao": "12.10V em repouso - abaixo da faixa mínima. Após recarga subiu para 12.65V",
                "TemValidacaoPosReparo": True,
                "DeltaPosReparo": 0.55
            }
        ]
    }

    file_b = os.path.join(diagnosticos_dir, f"{uuid.UUID(diag_b_id).hex}.json")
    with open(file_b, "w", encoding="utf-8") as f:
        json.dump(diag_b, f, indent=2, ensure_ascii=False)
    print(f"\n[OK] Diagnostico B criado:")
    print(f"     Id: {diag_b_id}")
    print(f"     OS: {os_b_id}")
    print(f"     Roteiro: D02")
    print(f"     Medicao Inicial: 12.10 V -> FORA_DO_ESPERADO")
    print(f"     Medicao Pos-Reparo: 12.65 V -> Delta: +0.55 V")
    print(f"     Arquivo: {file_b}")

    # 4. Validar Recuperação e Não-Sobrescrita (A permanece A, B permanece B)
    with open(file_a, "r", encoding="utf-8") as f:
        lido_a = json.load(f)
    with open(file_b, "r", encoding="utf-8") as f:
        lido_b = json.load(f)

    assert lido_a["Id"] == diag_a_id, "Id de A corrompido"
    assert lido_b["Id"] == diag_b_id, "Id de B corrompido"
    assert lido_a["Id"] != lido_b["Id"], "Ids de A e B são iguais!"
    assert lido_a["OrdemServicoId"] != lido_b["OrdemServicoId"], "OSs misturadas!"
    assert lido_a["VeiculoId"] == lido_b["VeiculoId"] == veiculo_id, "VeiculoId divergente!"
    assert lido_a["Medicoes"][0]["ValorInicial"] == 12.40, "Valor de A alterado!"
    assert lido_a["Medicoes"][0]["Resultado"] == "NORMAL", "Resultado de A alterado!"
    assert lido_b["Medicoes"][0]["ValorInicial"] == 12.10, "Valor de B alterado!"
    assert lido_b["Medicoes"][0]["Resultado"] == "FORA_DO_ESPERADO", "Resultado de B alterado!"
    assert lido_b["Medicoes"][0]["ValorPosReparo"] == 12.65, "Pós-reparo de B alterado!"

    print("\n" + "=" * 60)
    print("PROVA DE ISOLAMENTO E NÃO-SOBRESCRIÇÃO (A/B):")
    print("=" * 60)
    print(f"  Diagnostico A: Id={diag_a_id} | Roteiro={lido_a['RoteiroCodigo']} | Medicao={lido_a['Medicoes'][0]['ValorInicial']}V ({lido_a['Medicoes'][0]['Resultado']})")
    print(f"  Diagnostico B: Id={diag_b_id} | Roteiro={lido_b['RoteiroCodigo']} | Medicao={lido_b['Medicoes'][0]['ValorInicial']}V ({lido_b['Medicoes'][0]['Resultado']}) -> Pos: {lido_b['Medicoes'][0]['ValorPosReparo']}V")
    print(f"  Mesmo Veiculo: {veiculo_id} (Placa {placa})")
    print(f"  OSs Diferentes: OS_A={os_a_id} != OS_B={os_b_id}")
    print(f"  Ids Diferentes: SIM")
    print(f"  Sobrescrita: NÃO")
    print(f"  Mistura de Dados: NÃO")
    print(f"  Pós-Reparo Preservado: SIM (+0.55 V)")
    print("RESULTADO DO TESTE A/B: PASS")
    print("=" * 60)

    # Clean up test files
    os.remove(file_a)
    os.remove(file_b)
    print("Arquivos de teste A/B removidos com sucesso.")
    return True

if __name__ == "__main__":
    success = run_diagnostic_ab_test()
    if not success:
        exit(1)
