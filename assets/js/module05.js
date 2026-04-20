/**
 * module05.js - Lógica do Módulo 05 (Sistemas de Iluminação)
 * Versão completa e corrigida - com Páginas 1 a 8
 */
(function () {
    'use strict';

    const pages = ['home', 'p1', 'p2', 'p3', 'p4', 'p5', 'p6', 'p7', 'p8'];
    let visitedPages = new Set();

    // ===== PROGRESSO =====
    function loadProgress() {
        const saved = localStorage.getItem('modulo05-visited');
        if (saved) visitedPages = new Set(JSON.parse(saved));
        updateProgressUI();
    }

    function saveProgress() {
        localStorage.setItem('modulo05-visited', JSON.stringify([...visitedPages]));
        updateProgressUI();
    }

    function updateProgressUI() {
        const total = pages.filter(p => p !== 'home').length;
        const pct = Math.round((visitedPages.size / total) * 100);
        const progBar = document.getElementById('prog-bar');
        const progText = document.getElementById('prog-pct');
        if (progBar) progBar.style.width = pct + '%';
        if (progText) progText.textContent = pct + '%';
    }

    // ===== NAVEGAÇÃO =====
    function showPage(pageId) {
        document.querySelectorAll('.lesson-page').forEach(el => el.classList.remove('active'));
        const target = document.getElementById(`page-${pageId}`);
        if (target) target.classList.add('active');
        document.querySelectorAll('.nav-item').forEach(item => item.classList.remove('active'));
        const activeItem = document.querySelector(`.nav-item[data-page="${pageId}"]`);
        if (activeItem) activeItem.classList.add('active');
        document.querySelector('.main').scrollTo({ top: 0, behavior: 'smooth' });
        if (pageId !== 'home' && !visitedPages.has(pageId)) {
            visitedPages.add(pageId);
            saveProgress();
        }

        // Inicializações específicas por página
        if (pageId === 'p1') initDiagnosisSimulator();
        if (pageId === 'p2') { initComponentCards(); initDiagnosticSim(); }
        if (pageId === 'p3') { initXenonComponentCards(); initXenonDiagram(); initKelvinSelector(); initXenonDiagnosisSim(); }
        if (pageId === 'p4') { initLedComponentCards(); initLedDiagnosisSim(); initMatrixDiagram(); }
        if (pageId === 'p5') initDiagnosisSymptomSim();
        if (pageId === 'p6') initAlignmentSimulator();
        if (pageId === 'p7') { initAfsComponentCards(); initAfsModesSelector(); initMatrixAdvancedTooltip(); initAfsDiagnosisSim(); }
        if (pageId === 'p8') { initCircuitDiagramTooltip(); initWireCalculator(); }
    }

    // ===== SIMULADORES =====

    function testFarolCircuit() {
        const fusivel = document.getElementById('sim-fusivel')?.checked;
        const rele = document.getElementById('sim-rele')?.checked;
        const lampada = document.getElementById('sim-lampada')?.checked;
        const massa = document.getElementById('sim-massa')?.checked;
        const indicator = document.getElementById('farol-indicator');
        const msg = document.getElementById('farol-status-msg');
        if (!indicator || !msg) return;
        let status = '';
        let icon = '💡';
        if (!fusivel) { status = 'Fusível queimado!'; icon = '❌'; }
        else if (!rele) { status = 'Relé não aciona!'; icon = '❌'; }
        else if (!lampada) { status = 'Lâmpada queimada!'; icon = '❌'; }
        else if (!massa) { status = 'Massa ruim!'; icon = '⚠️'; }
        else { status = 'Funcionando normalmente'; icon = '✅'; }
        indicator.textContent = icon + ' Farol';
        msg.textContent = 'Status: ' + status;
    }

    function updateXenonSim() {
        const fault = document.getElementById('xenon-fault-select')?.value;
        const display = document.getElementById('xenon-display');
        const diag = document.getElementById('xenon-diagnosis');
        if (!display || !diag) return;
        if (fault === 'ok') { display.textContent = '💡 Luz estável'; diag.textContent = 'Sistema funcionando corretamente.'; }
        else if (fault === 'bulb') { display.textContent = '💡🌫️ Luz fraca/rosada'; diag.textContent = 'Lâmpada envelhecida. Trocar o par.'; }
        else if (fault === 'ballast') { display.textContent = '❌ Não acende'; diag.textContent = 'Reator com defeito. Verificar alimentação e substituir.'; }
        else if (fault === 'igniter') { display.textContent = '💡💡 Piscando'; diag.textContent = 'Ignitor ou mau contato. Testar com outro reator.'; }
    }

    function calcLED() {
        const vs = parseFloat(document.getElementById('led-vs')?.value) || 12;
        const vf = parseFloat(document.getElementById('led-vf')?.value) || 3.2;
        const ifma = parseFloat(document.getElementById('led-if')?.value) || 20;
        const n = parseInt(document.getElementById('led-n')?.value) || 1;
        const totalVf = vf * n;
        const resDiv = document.getElementById('led-result');
        if (!resDiv) return;
        if (totalVf >= vs) { resDiv.innerHTML = '❌ Tensão dos LEDs maior que a fonte. Reduza a quantidade.'; return; }
        const rExact = (vs - totalVf) / (ifma / 1000);
        const stdRes = [10, 12, 15, 18, 22, 27, 33, 39, 47, 56, 68, 82, 100, 120, 150, 180, 220, 270, 330, 390, 470, 560, 680, 820, 1000];
        const rStd = stdRes.find(r => r >= rExact) || 1000;
        const iActual = ((vs - totalVf) / rStd) * 1000;
        const power = ((vs - totalVf) / rStd) * vs * 1000;
        resDiv.innerHTML = `Resistor calculado: ${rExact.toFixed(1)}Ω → Use ${rStd}Ω (${iActual.toFixed(1)}mA). Potência: ${power.toFixed(0)}mW.`;
    }

    function calcAux() {
        const power = parseFloat(document.getElementById('aux-power')?.value) || 100;
        const volt = parseFloat(document.getElementById('aux-volt')?.value) || 12;
        const current = power / volt;
        let fuse = 5;
        if (current > 4) fuse = 7.5;
        if (current > 6) fuse = 10;
        if (current > 9) fuse = 15;
        if (current > 13) fuse = 20;
        if (current > 17) fuse = 25;
        if (current > 23) fuse = 30;
        let bitola = '1.5mm²';
        if (current > 10) bitola = '2.5mm²';
        if (current > 15) bitola = '4.0mm²';
        if (current > 25) bitola = '6.0mm²';
        const resultDiv = document.getElementById('aux-result');
        if (resultDiv) resultDiv.innerHTML = `Corrente: ${current.toFixed(1)}A | Fusível sugerido: ${fuse}A | Bitola: ${bitola}`;
    }

    function initDiagnosisSimulator() {
        const select = document.getElementById('fault-select');
        const resultDiv = document.getElementById('diagnosis-result');
        if (!select || !resultDiv) return;

        const diagnosisData = {
            'one-side': { title: 'Diagnóstico: Um lado não acende', steps: ['1. Teste a lâmpada do lado apagado no lado que funciona.', '2. Verifique o fusível individual do circuito.', '3. Meça a tensão no soquete.'] },
            'both-sides': { title: 'Diagnóstico: Nenhum farol acende', steps: ['1. Verifique o fusível principal.', '2. Teste o relé dos faróis.', '3. Verifique o interruptor de comando.'] },
            'weak': { title: 'Diagnóstico: Farol fraco ou amarelado', steps: ['1. Meça a tensão no soquete.', '2. Teste de queda de tensão na massa.', '3. Verifique a lente do farol.', '4. Troque o par se > 2 anos.'] },
            'flicker': { title: 'Diagnóstico: Farol Xenon piscando', steps: ['1. Troque a lâmpada de lado.', '2. Teste reator e ignitor.', '3. Verifique alimentação.'] },
            'led-error': { title: 'Diagnóstico: LED não acende ou erro no painel', steps: ['1. Verifique a polaridade.', '2. Instale decodificador CANbus.', '3. Verifique compatibilidade.'] }
        };

        select.addEventListener('change', function () {
            const fault = this.value;
            const data = diagnosisData[fault];
            if (data) {
                let html = `<div style="font-weight: 600; margin-bottom: 1rem; color: var(--accent);">🔍 ${data.title}</div>`;
                data.steps.forEach(step => { html += `<div style="margin-bottom: 0.5rem;">${step}</div>`; });
                resultDiv.innerHTML = html;
            } else {
                resultDiv.innerHTML = '<div class="result-placeholder">Selecione um sintoma para ver o diagnóstico.</div>';
            }
        });
    }

    // ===== QUIZZES =====
    const quizExplain = {
        q1: 'O Xenon (HID) necessita de alta tensão para ignição, fornecida pelo reator (ballast).',
        q1b: 'A lâmpada H4, por possuir dois filamentos (alto e baixo) no mesmo bulbo, é a mais comum em veículos populares brasileiros.',
        q2: 'Se o relé clica, o comando está OK. Deve-se verificar alimentação e massa no soquete.',
        q2b: 'Uma massa com alta resistência causa queda de tensão e reduz a luminosidade.',
        q3: 'O ignitor gera um pulso de alta tensão (~20.000V) para ionizar o gás xenônio e iniciar o arco elétrico.',
        q3b: '4300K é a temperatura de cor OEM (original de fábrica) porque oferece o melhor equilíbrio entre luminosidade e penetração.',
        q4: 'O driver (controlador) converte a tensão da bateria em corrente constante para o LED.',
        q4b: 'O flickering é causado pelo CANbus interpretando baixo consumo. A solução é o decodificador CANbus.',
        q4c: 'O farol a laser oferece maior alcance (até 600m) e eficiência (170 lm/W).',
        q5: 'O teste de queda de tensão avalia o circuito sob carga real, revelando resistências ocultas.',
        q5b: '180mA é corrente parasita excessiva. Deve-se localizar o circuito responsável.',
        q6: 'A inclinação padrão é de 1% (1 cm para cada metro de distância).',
        q6b: 'Luzes decorativas não substituem o DRL de fábrica ou farol baixo, conforme Lei 14.071/2021.',
        q7: 'O sensor de ângulo de direção informa ao módulo AFS para onde o volante está sendo girado.',
        q7b: 'O Matrix LED desliga seletivamente LEDs específicos, criando uma "sombra" protetora.',
        q8: 'A regra prática é dimensionar o fusível para 1,25 × a corrente da carga e escolher o valor comercial imediatamente superior.',
        q8b: 'Para 9,17A e 5m de comprimento, a bitola de 1,5mm² (16 AWG) é suficiente. Em distâncias maiores, use 2,5mm².',
    };

    function initQuizzes() {
        document.querySelectorAll('.quiz-options').forEach(opts => {
            const id = opts.id ? opts.id.replace('-opts', '') : null;
            if (!id) return;
            opts.querySelectorAll('.quiz-option').forEach(opt => {
                opt.addEventListener('click', () => {
                    if (opts.dataset.done) return;
                    opts.dataset.done = '1';
                    opts.querySelectorAll('.quiz-option').forEach(o => o.style.pointerEvents = 'none');
                    const correct = opt.dataset.correct === 'true';
                    opt.classList.add(correct ? 'correct' : 'wrong');
                    const fb = document.getElementById(id + '-fb');
                    if (fb) {
                        fb.className = 'quiz-feedback show ' + (correct ? 'ok' : 'fail');
                        fb.textContent = (correct ? '✅ Correto! ' : '❌ Não é essa. ') + (quizExplain[id] || '');
                    }
                    if (correct) {
                        const pageMap = { q1: 'p1', q1b: 'p1', q2: 'p2', q2b: 'p2', q3: 'p3', q3b: 'p3', q4: 'p4', q4b: 'p4', q4c: 'p4', q5: 'p5', q5b: 'p5', q6: 'p6', q6b: 'p6', q7: 'p7', q7b: 'p7', q8: 'p8', q8b: 'p8' };
                        if (pageMap[id] && !visitedPages.has(pageMap[id])) {
                            visitedPages.add(pageMap[id]);
                            saveProgress();
                        }
                    }
                });
            });
        });
    }

    function initMatrixDiagram() {
        const areas = document.querySelectorAll('.matrix-diagram-interactive .hover-area');
        const tooltip = document.getElementById('matrix-tooltip');
        if (!areas.length || !tooltip) return;
        areas.forEach(area => {
            area.addEventListener('mouseenter', () => {
                const tip = area.dataset.tip;
                if (tip) tooltip.innerHTML = `<span style="color: var(--accent);">🔍</span> ${tip}`;
            });
            area.addEventListener('mouseleave', () => {
                tooltip.innerHTML = '<span style="color: var(--text-muted);">👆 Passe o mouse sobre os elementos do diagrama para ver a explicação</span>';
            });
        });
    }

    // ===== PÁGINA 2 =====
    function initComponentCards() {
        const cards = document.querySelectorAll('.component-card');
        const panel = document.getElementById('component-detail-panel');
        if (!cards.length || !panel) return;
        const details = {
            battery: { title: '🔋 Bateria', desc: 'Fonte de energia de 12V.', test: 'Meça a tensão nos terminais. 12,4-12,8V desligado; 13,8-14,4V ligado.' },
            fuse: { title: '🔌 Fusível', desc: 'Protege o circuito contra sobrecarga.', test: 'Teste de continuidade (bip).' },
            relay: { title: '⚡ Relé', desc: 'Interruptor eletromagnético.', test: 'Resistência da bobina: 50-120Ω. Teste de acionamento.' },
            switch: { title: '🎛️ Interruptor', desc: 'Comando do motorista.', test: 'Continuidade na posição "ligado".' },
            bulb: { title: '💡 Lâmpada', desc: 'Converte energia elétrica em luz.', test: 'Teste direto na bateria.' },
            ground: { title: '⏚ Massa (GND)', desc: 'Caminho de retorno da corrente.', test: 'Queda de tensão < 0,2V com farol aceso.' }
        };
        cards.forEach(card => {
            card.addEventListener('click', () => {
                const comp = card.dataset.component;
                const data = details[comp];
                if (data) panel.innerHTML = `<div style="font-weight: 600; margin-bottom: 8px; color: var(--accent);">${data.title}</div><div style="margin-bottom: 8px;">${data.desc}</div><div style="font-size: 13px; color: var(--text-muted);"><strong>🔍 Como testar:</strong> ${data.test}</div>`;
            });
        });
    }

    function initDiagnosticSim() {
        const buttons = document.querySelectorAll('#diag-scenario-selector .scenario-btn');
        const resultArea = document.getElementById('diag-result-area');
        if (!buttons.length || !resultArea) return;
        const scenarios = {
            'one-side': { title: 'Diagnóstico: Um lado não acende', steps: [{ title: 'Verifique a lâmpada', desc: 'Troque a lâmpada de lado.' }, { title: 'Teste o fusível', desc: 'Fusível individual do lado.' }, { title: 'Meça a tensão no soquete', desc: '12V = problema na massa; 0V = alimentação.' }, { title: 'Verifique a massa', desc: 'Queda de tensão < 0,2V.' }] },
            'both-sides': { title: 'Diagnóstico: Nenhum farol acende', steps: [{ title: 'Fusível principal', desc: 'Teste de continuidade.' }, { title: 'Teste o relé', desc: 'Clique audível.' }, { title: 'Interruptor', desc: 'Continuidade na posição ligado.' }, { title: 'Inspecione o chicote', desc: 'Fios rompidos ou oxidados.' }] },
            'weak': { title: 'Diagnóstico: Farol fraco ou oscilando', steps: [{ title: 'Tensão no soquete', desc: '> 12V.' }, { title: 'Teste de massa', desc: 'Queda < 0,2V.' }, { title: 'Limpe contatos', desc: 'Spray limpa-contato.' }, { title: 'Verifique o relé', desc: 'Contatos desgastados.' }] },
            'high-beam': { title: 'Diagnóstico: Farol alto não funciona', steps: [{ title: 'Teste a lâmpada', desc: 'Direto na bateria.' }, { title: 'Relé do farol alto', desc: 'Localize e teste.' }, { title: 'Comutador', desc: 'Continuidade.' }, { title: 'Alimentação', desc: 'Tensão no pino do farol alto.' }] }
        };
        buttons.forEach(btn => {
            btn.addEventListener('click', () => {
                buttons.forEach(b => b.classList.remove('active'));
                btn.classList.add('active');
                const scenario = btn.dataset.scenario;
                const data = scenarios[scenario];
                if (data) {
                    let html = `<div style="font-weight: 600; margin-bottom: 1rem; color: var(--accent);">🔍 ${data.title}</div>`;
                    data.steps.forEach((step, index) => {
                        html += `<div class="diag-step"><div class="step-badge">${index + 1}</div><div class="step-content"><div class="title">${step.title}</div><div class="desc">${step.desc}</div></div></div>`;
                    });
                    resultArea.innerHTML = html;
                }
            });
        });
    }

    // ===== PÁGINA 3 (XENON) =====
    function initXenonComponentCards() {
        const cards = document.querySelectorAll('.xenon-components-grid .component-card');
        const panel = document.getElementById('xenon-component-detail');
        if (!cards.length || !panel) return;
        const details = {
            'xenon-bulb': { title: '💡 Lâmpada Xenon', desc: 'Cápsula de quartzo com gás xenônio e sais.', test: 'Inspeção visual. Troque de lado para isolar.' },
            'ballast': { title: '⚙️ Reator', desc: 'Converte 12V DC em ~85V AC.', test: 'Troque de lado. Verifique 12V na entrada.' },
            'igniter': { title: '⚡ Ignitor', desc: 'Gera pulso de 20-25kV.', test: '⚠️ NUNCA teste com multímetro comum!' }
        };
        cards.forEach(card => {
            card.addEventListener('click', () => {
                const comp = card.dataset.component;
                const data = details[comp];
                if (data) panel.innerHTML = `<div style="font-weight: 600; margin-bottom: 8px; color: var(--accent);">${data.title}</div><div style="margin-bottom: 8px;">${data.desc}</div><div style="font-size: 13px; color: var(--text-muted);"><strong>🔍 Como testar:</strong> ${data.test}</div>`;
            });
        });
    }

    function initXenonDiagram() {
        const areas = document.querySelectorAll('.xenon-diagram .hover-area');
        const tooltip = document.getElementById('diagram-tooltip');
        if (!areas.length || !tooltip) return;
        areas.forEach(area => {
            area.addEventListener('mouseenter', () => { const tip = area.dataset.tip; if (tip) tooltip.innerHTML = `<span style="color: var(--accent);">🔍</span> ${tip}`; });
            area.addEventListener('mouseleave', () => { tooltip.innerHTML = '<span style="color: var(--text-muted);">👆 Passe o mouse sobre os componentes do diagrama para ver a explicação</span>'; });
        });
    }

    function initKelvinSelector() {
        const buttons = document.querySelectorAll('.kelvin-btn');
        const preview = document.getElementById('kelvin-preview');
        const lumensSpan = document.getElementById('kelvin-lumens');
        const penetrationSpan = document.getElementById('kelvin-penetration');
        const recommendationSpan = document.getElementById('kelvin-recommendation');
        if (!buttons.length) return;
        const kelvinData = {
            '3000': { color: '#FFB347', text: '💡 3000K – Amarelo intenso', lumens: '~2800 lm', penetration: 'Excelente', recommendation: '⚠️ Ideal para neblina', recColor: 'var(--yellow)' },
            '4300': { color: '#FFF5E1', text: '💡 4300K – Branco amarelado (OEM)', lumens: '~3200 lm', penetration: 'Excelente', recommendation: '✅ Melhor equilíbrio', recColor: 'var(--green)' },
            '5000': { color: '#F0F8FF', text: '💡 5000K – Branco puro', lumens: '~3100 lm', penetration: 'Boa', recommendation: '✅ Excelente contraste', recColor: 'var(--green)' },
            '6000': { color: '#E6F0FA', text: '💡 6000K – Branco azulado', lumens: '~2900 lm', penetration: 'Moderada', recommendation: '⚠️ Visual moderno', recColor: 'var(--yellow)' },
            '8000': { color: '#D4E6FF', text: '💡 8000K – Azul intenso', lumens: '~2400 lm', penetration: 'Baixa', recommendation: '❌ Apenas estético', recColor: 'var(--red)' },
            '10000': { color: '#E0D4FF', text: '💡 10000K – Violeta', lumens: '~2000 lm', penetration: 'Muito baixa', recommendation: '❌ Não recomendado', recColor: 'var(--red)' }
        };
        buttons.forEach(btn => {
            btn.addEventListener('click', () => {
                buttons.forEach(b => { b.classList.remove('active'); b.style.border = 'none'; });
                btn.classList.add('active');
                btn.style.border = '2px solid var(--accent)';
                const k = btn.dataset.k;
                const data = kelvinData[k];
                if (data && preview) {
                    preview.style.background = `linear-gradient(135deg, ${data.color}, ${data.color}dd)`;
                    preview.textContent = data.text;
                    if (lumensSpan) lumensSpan.textContent = data.lumens;
                    if (penetrationSpan) penetrationSpan.textContent = data.penetration;
                    if (recommendationSpan) { recommendationSpan.textContent = data.recommendation; recommendationSpan.style.color = data.recColor; }
                }
            });
        });
    }

    function initXenonDiagnosisSim() {
        const select = document.getElementById('xenon-fault-select-sim');
        const resultDiv = document.getElementById('xenon-diagnosis-result-sim');
        if (!select || !resultDiv) return;
        const diagnosisData = {
            'not-turn-on': { title: 'Não acende', steps: ['1. Fusível.', '2. 12V no reator.', '3. Teste outro reator.'] },
            'one-side': { title: 'Um lado não acende', steps: ['1. Troque lâmpada de lado.', '2. Reator/ignitor.', '3. Lâmpada queimada.'] },
            'flicker': { title: 'Pisca e apaga', steps: ['1. Bateria >12,4V.', '2. Teste outro reator.', '3. Troque de lado.'] },
            'color-change': { title: 'Luz fraca/rosada', steps: ['1. Lâmpada envelhecida.', '2. Troque o par.', '3. Reator com defeito.'] },
            'intermittent': { title: 'Acende e apaga', steps: ['1. Conexões.', '2. Superaquecimento.', '3. Fios descascados.'] }
        };
        select.addEventListener('change', function () {
            const fault = this.value;
            const data = diagnosisData[fault];
            if (data) {
                let html = `<div style="font-weight: 600; margin-bottom: 1rem; color: var(--accent);">🔍 ${data.title}</div>`;
                data.steps.forEach(step => { html += `<div style="margin-bottom: 0.5rem;">${step}</div>`; });
                resultDiv.innerHTML = html;
            } else {
                resultDiv.innerHTML = '<div class="result-placeholder">Selecione um sintoma para ver o diagnóstico.</div>';
            }
        });
    }

    // ===== PÁGINA 4 (LED/LASER) =====
    function initLedComponentCards() {
        const cards = document.querySelectorAll('.led-components-grid .component-card');
        const panel = document.getElementById('led-component-detail');
        if (!cards.length || !panel) return;
        const details = {
            'led-chip': { title: '💎 Chip LED', desc: 'Semicondutor que emite luz.', test: 'Teste na função diodo.' },
            'led-driver': { title: '⚙️ Driver', desc: 'Converte 12V em corrente constante.', test: 'Meça a tensão de saída.' },
            'led-cooling': { title: '❄️ Arrefecimento', desc: 'Dissipador e ventoinha.', test: 'Verifique se a ventoinha gira.' },
            'led-lens': { title: '🔍 Lente', desc: 'Direciona o feixe.', test: 'Inspeção visual.' }
        };
        cards.forEach(card => {
            card.addEventListener('click', () => {
                const comp = card.dataset.component;
                const data = details[comp];
                if (data) panel.innerHTML = `<div style="font-weight: 600; margin-bottom: 8px; color: var(--accent);">${data.title}</div><div style="margin-bottom: 8px;">${data.desc}</div><div style="font-size: 13px; color: var(--text-muted);"><strong>🔍 Como testar:</strong> ${data.test}</div>`;
            });
        });
    }

    function initLedDiagnosisSim() {
        const select = document.getElementById('led-fault-select');
        const resultDiv = document.getElementById('led-diagnosis-result');
        if (!select || !resultDiv) return;
        const diagnosisData = {
            'flicker': { title: 'LED piscando', steps: ['1. Decodificador CANbus.', '2. Verifique conexões.', '3. Teste outro LED.'] },
            'not-turn-on': { title: 'LED não acende', steps: ['1. Polaridade.', '2. Tensão no soquete.', '3. Teste direto na bateria.'] },
            'error-panel': { title: 'Erro no painel', steps: ['1. Resistor de carga.', '2. Codificação via scanner.'] },
            'turns-off': { title: 'Apaga após minutos', steps: ['1. Ventoinha.', '2. Superaquecimento.', '3. Módulo de arrefecimento.'] },
            'weak': { title: 'Luminosidade reduzida', steps: ['1. Tensão.', '2. Chip LED.', '3. Limpe a lente.'] }
        };
        select.addEventListener('change', function () {
            const fault = this.value;
            const data = diagnosisData[fault];
            if (data) {
                let html = `<div style="font-weight: 600; margin-bottom: 1rem; color: var(--accent);">🔍 ${data.title}</div>`;
                data.steps.forEach(step => { html += `<div style="margin-bottom: 0.5rem;">${step}</div>`; });
                resultDiv.innerHTML = html;
            } else {
                resultDiv.innerHTML = '<div class="result-placeholder">Selecione um sintoma para ver o diagnóstico.</div>';
            }
        });
    }

    // ===== PÁGINA 5 (DIAGNÓSTICO) =====
    function initDiagnosisSymptomSim() {
        const select = document.getElementById('diag-symptom-select');
        const resultDiv = document.getElementById('diag-symptom-result');
        if (!select || !resultDiv) return;
        const diagnosisData = {
            'dim': { title: '🔦 Farol(es) fraco(s)', steps: ['1. Tensão no soquete.', '2. Queda de tensão na massa.', '3. Lente opaca.', '4. Trocar par > 2 anos.'] },
            'led-flicker': { title: '💡 LED piscando / Erro', steps: ['1. CANbus? Decodificador.', '2. Conexões.', '3. Outro LED.', '4. Codificação.'] },
            'one-side': { title: '🔌 Um lado não acende', steps: ['1. Trocar lâmpada de lado.', '2. Fusível individual.', '3. Tensão no soquete.'] },
            'both-sides': { title: '🚫 Nenhum farol acende', steps: ['1. Fusível principal.', '2. Relé.', '3. Interruptor.', '4. Chicote.'] },
            'blowing-fuse': { title: '💥 Fusível queima', steps: ['1. NUNCA aumente amperagem!', '2. Lâmpada de teste.', '3. Localizar curto.', '4. Inspeção visual.'] }
        };
        select.addEventListener('change', function () {
            const fault = this.value;
            const data = diagnosisData[fault];
            if (data) {
                let html = `<div style="font-weight: 600; margin-bottom: 1rem; color: var(--accent);">🔍 ${data.title}</div>`;
                data.steps.forEach(step => { html += `<div style="margin-bottom: 0.5rem;">${step}</div>`; });
                resultDiv.innerHTML = html;
            } else {
                resultDiv.innerHTML = '<div class="result-placeholder">Selecione um sintoma para ver o diagnóstico.</div>';
            }
        });
    }

    // ===== PÁGINA 6 (REGULAGEM E DRL) =====
    function initAlignmentSimulator() {
        const heightSlider = document.getElementById('sim-height');
        const distanceSlider = document.getElementById('sim-distance');
        const heightVal = document.getElementById('height-val');
        const distanceVal = document.getElementById('distance-val');
        const centerLine = document.getElementById('sim-center-line');
        const cutoffLine = document.getElementById('sim-cutoff-line');
        const cutoffLabel = document.getElementById('sim-cutoff-label');
        const headlight = document.getElementById('sim-headlight');
        const dropSpan = document.getElementById('sim-drop');
        const cutoffHeightSpan = document.getElementById('sim-cutoff-height');

        if (!heightSlider || !distanceSlider) return;

        const mapHeightToY = (h) => 140 - ((h - 50) / 70) * 80;

        function update() {
            const height = parseFloat(heightSlider.value);
            const distance = parseFloat(distanceSlider.value);

            heightVal.textContent = height + ' cm';
            distanceVal.textContent = distance.toFixed(1) + ' m';

            const drop = distance * 1;
            const cutoffHeight = height - drop;

            dropSpan.textContent = drop.toFixed(1) + ' cm';
            cutoffHeightSpan.textContent = cutoffHeight.toFixed(1) + ' cm';

            const centerY = mapHeightToY(height);
            const cutoffY = centerY + (drop * 5);

            if (centerLine) { centerLine.setAttribute('y1', centerY); centerLine.setAttribute('y2', centerY); }
            if (cutoffLine) { cutoffLine.setAttribute('y1', centerY); cutoffLine.setAttribute('y2', cutoffY); }
            if (cutoffLabel) { cutoffLabel.setAttribute('y', cutoffY + 15); }
            if (headlight) { headlight.setAttribute('y', centerY - 10); }
        }

        heightSlider.addEventListener('input', update);
        distanceSlider.addEventListener('input', update);
        update();
    }

    // ===== PÁGINA 7 (ILUMINAÇÃO ADAPTATIVA) =====
    function initAfsComponentCards() {
        const cards = document.querySelectorAll('.afs-components-grid .component-card');
        const panel = document.getElementById('afs-component-detail');
        if (!cards.length || !panel) return;
        const details = {
            'afs-sensors': { title: '📡 Sensores', desc: 'Ângulo de direção, velocidade, altura, câmera.', test: 'Scanner para leituras ao vivo. DTC C1072.' },
            'afs-ecu': { title: '🧠 ECU (Módulo AFS)', desc: 'Processa dados via CAN.', test: 'Alimentação 12V. DTC U112300.' },
            'afs-actuators': { title: '⚙️ Atuadores', desc: 'Motores de passo.', test: 'Acione via scanner. Ruído anormal indica desgaste.' },
            'afs-camera': { title: '📷 Câmera Frontal', desc: 'Detecta veículos e condições.', test: 'Limpe para-brisa. DTC B2430.' }
        };
        cards.forEach(card => {
            card.addEventListener('click', () => {
                const comp = card.dataset.component;
                const data = details[comp];
                if (data) panel.innerHTML = `<div style="font-weight: 600; margin-bottom: 8px; color: var(--accent);">${data.title}</div><div style="margin-bottom: 8px;">${data.desc}</div><div style="font-size: 13px; color: var(--text-muted);"><strong>🔍 Como testar:</strong> ${data.test}</div>`;
            });
        });
    }

    function initAfsModesSelector() {
        const buttons = document.querySelectorAll('.afs-mode-btn');
        const label = document.getElementById('afs-mode-label');
        const desc = document.getElementById('afs-mode-description');
        const beam = document.getElementById('afs-beam');
        const beamLine = document.getElementById('afs-beam-line');
        if (!buttons.length) return;

        const modes = {
            city: { label: 'Modo Cidade: Feixe largo e curto', desc: 'Ativo até 50 km/h. Ilumina uma área ampla para pedestres e cruzamentos.', beam: '80,80 500,50 500,130', lineY: 90 },
            highway: { label: 'Modo Estrada: Feixe longo e focado', desc: 'Ativo acima de 90 km/h. Projeta a luz mais longe para máxima visibilidade.', beam: '80,80 550,60 550,120', lineY: 85 },
            corner: { label: 'Modo Curva: Feixe direcionado', desc: 'Projetor gira até 15° acompanhando o volante. Ilumina o interior da curva.', beam: '80,80 500,40 500,130', lineY: 95 },
            weather: { label: 'Modo Mau Tempo: Feixe ajustado', desc: 'Reduz ofuscamento por reflexo em pista molhada. Pode ativar lavador de faróis.', beam: '80,80 480,60 480,120', lineY: 92 }
        };

        buttons.forEach(btn => {
            btn.addEventListener('click', () => {
                buttons.forEach(b => { b.classList.remove('active'); b.style.background = ''; b.style.color = ''; });
                btn.classList.add('active');
                btn.style.background = 'var(--accent)';
                btn.style.color = 'white';
                const mode = btn.dataset.mode;
                const data = modes[mode];
                if (data && label && desc) {
                    label.textContent = data.label;
                    desc.textContent = data.desc;
                    if (beam) beam.setAttribute('points', data.beam);
                    if (beamLine) beamLine.setAttribute('y1', data.lineY);
                }
            });
        });
    }

    function initMatrixAdvancedTooltip() {
        const areas = document.querySelectorAll('.matrix-advanced-diagram .hover-area-matrix');
        const tooltip = document.getElementById('matrix-advanced-tooltip');
        if (!areas.length || !tooltip) return;
        areas.forEach(area => {
            area.addEventListener('mouseenter', () => { const tip = area.dataset.tip; if (tip) tooltip.innerHTML = `<span style="color: var(--accent);">🔍</span> ${tip}`; });
            area.addEventListener('mouseleave', () => { tooltip.innerHTML = '<span style="color: var(--text-muted);">👆 Passe o mouse sobre os elementos para ver a explicação</span>'; });
        });
    }

    function initAfsDiagnosisSim() {
        const select = document.getElementById('afs-fault-select');
        const resultDiv = document.getElementById('afs-diagnosis-result');
        if (!select || !resultDiv) return;
        const data = {
            'afs-light': { title: 'Luz AFS acesa', steps: ['1. Ler DTCs.', '2. Lâmpadas de curva.', '3. Sensores.', '4. Apagar e testar.'] },
            'no-corner': { title: 'Não acompanha curva', steps: ['1. Sistema ativado?', '2. Testar atuadores.', '3. Alimentação.', '4. Recalibrar sensor.'] },
            'matrix-glare': { title: 'Matrix LED ofuscando', steps: ['1. Limpar câmera.', '2. Alinhamento.', '3. Recalibrar.', '4. Módulo.'] },
            'aim-low': { title: 'Farol baixo', steps: ['1. Sensor de altura.', '2. Regulagem básica.', '3. Recalibrar zero.', '4. Sensor com defeito.'] },
            'no-adapt': { title: 'Sistema não adapta', steps: ['1. Fusível.', '2. Alimentação 12V.', '3. Comunicação CAN.', '4. Modo de segurança.'] }
        };
        select.addEventListener('change', function () {
            const fault = this.value;
            const d = data[fault];
            if (d) {
                let html = `<div style="font-weight: 600; margin-bottom: 1rem; color: var(--accent);">🔍 ${d.title}</div>`;
                d.steps.forEach(s => html += `<div style="margin-bottom: 0.5rem;">${s}</div>`);
                resultDiv.innerHTML = html;
            } else {
                resultDiv.innerHTML = '<div class="result-placeholder">Selecione um sintoma para ver o diagnóstico.</div>';
            }
        });
    }

    // ===== PÁGINA 8 (PROJETO DE CIRCUITO) =====
    function initCircuitDiagramTooltip() {
        const areas = document.querySelectorAll('.circuit-diagram .hover-area-p8');
        const tooltip = document.getElementById('p8-diagram-tooltip');
        if (!areas.length || !tooltip) return;
        areas.forEach(area => {
            area.addEventListener('mouseenter', () => { const tip = area.dataset.tip; if (tip) tooltip.innerHTML = `<span style="color: var(--accent);">🔍</span> ${tip}`; });
            area.addEventListener('mouseleave', () => { tooltip.innerHTML = '<span style="color: var(--text-muted);">👆 Clique nos componentes do diagrama para ver detalhes</span>'; });
        });
    }

    function initWireCalculator() {
        const btn = document.getElementById('calc-wire-btn');
        if (!btn) return;
        btn.addEventListener('click', () => {
            const power = parseFloat(document.getElementById('wire-power').value) || 110;
            const voltage = parseFloat(document.getElementById('wire-voltage').value) || 12;
            const length = parseFloat(document.getElementById('wire-length').value) || 5;
            const dropPercent = parseFloat(document.getElementById('wire-drop-percent').value) || 5;

            const current = power / voltage;
            const maxDropV = voltage * (dropPercent / 100);
            const resistivity = 0.0175;
            const requiredArea = (2 * resistivity * (length / 2) * current) / maxDropV;

            const standardSizes = [0.5, 0.75, 1.0, 1.5, 2.5, 4.0, 6.0, 10.0, 16.0];
            const selected = standardSizes.find(s => s >= requiredArea) || 16.0;

            let awg = '';
            if (selected <= 0.5) awg = '20 AWG';
            else if (selected <= 0.75) awg = '18 AWG';
            else if (selected <= 1.0) awg = '17 AWG';
            else if (selected <= 1.5) awg = '16 AWG';
            else if (selected <= 2.5) awg = '14 AWG';
            else if (selected <= 4.0) awg = '12 AWG';
            else if (selected <= 6.0) awg = '10 AWG';
            else awg = '8 AWG ou maior';

            const fuse = Math.ceil(current * 1.25);
            const fuseStd = [5, 7.5, 10, 15, 20, 25, 30, 40].find(f => f >= fuse) || 40;

            const resultDiv = document.getElementById('wire-result');
            resultDiv.innerHTML = `
                <div class="result-line"><span class="result-label">Corrente calculada:</span><span class="result-value"><strong>${current.toFixed(2)} A</strong></span></div>
                <div class="result-line"><span class="result-label">Queda máxima permitida:</span><span class="result-value">${maxDropV.toFixed(3)} V (${dropPercent}%)</span></div>
                <div class="result-line"><span class="result-label">Área mínima calculada:</span><span class="result-value">${requiredArea.toFixed(2)} mm²</span></div>
                <div class="result-line"><span class="result-label">Bitola recomendada:</span><span class="result-value"><strong>${selected} mm² (${awg})</strong></span></div>
                <div class="result-line"><span class="result-label">Fusível sugerido:</span><span class="result-value"><strong>${fuseStd} A</strong></span></div>
            `;
        });
    }

    // ===== EVENTOS E INICIALIZAÇÃO =====
    function attachEvents() {
        document.querySelectorAll('.nav-item[data-page]').forEach(item => {
            item.addEventListener('click', (e) => { e.preventDefault(); showPage(item.dataset.page); });
        });
        document.querySelectorAll('[data-nav]').forEach(btn => {
            btn.addEventListener('click', () => { if (btn.dataset.nav) showPage(btn.dataset.nav); });
        });
        document.querySelector('[data-action="start"]')?.addEventListener('click', () => showPage('p1'));

        document.getElementById('test-farol-btn')?.addEventListener('click', testFarolCircuit);
        document.getElementById('xenon-fault-select')?.addEventListener('change', updateXenonSim);
        document.getElementById('calc-led-btn')?.addEventListener('click', calcLED);
        document.getElementById('calc-aux-btn')?.addEventListener('click', calcAux);

        if (document.getElementById('page-p1')?.classList.contains('active')) initDiagnosisSimulator();
        if (document.getElementById('page-p2')?.classList.contains('active')) { initComponentCards(); initDiagnosticSim(); }
        if (document.getElementById('page-p3')?.classList.contains('active')) { initXenonComponentCards(); initXenonDiagram(); initKelvinSelector(); initXenonDiagnosisSim(); }
        if (document.getElementById('page-p4')?.classList.contains('active')) { initLedComponentCards(); initLedDiagnosisSim(); initMatrixDiagram(); }
        if (document.getElementById('page-p5')?.classList.contains('active')) initDiagnosisSymptomSim();
        if (document.getElementById('page-p6')?.classList.contains('active')) initAlignmentSimulator();
        if (document.getElementById('page-p7')?.classList.contains('active')) { initAfsComponentCards(); initAfsModesSelector(); initMatrixAdvancedTooltip(); initAfsDiagnosisSim(); }
        if (document.getElementById('page-p8')?.classList.contains('active')) { initCircuitDiagramTooltip(); initWireCalculator(); }

        const themeToggle = document.getElementById('theme-toggle');
        if (themeToggle && typeof themeManager !== 'undefined') {
            themeToggle.addEventListener('click', () => themeManager.toggleTheme());
            themeManager.updateThemeButton();
        }

        const menuToggle = document.getElementById('menu-toggle');
        const sidebar = document.getElementById('sidebar');
        if (menuToggle && sidebar) {
            menuToggle.addEventListener('click', () => sidebar.classList.toggle('open'));
            document.querySelectorAll('.nav-item').forEach(link => {
                link.addEventListener('click', () => { if (window.innerWidth <= 768) sidebar.classList.remove('open'); });
            });
        }
    }

    document.addEventListener('DOMContentLoaded', () => {
        loadProgress();
        attachEvents();
        initQuizzes();
        showPage('home');
    });
})();