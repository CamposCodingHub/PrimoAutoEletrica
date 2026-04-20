/**
 * module03.js - Lógica do Módulo 03 (Componentes Elétricos Fundamentais)
 */
(function () {
    // ===== PÁGINAS E NAVEGAÇÃO =====
    const pages = ['home', 'p1', 'p2', 'p3', 'p4', 'p5', 'p6', 'p7', 'p8'];
    let visitedPages = new Set();

    function loadProgress() {
        const saved = localStorage.getItem('modulo03-visited');
        if (saved) visitedPages = new Set(JSON.parse(saved));
        updateProgressUI();
    }

    function saveProgress() {
        localStorage.setItem('modulo03-visited', JSON.stringify([...visitedPages]));
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
        // Inicializar simuladores específicos
        if (pageId === 'p2') initRelaySim();
        if (pageId === 'p4') initNTCSim();
        if (pageId === 'p5') initLEDCalc();
    }

    // ===== FUSÍVEIS (seletor) =====
    const fuseData = {
        mini: { name: 'MINI / APM', body: 'Tamanho: 10,9mm × 3,8mm. Faixas: 2A a 30A. Uso: veículos modernos, caixas de fusíveis no habitáculo.' },
        ato: { name: 'ATO / APR / Padrão', body: 'Tamanho: 19,1mm × 5,1mm. Faixas: 1A a 40A. Uso: veículos de passeio, caixas de fusíveis de motor e habitáculo.' },
        maxi: { name: 'MAXI / APX / Alta corrente', body: 'Tamanho: 29mm × 8,5mm. Faixas: 20A a 120A. Uso: circuitos de alta corrente (ventilador, compressor).' },
        glass: { name: 'Vidro / AGC / Cilíndrico', body: 'Corpo cilíndrico de vidro. Raro em veículos modernos, mais comum em instalações de som antigas.' }
    };

    function showFuse(type, el) {
        document.querySelectorAll('.fuse-card').forEach(c => c.classList.remove('selected'));
        if (el) el.classList.add('selected');
        const d = fuseData[type];
        const detailDiv = document.getElementById('fuse-detail');
        if (d) {
            detailDiv.innerHTML = `<strong style="color:var(--accent)">${d.name}</strong><br><br>${d.body}`;
            detailDiv.classList.add('show');
        }
    }

    // ===== RELÉ SIMULADOR =====
    let relayOn = false;
    function initRelaySim() {
        const btn = document.getElementById('relay-toggle-btn');
        if (!btn) return;
        // Remove listener anterior se existir
        const newBtn = btn.cloneNode(true);
        btn.parentNode.replaceChild(newBtn, btn);
        newBtn.addEventListener('click', toggleRelay);
    }

    function toggleRelay() {
        relayOn = !relayOn;
        const btn = document.getElementById('relay-toggle-btn');
        const ind = document.getElementById('relay-ind');
        const statusDiv = document.getElementById('relay-status');
        if (relayOn) {
            btn.classList.add('on');
            ind.style.background = 'var(--accent)';
            statusDiv.innerHTML = '<strong>Pino 87:</strong> com tensão (contato fechado) — carga LIGADA ✅<br><strong>Pino 87A:</strong> sem tensão (contato aberto) — ramo NF inativo';
        } else {
            btn.classList.remove('on');
            ind.style.background = '';
            statusDiv.innerHTML = '<strong>Pino 87:</strong> sem tensão (contato aberto) — carga DESLIGADA<br><strong>Pino 87A:</strong> com tensão (contato fechado) — ramo NF ATIVO';
        }
    }

    // ===== NTC SIMULADOR =====
    function ntcResistance(temp) {
        if (temp <= -20) return 15000;
        if (temp >= 120) return 50;
        const pts = [[-20, 15000], [-10, 9500], [0, 5900], [10, 3800], [20, 2500], [30, 1700], [40, 1200], [50, 850], [60, 610], [70, 450], [80, 340], [90, 260], [100, 200], [110, 155], [120, 120]];
        for (let i = 0; i < pts.length - 1; i++) {
            if (temp >= pts[i][0] && temp <= pts[i + 1][0]) {
                const t = (temp - pts[i][0]) / (pts[i + 1][0] - pts[i][0]);
                return Math.round(pts[i][1] + (pts[i + 1][1] - pts[i][1]) * t);
            }
        }
        return 300;
    }

    function initNTCSim() {
        const slider = document.getElementById('ntc-temp');
        if (!slider) return;
        slider.addEventListener('input', updateNTC);
        updateNTC();
    }

    function updateNTC() {
        const temp = parseInt(document.getElementById('ntc-temp').value);
        document.getElementById('ntc-temp-val').textContent = temp + '°C';
        const r = ntcResistance(temp);
        const pullup = 2200;
        const vin = 5;
        const vout = vin * (r / (r + pullup));
        document.getElementById('ntc-res').textContent = r >= 1000 ? (r / 1000).toFixed(1) + 'k' : r;
        document.getElementById('ntc-v').textContent = vout.toFixed(2);
        let tip = '';
        if (temp < 0) tip = 'ECU espera tensão alta (3,5–4,8V). Se o motor aquecer mas a tensão não cair: sensor NTC aberto.';
        else if (temp < 40) tip = 'Tensão caindo gradualmente. Degraus abruptos indicam falso contato.';
        else if (temp < 90) tip = 'Faixa operacional normal. ECU usa para calcular tempo de injeção.';
        else tip = 'Tensão deve ser baixa (0,3–0,8V). Se estiver alta com motor quente: sensor NTC aberto.';
        document.getElementById('ntc-tip').textContent = tip;
    }

    // ===== DIVISOR DE TENSÃO =====
    function initDivCalc() {
        const inputs = ['dv-vin', 'dv-r1', 'dv-r2'];
        inputs.forEach(id => {
            const el = document.getElementById(id);
            if (el) el.addEventListener('input', calcDiv);
        });
        calcDiv();
    }

    function calcDiv() {
        const vin = parseFloat(document.getElementById('dv-vin').value) || 5;
        const r1 = parseFloat(document.getElementById('dv-r1').value) || 2200;
        const r2 = parseFloat(document.getElementById('dv-r2').value) || 300;
        const vout = vin * (r2 / (r1 + r2));
        const i = (vin / (r1 + r2)) * 1000;
        const resDiv = document.getElementById('div-result');
        if (resDiv) {
            resDiv.innerHTML = `
      <div class="result-line"><span class="result-label">🔻 Tensão de saída (Vout):</span><span class="result-value"><strong>${vout.toFixed(3)} V</strong></span></div>
      <div class="result-line"><span class="result-label">⚡ Corrente no divisor:</span><span class="result-value">${i.toFixed(2)} mA</span></div>
      <div class="result-line"><span class="result-label">📐 Fórmula:</span><span class="result-value">Vout = Vin × (R2 / (R1+R2))</span></div>
    `;
            resDiv.classList.add('show');
        }
    }
    // ===== CALCULADORA DE LED =====
    function initLEDCalc() {
        const inputs = ['led-vs', 'led-vf', 'led-if', 'led-n'];
        inputs.forEach(id => {
            const el = document.getElementById(id);
            if (el) el.addEventListener('input', calcLED);
        });
        calcLED();
    }

    function calcLED() {
        const vs = parseFloat(document.getElementById('led-vs').value) || 12;
        const vf = parseFloat(document.getElementById('led-vf').value) || 2.1;
        const ifma = parseFloat(document.getElementById('led-if').value) || 20;
        const n = parseInt(document.getElementById('led-n').value) || 1;
        const resDiv = document.getElementById('led-result');
        if (!resDiv) return;
        const totalVf = vf * n;
        if (totalVf >= vs) {
            resDiv.innerHTML = `<div class="result-placeholder" style="color:var(--red);">❌ Tensão dos LEDs em série (${totalVf.toFixed(1)}V) é maior ou igual à fonte (${vs}V). Reduza o número de LEDs.</div>`;
            return;
        }
        const rExact = (vs - totalVf) / (ifma / 1000);
        const stdRes = [10, 12, 15, 18, 22, 27, 33, 39, 47, 56, 68, 82, 100, 120, 150, 180, 220, 270, 330, 390, 470, 560, 680, 820, 1000, 1200, 1500, 1800, 2200, 2700, 3300, 3900, 4700, 5600, 6800, 8200, 10000];
        const rStd = stdRes.find(r => r >= rExact) || stdRes[stdRes.length - 1];
        const iActual = ((vs - totalVf) / rStd) * 1000;
        const power = ((vs - totalVf) / rStd) * vs * 1000;
        const wattSuggestion = power < 250 ? '1/4W (250mW)' : (power < 500 ? '1/2W (500mW)' : '1W ou mais');

        resDiv.innerHTML = `
    <div class="result-line"><span class="result-label">⚡ Cálculo exato:</span><span class="result-value">${rExact.toFixed(1)} Ω</span></div>
    <div class="result-line"><span class="result-label">🔧 Resistor padrão comercial:</span><span class="result-value"><strong>${rStd} Ω</strong></span></div>
    <div class="result-line"><span class="result-label">📈 Corrente real:</span><span class="result-value">${iActual.toFixed(1)} mA <span style="color:var(--text-muted);">(limite: ${ifma} mA)</span></span></div>
    <div class="result-line"><span class="result-label">🔥 Potência dissipada:</span><span class="result-value">${power.toFixed(0)} mW → use resistor de <strong>${wattSuggestion}</strong></span></div>
  `;
    }

    // ===== CONECTORES =====
    const connectorData = {
        delphi: { name: 'Delphi Metri-Pack', desc: 'Família de conectores da Aptiv/Delphi. Trava lateral, terminais 150/280/480 series. Presente em GM, Fiat, VW.' },
        amp: { name: 'AMP Junior Power Timer', desc: 'TE Connectivity. Trava superior, terminais robustos. Usado em ECUs, relés, conectores de potência.' },
        molex: { name: 'Molex MX-150', desc: 'Selado, uso em ambiente hostil. Motores elétricos, transmissão, ABS.' },
        deutsch: { name: 'Deutsch DT', desc: 'IP67, múltiplas posições, muito robusto. Caminhões, implementos, offroad.' }
    };

    function showConnector(key, el) {
        document.querySelectorAll('.connector-card').forEach(c => c.classList.remove('active'));
        if (el) el.classList.add('active');
        const d = connectorData[key];
        const detailDiv = document.getElementById('connector-detail');
        if (d) {
            detailDiv.innerHTML = `<strong style="color:var(--accent)">${d.name}</strong><br><br>${d.desc}`;
            detailDiv.classList.add('show');
        }
    }

    // ===== QUIZZES =====
    const quizExplain = {
        q1: 'Fusível que queima repetidamente é sintoma de curto ou sobrecarga no circuito protegido.',
        q2: 'Contato NA (30–87) DEVE ser aberto em repouso. Continuidade sem bobina acionada indica contato soldado.',
        q3: 'Diodo em curto conduz nos dois sentidos. Leitura de 0V em ambos indica junção em curto.',
        q4: '-40°C é o valor de timeout quando o sinal do ECT está em circuito aberto (pull-up em nível alto).',
        q5: 'R = (Vs - n×Vf) / If. Para 3 LEDs brancos em série: (12-9,6)/0,02 = 120Ω → usar 150Ω ou 180Ω padrão.',
        q6: 'Queda de 1,2V em 30cm de fio indica alta resistência no caminho (terminal crimpado frouxo, conector oxidado).',
        q7: 'Sensor de pressão de óleo NF: com pressão adequada deve ABRIR. 0Ω indica contato soldado.'
    };

    function initQuizzes() {
        document.querySelectorAll('.quiz-options').forEach(opts => {
            const id = opts.id ? opts.id.replace('-opts', '') : null;
            if (!id) return;
            opts.querySelectorAll('.quiz-option').forEach(opt => {
                opt.addEventListener('click', () => handleQuiz(id, opt));
            });
        });
    }

    function handleQuiz(id, opt) {
        const opts = document.getElementById(id + '-opts');
        if (opts.dataset.done) return;
        opts.dataset.done = '1';
        opts.querySelectorAll('.quiz-option').forEach(o => o.style.pointerEvents = 'none');
        const correct = opt.dataset.correct === 'true';
        opt.classList.add(correct ? 'correct' : 'wrong');
        const fb = document.getElementById(id + '-fb');
        if (!fb) return;
        if (correct) {
            fb.className = 'quiz-feedback show ok';
            fb.textContent = '✅ Correto! ' + (quizExplain[id] || '');
            const pageMap = { q1: 'p1', q2: 'p2', q3: 'p3', q4: 'p4', q5: 'p5', q6: 'p6', q7: 'p7' };
            if (pageMap[id] && !visitedPages.has(pageMap[id])) {
                visitedPages.add(pageMap[id]);
                saveProgress();
            }
        } else {
            fb.className = 'quiz-feedback show fail';
            fb.textContent = '❌ Não é essa. ' + (quizExplain[id] || '');
            opts.querySelectorAll('.quiz-option').forEach(o => {
                if (o.dataset.correct === 'true') o.classList.add('correct');
            });
        }
    }

    // ===== EVENTOS E INICIALIZAÇÃO =====
    function attachEventListeners() {
        document.querySelectorAll('.nav-item[data-page]').forEach(item => {
            item.addEventListener('click', (e) => {
                e.preventDefault();
                const page = item.dataset.page;
                if (page) showPage(page);
            });
        });
        document.querySelectorAll('[data-nav]').forEach(btn => {
            btn.addEventListener('click', () => {
                const target = btn.dataset.nav;
                if (target) showPage(target);
            });
        });
        const startBtn = document.querySelector('[data-action="start"]');
        if (startBtn) startBtn.addEventListener('click', () => showPage('p1'));

        // Fusíveis
        document.querySelectorAll('.fuse-card').forEach(card => {
            card.addEventListener('click', () => showFuse(card.dataset.fuse, card));
        });

        // Relé (inicializado quando a página p2 for ativada)
        initRelaySim();

        // Divisor de tensão
        initDivCalc();

        // NTC
        initNTCSim();

        // LED
        initLEDCalc();

        // Conectores
        document.querySelectorAll('.connector-card').forEach(card => {
            card.addEventListener('click', () => showConnector(card.dataset.conn, card));
        });

        // Tema e mobile
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
                link.addEventListener('click', () => {
                    if (window.innerWidth <= 768) sidebar.classList.remove('open');
                });
            });
        }
    }

    document.addEventListener('DOMContentLoaded', () => {
        loadProgress();
        attachEventListeners();
        initQuizzes();
        showPage('home');
    });
    // ===== DECODIFICADOR SMD =====
    function decodeSMD() {
        const code = document.getElementById('smd-code').value.trim().toUpperCase();
        const resultField = document.getElementById('smd-result');
        if (!resultField) return;
        if (!code) {
            resultField.value = '';
            return;
        }
        let resistance = '';
        if (code === '0R' || code === '0') {
            resistance = '0 Ω (jumper)';
        } else if (/^\d{3}$/.test(code)) {
            const val = parseInt(code.substring(0, 2));
            const mult = Math.pow(10, parseInt(code[2]));
            resistance = (val * mult) + ' Ω';
            if (val * mult >= 1000) resistance = (val * mult / 1000).toFixed(1) + ' kΩ';
            if (val * mult >= 1000000) resistance = (val * mult / 1000000).toFixed(1) + ' MΩ';
        } else if (/^\d{4}$/.test(code)) {
            const val = parseInt(code.substring(0, 3));
            const mult = Math.pow(10, parseInt(code[3]));
            resistance = (val * mult) + ' Ω';
            if (val * mult >= 1000) resistance = (val * mult / 1000).toFixed(1) + ' kΩ';
            if (val * mult >= 1000000) resistance = (val * mult / 1000000).toFixed(1) + ' MΩ';
        } else {
            resistance = 'Código inválido';
        }
        resultField.value = resistance;
    }

    // Adicionar listener para o campo SMD
    const smdInput = document.getElementById('smd-code');
    if (smdInput) {
        smdInput.addEventListener('input', decodeSMD);
    }

    // ===== NOVOS QUIZZES (página 8) =====
    const quizExplainExtra = {
        q8a: 'Sem o resistor pull-up, a entrada da ECU fica flutuando (estado indefinido) quando o interruptor está aberto, podendo ler valores aleatórios devido a ruídos.',
        q8b: 'O MOSFET moderno tem RDSon de poucos miliohms, dissipando muito menos calor que um BJT em alta corrente, sendo ideal para drivers de injeção e bombas.'
    };

    function handleExtraQuiz(id, opt) {
        const opts = document.getElementById(id + '-opts');
        if (opts.dataset.done) return;
        opts.dataset.done = '1';
        opts.querySelectorAll('.quiz-option').forEach(o => o.style.pointerEvents = 'none');
        const correct = opt.dataset.correct === 'true';
        opt.classList.add(correct ? 'correct' : 'wrong');
        const fb = document.getElementById(id + '-fb');
        if (!fb) return;
        if (correct) {
            fb.className = 'quiz-feedback show ok';
            fb.textContent = '✅ Correto! ' + (quizExplainExtra[id] || '');
            // Marcar página p8 como visitada
            if (!visitedPages.has('p8')) {
                visitedPages.add('p8');
                saveProgress();
            }
        } else {
            fb.className = 'quiz-feedback show fail';
            fb.textContent = '❌ Não é essa. ' + (quizExplainExtra[id] || '');
            opts.querySelectorAll('.quiz-option').forEach(o => {
                if (o.dataset.correct === 'true') o.classList.add('correct');
            });
        }
    }

    // Registrar quizzes da página 8
    const extraQuizIds = ['q8a', 'q8b'];
    extraQuizIds.forEach(qid => {
        const opts = document.getElementById(qid + '-opts');
        if (opts) {
            opts.querySelectorAll('.quiz-option').forEach(opt => {
                opt.addEventListener('click', () => handleExtraQuiz(qid, opt));
            });
        }
    });
})();