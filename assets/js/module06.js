/**
 * module06.js - Lógica do Módulo 06 (Sensores Automotivos)
 * Baseado no padrão do Módulo 05, sem destaque visual nos links ativos.
 */
(function () {
    'use strict';

    const pages = ['home', 'p1', 'p2', 'p3', 'p4', 'p5', 'p6', 'p7', 'p8', 'p9', 'p10'];
    let visitedPages = new Set();

    // ===== PROGRESSO =====
    function loadProgress() {
        const saved = localStorage.getItem('modulo06-visited');
        if (saved) visitedPages = new Set(JSON.parse(saved));
        updateProgressUI();
    }

    function saveProgress() {
        localStorage.setItem('modulo06-visited', JSON.stringify([...visitedPages]));
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

    // ===== NAVEGAÇÃO (sem adicionar classe 'active' aos itens da sidebar) =====
    function showPage(pageId) {
        // Oculta todas as páginas
        document.querySelectorAll('.lesson-page').forEach(el => el.classList.remove('active'));
        const target = document.getElementById(`page-${pageId}`);
        if (target) target.classList.add('active');

        // 🔥 REMOVIDO: não adiciona classe 'active' nos itens da sidebar
        // document.querySelectorAll('.nav-item').forEach(item => item.classList.remove('active'));
        // const activeItem = document.querySelector(`.nav-item[data-page="${pageId}"]`);
        // if (activeItem) activeItem.classList.add('active');

        // Rola para o topo
        document.querySelector('.main')?.scrollTo({ top: 0, behavior: 'smooth' });

        // Marca página como visitada (para progresso)
        if (pageId !== 'home' && !visitedPages.has(pageId)) {
            visitedPages.add(pageId);
            saveProgress();
        }

        // Inicializações específicas por página
        if (pageId === 'p1') initCKPSimulator();
        if (pageId === 'p3') initMAPSimulator();
        if (pageId === 'p4') initMAFSimulator();
        if (pageId === 'p6') initNTCSimulator();
    }

    // ===== SIMULADOR CKP (PÁGINA 1) =====
    let ckpAnimFrame = null;

    function initCKPSimulator() {
        const canvas = document.getElementById('ckp-canvas');
        if (!canvas) return;
        if (ckpAnimFrame) cancelAnimationFrame(ckpAnimFrame);

        const typeRadios = document.querySelectorAll('input[name="ckpType"]');
        typeRadios.forEach(radio => radio.addEventListener('change', () => updateCKPSignal()));
        const rpmSlider = document.getElementById('ckp-rpm-slider');
        if (rpmSlider) rpmSlider.addEventListener('input', () => updateCKPSignal());

        updateCKPSignal();
    }

    function updateCKPSignal() {
        const type = document.querySelector('input[name="ckpType"]:checked')?.value || 'indutivo';
        const rpm = parseFloat(document.getElementById('ckp-rpm-slider')?.value || 850);
        const rpmIndicator = document.getElementById('ckp-rpm-indicator');
        if (rpmIndicator) rpmIndicator.innerHTML = `RPM: ${Math.round(rpm)}`;

        const canvas = document.getElementById('ckp-canvas');
        if (!canvas) return;
        if (ckpAnimFrame) cancelAnimationFrame(ckpAnimFrame);

        let phase = 0;
        function draw() {
            const ctx = canvas.getContext('2d');
            const W = canvas.width, H = canvas.height;
            ctx.fillStyle = '#03060a';
            ctx.fillRect(0, 0, W, H);
            // grade
            ctx.strokeStyle = 'rgba(255,255,255,0.05)';
            ctx.lineWidth = 0.5;
            for (let y = 0; y < H; y += H / 4) {
                ctx.beginPath();
                ctx.moveTo(0, y);
                ctx.lineTo(W, y);
                ctx.stroke();
            }
            const freq = rpm / 60;
            const cycles = freq * 0.1;
            ctx.beginPath();
            for (let x = 0; x < W; x++) {
                const t = phase + (x / W) * Math.PI * 8 * cycles;
                let y = H / 2;
                if (type === 'indutivo') {
                    const amp = Math.min(H / 2 - 10, 20 + (rpm / 7000) * (H / 2 - 30));
                    y = H / 2 + Math.sin(t) * amp;
                    ctx.strokeStyle = '#FFB700';
                } else {
                    const val = Math.sin(t) > 0 ? 1 : 0;
                    y = H / 2 + (val === 1 ? -H / 4 : H / 4);
                    ctx.strokeStyle = '#22C55E';
                }
                if (x === 0) ctx.moveTo(x, y);
                else ctx.lineTo(x, y);
            }
            ctx.lineWidth = 2.5;
            ctx.stroke();
            phase += 0.03;
            ckpAnimFrame = requestAnimationFrame(draw);
        }
        draw();

        const descDiv = document.getElementById('ckp-signal-desc');
        if (descDiv) {
            if (type === 'indutivo') {
                descDiv.innerHTML = '<strong>Sinal indutivo (VR):</strong> forma de onda senoidal. A amplitude aumenta com a rotação. Em baixas rotações (abaixo de 200 RPM) o sinal pode ser muito fraco (<0,5V), dificultando a leitura pela ECU.';
            } else {
                descDiv.innerHTML = '<strong>Sinal Hall (ativo):</strong> forma de onda digital (quadrada), amplitude constante (0V a 5V ou 12V). O sinal é robusto mesmo em baixíssimas rotações, permitindo partida rápida e detecção de travamento do motor.';
            }
        }
    }

    function updateCkpCalc() {
        const teeth = parseFloat(document.getElementById('ckp-teeth')?.value);
        const hz = parseFloat(document.getElementById('ckp-hz')?.value);
        const box = document.getElementById('ckp-calc-result');
        if (isNaN(teeth) || isNaN(hz) || teeth <= 0) {
            if (box) box.classList.remove('show');
            return;
        }
        const rpm = (hz * 60) / teeth;
        if (box) {
            box.innerHTML = `<strong>RPM estimado:</strong> ${Math.round(rpm)} RPM<br><small style="color:var(--text-muted)">Considerando 1 pulso por dente. Motores com dente faltante: a frequência medida no osciloscópio é ligeiramente menor, mas a fórmula ainda é válida para RPM médio.</small>`;
            box.classList.add('show');
        }
    }

    // ===== SIMULADOR MAP (PÁGINA 3) =====
    function initMAPSimulator() {
        const input = document.getElementById('map-vin');
        if (input) input.addEventListener('input', updateMapCalc);
        updateMapCalc();
    }

    function updateMapCalc() {
        const v = parseFloat(document.getElementById('map-vin')?.value);
        const box = document.getElementById('map-calc-result');
        if (isNaN(v)) {
            if (box) box.classList.remove('show');
            return;
        }
        const vmin = 0.5, vmax = 4.5, pmin = 20, pmax = 105;
        let p = pmin + (Math.min(Math.max(v, vmin), vmax) - vmin) / (vmax - vmin) * (pmax - pmin);
        const vac = (p - 101.3) < 0 ? Math.round((101.3 - p) * 0.2953) : 0;
        if (box) {
            box.innerHTML = `Modelo <strong>didático</strong> (muitos sensores 1 bar: ~0,5 V → pressão baixa, ~4,5 V → pressão alta):<br>Pressão absoluta estimada: <strong>${p.toFixed(1)} kPa</strong> abs<br>Vácuo no coletor (referência 101,3 kPa): <strong>${vac} inHg</strong> aprox.<br><span style="color:var(--text-muted);font-size:12px">Calibração real varia por part number – use dados do fabricante.</span>`;
            box.classList.add('show');
        }
    }

    // ===== SIMULADOR MAF (PÁGINA 4) =====
    function initMAFSimulator() {
        const input = document.getElementById('maf-v');
        if (input) input.addEventListener('input', updateMafFlow);
        updateMafFlow();
    }

    function updateMafFlow() {
        const v = parseFloat(document.getElementById('maf-v')?.value);
        const box = document.getElementById('maf-flow-result');
        if (isNaN(v)) {
            if (box) box.classList.remove('show');
            return;
        }
        const kg = 0.02 + Math.pow(Math.max(0, v - 1.2), 1.8) * 0.015;
        if (box) {
            box.innerHTML = `Tensão MAF (hot-wire): <strong>${v.toFixed(2)} V</strong><br>Fluxo <strong>ilustrativo</strong> (curva fictícia para estudo): ~<strong>${kg.toFixed(2)} kg/h</strong> – sempre compare com gráfico do fabricante.`;
            box.classList.add('show');
        }
    }

    // ===== SIMULADOR NTC (PÁGINA 6) =====
    function initNTCSimulator() {
        const input = document.getElementById('ntc-t');
        if (input) input.addEventListener('input', updateNtcDemo);
        updateNtcDemo();
    }

    function updateNtcDemo() {
        const t = parseFloat(document.getElementById('ntc-t')?.value);
        const box = document.getElementById('ntc-result');
        if (isNaN(t)) {
            if (box) box.classList.remove('show');
            return;
        }
        const r0 = 2500, t0 = 25, beta = 3500;
        const tk = t + 273.15, tk0 = t0 + 273.15;
        const r = r0 * Math.exp(beta * (1 / tk - 1 / tk0));
        if (box) {
            box.innerHTML = `NTC (modelo beta simplificado, <strong>exemplo</strong>): a <strong>${t.toFixed(0)} °C</strong> a resistência cai vs frio.<br>R estimada (~2,5 kΩ @ 25 °C, β=3500): <strong>${(r / 1000).toFixed(2)} kΩ</strong><br>ECU usa tabela ou lei para converter tensão do divisor em temperatura.`;
            box.classList.add('show');
        }
    }

    // ===== QUIZZES =====
    const quizExplain = {
        q1: 'CKP indutivo gera AC ao girar; Hall/VR com eletrônica costuma entregar sinal quadrangular ao PCM. Sem referência (dente faltante) muitos motores não sabem fase. O diagnóstico correto começa pela verificação mecânica (gap, oxidação) e elétrica (continuidade do chicote) antes de substituir o sensor.',
        q2: 'CMP informa qual volta do 4T; injeção/ignição em sequência dependem de CKP+CMP coerentes.',
        q3: 'MAP mede pressão absoluta no coletor; com carga/RPM a ECU calcula massa de enchimento (junto IAT em speed-density).',
        q4: 'Contaminação do fio quente altera resfriamento → subleitura; limpeza específica ou troca conforme manual.',
        q5: 'TPS é potenciômetro no corpo; APP usa dois sensores redundantes no pedal para segurança.',
        q6: 'NTC: temperatura sobe → resistência cai → tensão no divisor muda; ECT e IAT seguem o mesmo princípio.',
        q7: 'Sonda estreita só indica rico/pobre em torno de λ=1; wideband mede λ real para calibração e estratégias wide open.',
        q8: 'Knock filtra faixa de detonação e atrasa ignição para proteger – falso ruído mecânico pode causar atraso indevido.',
        q9: 'Sensor ABS ativo alimenta e mede; passivo gera sinal ao girar roda; anel/encoder define pulsos por volta.',
        q10: 'Pressão de óleo costuma ser switch ou transdutor; combustível frequentemente transdutor de pressão absoluta ou relativa na linha.'
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
                        const pageMap = { q1: 'p1', q2: 'p2', q3: 'p3', q4: 'p4', q5: 'p5', q6: 'p6', q7: 'p7', q8: 'p8', q9: 'p9', q10: 'p10' };
                        if (pageMap[id] && !visitedPages.has(pageMap[id])) {
                            visitedPages.add(pageMap[id]);
                            saveProgress();
                        }
                    }
                });
            });
        });
    }

    // ===== EVENTOS E INICIALIZAÇÃO =====
    function attachEvents() {
        // Clique nos itens da sidebar – navega sem adicionar classe 'active'
        document.querySelectorAll('.nav-item[data-page]').forEach(item => {
            item.addEventListener('click', (e) => {
                e.preventDefault();
                showPage(item.dataset.page);
            });
        });

        // Botões de navegação "Anterior" e "Próxima"
        document.querySelectorAll('[data-nav]').forEach(btn => {
            btn.addEventListener('click', () => {
                if (btn.dataset.nav) showPage(btn.dataset.nav);
            });
        });

        // Botão "Iniciar Módulo 06"
        document.querySelector('[data-action="start"]')?.addEventListener('click', () => showPage('p1'));

        // Eventos dos simuladores (calculadoras)
        const ckpTeeth = document.getElementById('ckp-teeth');
        const ckpHZ = document.getElementById('ckp-hz');
        if (ckpTeeth) ckpTeeth.addEventListener('input', updateCkpCalc);
        if (ckpHZ) ckpHZ.addEventListener('input', updateCkpCalc);

        const mapVin = document.getElementById('map-vin');
        if (mapVin) mapVin.addEventListener('input', updateMapCalc);
        const mafV = document.getElementById('maf-v');
        if (mafV) mafV.addEventListener('input', updateMafFlow);
        const ntcT = document.getElementById('ntc-t');
        if (ntcT) ntcT.addEventListener('input', updateNtcDemo);

        // Inicializa simuladores da página ativa (caso já esteja em alguma página)
        const activePageId = document.querySelector('.lesson-page.active')?.id?.replace('page-', '');
        if (activePageId === 'p1') initCKPSimulator();
        if (activePageId === 'p3') initMAPSimulator();
        if (activePageId === 'p4') initMAFSimulator();
        if (activePageId === 'p6') initNTCSimulator();
    }

    // ===== INICIALIZAÇÃO GERAL =====
    document.addEventListener('DOMContentLoaded', () => {
        loadProgress();
        attachEvents();
        initQuizzes();
        showPage('home'); // exibe a página inicial
    });
})();