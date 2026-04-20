/**
 * module04.js - Lógica do Módulo 04 (Sistema Elétrico) - VERSÃO FINAL
 */
(function() {
    'use strict';

    // ===== PÁGINAS E NAVEGAÇÃO =====
    const pages = ['home', 'p1', 'p2', 'p3', 'p4', 'p5', 'p6', 'p7', 'p8', 'p9', 'p10'];
    let visitedPages = new Set();

    // Variáveis para animações
    let batteryAnimFrame = null;
    let altAnimId = null;
    let vdropAnimFrame = null;
    let vdropSimAnimFrame = null;

    function loadProgress() {
        const saved = localStorage.getItem('modulo04-visited');
        if (saved) visitedPages = new Set(JSON.parse(saved));
        updateProgressUI();
    }

    function saveProgress() {
        localStorage.setItem('modulo04-visited', JSON.stringify([...visitedPages]));
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
        // Cancelar animações ao mudar de página
        if (batteryAnimFrame) cancelAnimationFrame(batteryAnimFrame);
        if (altAnimId) cancelAnimationFrame(altAnimId);
        if (vdropAnimFrame) cancelAnimationFrame(vdropAnimFrame);
        if (vdropSimAnimFrame) cancelAnimationFrame(vdropSimAnimFrame);

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
        if (pageId === 'p2') { updateBatterySim(); drawBatteryGauge(); }
        if (pageId === 'p3') { updateAltSim(); drawAltWaveform(); }
        if (pageId === 'p4') { updateStarterSim(); initDisassemblySteps(); }
        if (pageId === 'p5') initFuseSim();
        if (pageId === 'p6') { updateVDropSim(); drawVDropGauge(); initBrandColorCards(); }
        if (pageId === 'p8') { initBrandCardsPage8(); updateWireCalc(); }
        if (pageId === 'p9') { updateVDropSimulator(); drawVDropSimGauge(); }
        if (pageId === 'p10') { updateParasiteSim(); initFinalQuiz(); }
    }

    // ===== SIMULADOR DE BATERIA =====
    function updateBatterySim() {
        const v = parseFloat(document.getElementById('batt-v').value);
        const cca = parseFloat(document.getElementById('batt-cca').value);
        const ccaNom = parseFloat(document.getElementById('batt-cca-nom').value);
        const age = parseFloat(document.getElementById('batt-age').value);
        const perc = (cca / ccaNom) * 100;

        let status = '';
        let color = '';
        if (v < 12.2) { status = '❌ Tensão baixa: recarregue e teste novamente.'; color = 'var(--red)'; }
        else if (v < 12.6) { status = '⚠️ Tensão parcial: pode estar descarregada.'; color = 'var(--yellow)'; }
        else { status = '✅ Tensão OK.'; color = 'var(--green)'; }

        if (perc < 50) status += ' ❌ CCA muito baixo (' + perc.toFixed(0) + '%) — substituir bateria.';
        else if (perc < 80) status += ' ⚠️ Capacidade reduzida (' + perc.toFixed(0) + '%) — monitorar.';
        else status += ' ✅ CCA dentro do esperado (' + perc.toFixed(0) + '%).';
        if (age > 4) status += ' Bateria com mais de 4 anos — vida útil esgotada.';

        const resultDiv = document.getElementById('batt-status');
        if (resultDiv) resultDiv.innerHTML = `<div style="color:${color}">${status}</div>`;
    }

    let batteryPulse = 0;
    function drawBatteryGauge() {
        const canvas = document.getElementById('batt-gauge-canvas');
        if (!canvas) return;

        const rect = canvas.getBoundingClientRect();
        canvas.width = rect.width;
        canvas.height = rect.height;

        const ctx = canvas.getContext('2d');
        const v = parseFloat(document.getElementById('batt-v').value) || 12.6;
        const percent = Math.min(100, Math.max(0, (v - 10) / 3 * 100));
        const w = canvas.width;
        const h = canvas.height;

        batteryPulse = (batteryPulse + 0.03) % (Math.PI * 2);
        const pulseIntensity = 0.2 + 0.1 * Math.sin(batteryPulse);

        ctx.clearRect(0, 0, w, h);
        ctx.fillStyle = '#1a1a2e';
        ctx.fillRect(0, 0, w, h);

        const fillWidth = (percent / 100) * (w - 20);
        let barColor;
        if (percent < 50) barColor = '#ff6b6b';
        else if (percent < 75) barColor = '#ffd93d';
        else barColor = '#6bcb77';

        ctx.fillStyle = barColor;
        ctx.fillRect(10, 10, fillWidth, 20);
        ctx.strokeStyle = '#ffffff';
        ctx.lineWidth = 1.5;
        ctx.strokeRect(10, 10, w - 20, 20);

        if (fillWidth > 0) {
            ctx.save();
            ctx.globalAlpha = 0.4 * pulseIntensity;
            ctx.fillStyle = '#ffffff';
            ctx.fillRect(10, 10, fillWidth, 20);
            ctx.restore();
        }

        ctx.fillStyle = '#ffffff';
        ctx.font = 'bold 14px "JetBrains Mono", monospace';
        ctx.fillText(v.toFixed(1) + ' V', w / 2 - 20, 45);
        ctx.fillStyle = '#aaaaaa';
        ctx.font = '10px "JetBrains Mono", monospace';
        ctx.fillText('Tensão', 10, 45);

        if (batteryAnimFrame) cancelAnimationFrame(batteryAnimFrame);
        batteryAnimFrame = requestAnimationFrame(() => drawBatteryGauge());
    }

    // ===== SIMULADOR DE ALTERNADOR =====
    function updateAltSim() {
        const v = parseFloat(document.getElementById('alt-v').value);
        const rip = parseFloat(document.getElementById('alt-rip').value);
        let msg = '';
        if (v < 13.2) msg = '❌ Tensão muito baixa (' + v + 'V) — alternador não está carregando.';
        else if (v > 14.6) msg = '❌ Tensão excessiva (' + v + 'V) — regulador com defeito.';
        else msg = '✅ Tensão de carga normal (' + v + 'V).';
        if (rip > 50) msg += ' ⚠️ Ripple CA elevado (' + rip + 'mV) — diodo(s) do alternador com defeito.';
        else msg += ' ✅ Ripple OK.';
        document.getElementById('alt-status').innerHTML = msg;
        drawAltWaveform();
    }

    function drawAltWaveform() {
        const canvas = document.getElementById('alt-wave-canvas');
        if (!canvas) return;
        if (altAnimId) cancelAnimationFrame(altAnimId);

        const rip = parseFloat(document.getElementById('alt-rip').value) || 30;
        let phase = 0;

        function draw() {
            const ctx = canvas.getContext('2d');
            const rect = canvas.getBoundingClientRect();
            canvas.width = rect.width;
            canvas.height = rect.height;
            const w = canvas.width;
            const h = canvas.height;

            ctx.fillStyle = '#050a05';
            ctx.fillRect(0, 0, w, h);
            ctx.beginPath();
            ctx.strokeStyle = '#22C55E';
            ctx.lineWidth = 2;
            const center = h / 2;
            const amplitude = Math.min(center - 10, rip / 200 * center);
            for (let x = 0; x < w; x++) {
                const t = x / w * Math.PI * 4 + phase;
                const y = center - Math.sin(t) * amplitude;
                if (x === 0) ctx.moveTo(x, y);
                else ctx.lineTo(x, y);
            }
            ctx.stroke();
            phase += 0.05;
            altAnimId = requestAnimationFrame(draw);
        }
        draw();
    }

    // ===== SIMULADOR DE SISTEMA DE PARTIDA =====
    function updateStarterSim() {
        const current = parseFloat(document.getElementById('starter-current').value);
        const voltage = parseFloat(document.getElementById('starter-voltage').value);
        let msg = '';
        if (current < 150) msg = '⚠️ Corrente baixa (' + current + 'A) — possível mau contato ou bateria fraca.';
        else if (current > 400) msg = '⚠️ Corrente alta (' + current + 'A) — motor de arranque sobrecarregado ou motor preso.';
        else msg = '✅ Corrente normal (' + current + 'A).';
        if (voltage < 9.5) msg += ' ❌ Queda de tensão excessiva (' + voltage + 'V) — verifique cabos e conexões.';
        else if (voltage < 10.5) msg += ' ⚠️ Queda de tensão moderada (' + voltage + 'V).';
        else msg += ' ✅ Tensão OK durante partida.';
        document.getElementById('starter-status').innerHTML = msg;
    }

    // ===== SIMULADOR DE QUEDA DE TENSÃO (PÁGINA 6 - ATERRAMENTO) =====
    function updateVDropSim() {
        const i = parseFloat(document.getElementById('vdrop-i').value);
        const r = parseFloat(document.getElementById('vdrop-r').value);
        const drop = i * r;
        let msg = `Queda calculada: <strong>${drop.toFixed(3)} V</strong><br>Resistência medida: ${r} Ω`;
        if (drop > 0.2) msg += '<br><span style="color:var(--red)">❌ Queda excessiva! Mau contato de massa. Limpe e aperte o ponto de aterramento.</span>';
        else msg += '<br><span style="color:var(--green)">✅ Queda dentro do limite aceitável.</span>';
        document.getElementById('vdrop-result').innerHTML = msg;
        drawVDropGauge();
    }

    function drawVDropGauge() {
        const canvas = document.getElementById('vdrop-gauge-canvas');
        if (!canvas) return;

        const rect = canvas.getBoundingClientRect();
        canvas.width = rect.width;
        canvas.height = rect.height;

        const ctx = canvas.getContext('2d');
        const i = parseFloat(document.getElementById('vdrop-i').value) || 10;
        const r = parseFloat(document.getElementById('vdrop-r').value) || 0.05;
        const drop = i * r;
        const percent = Math.min(100, (drop / 1.0) * 100);

        ctx.clearRect(0, 0, canvas.width, canvas.height);
        ctx.fillStyle = '#1a1a2e';
        ctx.fillRect(0, 0, canvas.width, canvas.height);

        const fillWidth = (percent / 100) * (canvas.width - 20);
        let barColor = drop > 0.5 ? '#ff6b6b' : (drop > 0.2 ? '#ffd93d' : '#6bcb77');
        ctx.fillStyle = barColor;
        ctx.fillRect(10, 10, fillWidth, 20);
        ctx.strokeStyle = '#ffffff';
        ctx.strokeRect(10, 10, canvas.width - 20, 20);

        ctx.fillStyle = '#ffffff';
        ctx.font = 'bold 12px "JetBrains Mono", monospace';
        ctx.fillText(drop.toFixed(3) + ' V', canvas.width / 2 - 30, 45);
        ctx.fillStyle = '#aaaaaa';
        ctx.font = '10px "JetBrains Mono", monospace';
        ctx.fillText('Queda de tensão', 10, 45);

        if (vdropAnimFrame) cancelAnimationFrame(vdropAnimFrame);
        vdropAnimFrame = requestAnimationFrame(() => drawVDropGauge());
    }

    // ===== SIMULADOR DE QUEDA DE TENSÃO (PÁGINA 9 - COMPLETO) =====
    function updateVDropSimulator() {
        const current = parseFloat(document.getElementById('vdrop-current-sim').value);
        const resistance = parseFloat(document.getElementById('vdrop-resistance-sim').value);
        const drop = current * resistance;
        const voltageAtLoad = 12 - drop;

        document.getElementById('vdrop-current-val').textContent = current.toFixed(1) + ' A';
        document.getElementById('vdrop-res-val').textContent = resistance.toFixed(3) + ' Ω';

        let status = '';
        let color = '';
        if (drop > 0.5) { status = '❌ Queda excessiva! Componente recebe pouca tensão.'; color = 'var(--red)'; }
        else if (drop > 0.2) { status = '⚠️ Queda elevada. Verifique conexões e cabos.'; color = 'var(--yellow)'; }
        else { status = '✅ Queda aceitável. Circuito em boas condições.'; color = 'var(--green)'; }

        const resultDiv = document.getElementById('vdrop-sim-result');
        if (resultDiv) {
            resultDiv.innerHTML = `
                <div class="result-line"><span class="result-label">Queda de tensão calculada:</span><span class="result-value"><strong>${drop.toFixed(3)} V</strong></span></div>
                <div class="result-line"><span class="result-label">Tensão no componente:</span><span class="result-value">${voltageAtLoad.toFixed(1)} V</span></div>
                <div class="result-line"><span class="result-label">Diagnóstico:</span><span class="result-value" style="color:${color}">${status}</span></div>
            `;
        }
        drawVDropSimGauge(drop);
    }

    function drawVDropSimGauge(drop) {
        const canvas = document.getElementById('vdrop-sim-canvas');
        if (!canvas) return;

        const rect = canvas.getBoundingClientRect();
        canvas.width = rect.width;
        canvas.height = rect.height;

        const ctx = canvas.getContext('2d');
        const percent = Math.min(100, (drop / 1.0) * 100);
        const w = canvas.width;
        const h = canvas.height;

        ctx.clearRect(0, 0, w, h);
        ctx.fillStyle = '#1a1a2e';
        ctx.fillRect(0, 0, w, h);

        const fillWidth = (percent / 100) * (w - 20);
        let barColor = drop > 0.5 ? '#ff6b6b' : (drop > 0.2 ? '#ffd93d' : '#6bcb77');
        ctx.fillStyle = barColor;
        ctx.fillRect(10, 10, fillWidth, 20);
        ctx.strokeStyle = '#ffffff';
        ctx.strokeRect(10, 10, w - 20, 20);

        ctx.fillStyle = '#ffffff';
        ctx.font = 'bold 12px "JetBrains Mono", monospace';
        ctx.fillText(drop.toFixed(3) + ' V', w / 2 - 30, 45);
        ctx.fillStyle = '#aaaaaa';
        ctx.font = '10px "JetBrains Mono", monospace';
        ctx.fillText('Queda de tensão', 10, 45);
    }

    // ===== SIMULADOR DE CORRENTE PARASITA (AULA 10 - ATUALIZADO) =====
    function updateParasiteSim() {
        const current = parseFloat(document.getElementById('parasite-current').value);
        const batteryAh = parseFloat(document.getElementById('parasite-battery').value) || 60;
        
        // Atualiza barra de progresso
        const fillPercent = Math.min(100, (current / 200) * 100);
        const fillElem = document.getElementById('parasite-fill');
        const nivelElem = document.getElementById('parasite-consumo-nivel');
        
        if (fillElem) fillElem.style.width = fillPercent + '%';
        
        const hours = (batteryAh * 1000) / current;
        const days = hours / 24;
        
        let status = '';
        let nivel = '';
        let color = '';

        if (current <= 50) {
            status = '✅ Consumo normal. Bateria saudável.';
            nivel = 'Normal';
            color = 'var(--green)';
        } else if (current <= 75) {
            status = '⚠️ Consumo limítrofe. Monitorar.';
            nivel = 'Atenção';
            color = 'var(--yellow)';
        } else {
            status = '❌ Corrente parasita excessiva! Diagnosticar.';
            nivel = 'Crítico';
            color = 'var(--red)';
        }
        
        if (nivelElem) {
            nivelElem.textContent = nivel;
            nivelElem.style.color = color;
        }

        const resultDiv = document.getElementById('parasite-status');
        if (resultDiv) {
            resultDiv.innerHTML = `
                <div style="color:${color}; font-weight: 600; margin-bottom: 0.5rem;">${status}</div>
                <div class="result-line"><span class="result-label">📊 Corrente medida:</span><span class="result-value"><strong>${current} mA</strong></span></div>
                <div class="result-line"><span class="result-label">🔋 Capacidade da bateria:</span><span class="result-value"><strong>${batteryAh} Ah</strong></span></div>
                <div class="result-line"><span class="result-label">⏱️ Autonomia aproximada:</span><span class="result-value"><strong>${hours.toFixed(1)} horas (${days.toFixed(1)} dias)</strong> até descarga total.</span></div>
            `;
        }
    }

    // ===== CALCULADORA DE BITOLA DE FIOS (AULA 8) =====
    function updateWireCalc() {
        const current = parseFloat(document.getElementById('wire-current').value);
        const length = parseFloat(document.getElementById('wire-length').value);
        const dropPercent = parseFloat(document.getElementById('wire-drop').value);
        const voltage = 12;
        const maxDrop = voltage * (dropPercent / 100);

        if (isNaN(current) || isNaN(length) || current <= 0) {
            document.getElementById('wire-result').innerHTML = '<div class="result-placeholder">Preencha os valores acima para calcular</div>';
            return;
        }

        const resistivity = 0.0175;
        const resistanceMax = maxDrop / current;
        const requiredMm2 = (resistivity * 2 * length) / resistanceMax;

        const standardSizes = [0.5, 0.75, 1.0, 1.5, 2.5, 4.0, 6.0, 10.0, 16.0, 25.0, 35.0, 50.0];
        let selectedMm2 = standardSizes.find(s => s >= requiredMm2) || standardSizes[standardSizes.length - 1];

        let awg = '';
        if (selectedMm2 <= 0.5) awg = '20 AWG';
        else if (selectedMm2 <= 0.75) awg = '18 AWG';
        else if (selectedMm2 <= 1.0) awg = '17 AWG';
        else if (selectedMm2 <= 1.5) awg = '16 AWG';
        else if (selectedMm2 <= 2.5) awg = '14 AWG';
        else if (selectedMm2 <= 4.0) awg = '12 AWG';
        else if (selectedMm2 <= 6.0) awg = '10 AWG';
        else if (selectedMm2 <= 10) awg = '8 AWG';
        else if (selectedMm2 <= 16) awg = '6 AWG';
        else if (selectedMm2 <= 25) awg = '4 AWG';
        else awg = '2 AWG ou maior';

        document.getElementById('wire-result').innerHTML = `
            <div class="result-line"><span class="result-label">Corrente:</span><span class="result-value">${current} A</span></div>
            <div class="result-line"><span class="result-label">Comprimento (ida+volta):</span><span class="result-value">${(length * 2).toFixed(1)} m</span></div>
            <div class="result-line"><span class="result-label">Queda máxima permitida:</span><span class="result-value">${maxDrop.toFixed(3)} V (${dropPercent}%)</span></div>
            <div class="result-line"><span class="result-label">Bitola mínima recomendada:</span><span class="result-value"><strong>${selectedMm2} mm² (${awg})</strong></span></div>
            <div class="result-line"><span class="result-label">Resistência máxima do cabo:</span><span class="result-value">${resistanceMax.toFixed(4)} Ω</span></div>
        `;
    }

    // ===== QUIZ FINAL (mantido caso exista na página, mas não interfere) =====
    let finalScore = 0;
    function initFinalQuiz() {
        finalScore = 0;
        document.querySelectorAll('#final-quiz .quiz-option').forEach(opt => {
            opt.style.pointerEvents = 'auto';
            opt.classList.remove('correct', 'wrong');
        });
        const finalScoreElem = document.getElementById('final-score');
        if (finalScoreElem) finalScoreElem.style.display = 'none';
    }

    function checkFinalQuiz() {
        const answers = {
            qf1: 'true', qf2: 'false', qf3: 'true', qf4: 'true', qf5: 'false',
            qf6: 'true', qf7: 'true', qf8: 'false', qf9: 'true', qf10: 'true'
        };
        let correct = 0;
        for (let i = 1; i <= 10; i++) {
            const selected = document.querySelector(`input[name="qf${i}"]:checked`);
            const isCorrect = selected && selected.value === answers[`qf${i}`];
            if (isCorrect) correct++;
            const parent = selected ? selected.closest('.quiz-option') : null;
            if (parent) parent.classList.add(isCorrect ? 'correct' : 'wrong');
        }
        finalScore = correct;
        const finalScoreElem = document.getElementById('final-score');
        if (finalScoreElem) finalScoreElem.style.display = 'block';
        const finalScoreTextElem = document.getElementById('final-score-text');
        if (finalScoreTextElem) {
            finalScoreTextElem.innerHTML = `Você acertou ${correct} de 10 perguntas (${correct * 10}%). ${correct >= 7 ? 'Parabéns! Você está pronto para o próximo módulo.' : 'Revise o conteúdo e tente novamente.'}`;
        }
    }

    // ===== QUIZZES (página individual) =====
    const quizExplain = {
        q1: 'Faróis e a maioria dos acessórios de alta potência recebem energia com a chave na posição ON.',
        q1b: 'O fio amarelo (+BAT) mantém a memória do rádio.',
        q2: 'CCA muito abaixo do nominal indica perda de capacidade.',
        q3: 'Alternador deve fornecer tensão acima de 13,2V mesmo em marcha lenta.',
        q3b: 'Ripple alto causa ruído no sistema de áudio e pode interferir em sensores.',
        q4: 'Um clique único indica solenoide acionado, mas motor não girou.',
        q4b: 'A roda livre do bendix patinando impede o engate da coroa do volante.',
        q5: 'Fusível OK, mas farol não acende: próximo passo é testar tensão no soquete.',
        q5b: 'Relé de 5 pinos: contato 30-87 deve estar aberto com relé desenergizado. Continuidade indica contato soldado.',
        q6: 'Queda de tensão acima de 0,2V indica resistência excessiva no caminho de massa.',
        q6b: 'O teste de queda de tensão com o circuito energizado é o método mais confiável, pois detecta resistência sob carga real.',
        q6c: 'Volkswagen e Audi usam o fio marrom (BR) como aterramento, diferente do padrão preto de outras montadoras.',
        q7: 'Diagramas Ford incluem tabela de conectores (connector views).',
        q8: 'No padrão DIN, o fio vermelho (RD) é o positivo permanente (+BAT).',
        q8b: 'Volkswagen e Audi utilizam o fio marrom (BR) como aterramento, ao contrário do preto usado pela maioria das montadoras.',
        q8c: 'Para 15A contínuos com 3m de distância, 2,5mm² (14 AWG) é a bitola mínima recomendada para queda de tensão aceitável.',
        q8d: 'A cor laranja é padrão internacional para cabos de alta tensão (HV) em veículos elétricos e híbridos, alertando sobre o risco.',
        q8e: 'Falhas intermitentes que aparecem ao movimentar o chicote (como ao abrir a porta) indicam fio rompido internamente ou mau contato no conector daquela região.',
        q9: 'Queda de tensão acima de 0,5V em um circuito de 10A indica resistência excessiva de pelo menos 0,05Ω no caminho.',
        q10: 'O método do fusível identifica o circuito problemático removendo fusíveis um a um até a corrente de repouso cair.',
        q10b: 'O método da queda de tensão nos fusíveis evita "acordar" módulos que entrariam em sleep mode, fornecendo um diagnóstico mais preciso.'
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
            const pageMap = {
                q1: 'p1', q1b: 'p1', q2: 'p2', q3: 'p3', q3b: 'p3',
                q4: 'p4', q4b: 'p4', q5: 'p5', q5b: 'p5',
                q6: 'p6', q6b: 'p6', q6c: 'p6', q7: 'p7',
                q8: 'p8', q8b: 'p8', q8c: 'p8', q8d: 'p8', q8e: 'p8',
                q9: 'p9', q10: 'p10', q10b: 'p10'
            };
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

    // ===== SIMULADOR DE IDENTIFICAÇÃO DE FUSÍVEL =====
    function initFuseSim() {
        const buttons = document.querySelectorAll('[data-component]');
        const resultDiv = document.getElementById('fuse-sim-result');
        if (!resultDiv) return;

        const fuseData = {
            farol: { current: (55 * 2) / 12, fuse: 10, color: 'Vermelho', type: 'ATO/Mini', desc: 'Dois faróis de 55W consomem ~9,2A. Fusível de 10A protege o circuito, com folga para pico inicial.' },
            ventilador: { current: 15, fuse: 25, color: 'Transparente', type: 'Maxi ou JCASE', desc: 'Motores elétricos têm alto pico de partida. Fusível de 25A suporta o inrush sem queimar.' },
            bomba: { current: 8, fuse: 15, color: 'Azul', type: 'Mini/ATO', desc: 'Bomba de combustível consome ~5-8A em regime. Fusível de 15A garante segurança.' },
            som: { current: 300 / 12, fuse: 30, color: 'Verde', type: 'Maxi ou ANL', desc: 'Amplificador de 300W RMS consome até 25A em pico. Fusível de 30A é o recomendado.' },
            'ar-condicionado': { current: 12, fuse: 20, color: 'Amarelo', type: 'Mini/ATO', desc: 'Compressor do A/C consome ~10-12A. Fusível de 20A protege contra picos.' }
        };

        buttons.forEach(btn => {
            const newBtn = btn.cloneNode(true);
            btn.parentNode.replaceChild(newBtn, btn);
            newBtn.addEventListener('click', () => {
                buttons.forEach(b => b.classList.remove('active'));
                newBtn.classList.add('active');
                const comp = newBtn.dataset.component;
                const data = fuseData[comp];
                if (data) {
                    resultDiv.innerHTML = `
                        <div class="result-line"><span class="result-label">Componente:</span><span class="result-value">${newBtn.textContent}</span></div>
                        <div class="result-line"><span class="result-label">Corrente calculada:</span><span class="result-value">${data.current.toFixed(1)} A</span></div>
                        <div class="result-line"><span class="result-label">Fusível recomendado:</span><span class="result-value"><strong>${data.fuse} A (${data.color})</strong></span></div>
                        <div class="result-line"><span class="result-label">Tipo sugerido:</span><span class="result-value">${data.type}</span></div>
                        <div class="result-line"><span class="result-label">Observação:</span><span class="result-value">${data.desc}</span></div>
                    `;
                }
            });
        });
    }

    function checkFuse() {
        const selectedFuse = document.querySelector('.fuse-item.selected');
        if (!selectedFuse) {
            document.getElementById('fuse-feedback').innerHTML = '⚠️ Selecione um fusível primeiro.';
            return;
        }
        const fuseId = selectedFuse.dataset.fuse;
        const correct = (fuseId === 'farol');
        if (correct) {
            document.getElementById('fuse-feedback').innerHTML = '✅ Correto! O fusível do farol direito estava queimado. Substitua por um de 10A e verifique se há curto no circuito.';
        } else {
            document.getElementById('fuse-feedback').innerHTML = '❌ Incorreto. O fusível do farol direito é o de 10A (vermelho). Tente novamente.';
        }
    }

    // ===== GUIA DE DESMONTAGEM INTERATIVO =====
    function initDisassemblySteps() {
        const stepCards = document.querySelectorAll('.step-card');
        const totalSteps = stepCards.length;
        const totalSpan = document.getElementById('steps-total-count');
        const completedSpan = document.getElementById('steps-completed-count');
        const progressFill = document.getElementById('steps-progress-fill');
        const completionMsg = document.getElementById('completion-message');

        if (!stepCards.length) return;
        if (totalSpan) totalSpan.textContent = totalSteps;

        const savedState = localStorage.getItem('starter-disassembly-steps');
        const completedSteps = savedState ? JSON.parse(savedState) : [];

        function updateProgress() {
            const completedCount = Array.from(stepCards).filter(card => card.classList.contains('completed')).length;
            if (completedSpan) completedSpan.textContent = completedCount;
            if (progressFill) progressFill.style.width = (completedCount / totalSteps) * 100 + '%';
            if (completionMsg) completionMsg.style.display = completedCount === totalSteps ? 'block' : 'none';
            const currentCompleted = Array.from(stepCards).map(card => card.classList.contains('completed'));
            localStorage.setItem('starter-disassembly-steps', JSON.stringify(currentCompleted));
        }

        stepCards.forEach((card, index) => {
            if (completedSteps[index] === true) {
                card.classList.add('completed');
                const statusDiv = card.querySelector('.step-status');
                const progressDiv = card.querySelector('.step-progress');
                if (statusDiv) statusDiv.textContent = '✅';
                if (progressDiv) progressDiv.textContent = '✅ Concluído';
            }

            const newCard = card.cloneNode(true);
            card.parentNode.replaceChild(newCard, card);
            newCard.addEventListener('click', () => {
                newCard.classList.toggle('completed');
                const statusDiv = newCard.querySelector('.step-status');
                const progressDiv = newCard.querySelector('.step-progress');
                if (newCard.classList.contains('completed')) {
                    if (statusDiv) statusDiv.textContent = '✅';
                    if (progressDiv) progressDiv.textContent = '✅ Concluído';
                } else {
                    if (statusDiv) statusDiv.textContent = '⭕';
                    if (progressDiv) progressDiv.textContent = '📌 Pendente';
                }
                updateProgress();
            });
        });
        updateProgress();
    }

    // ===== CARDS INTERATIVOS DE MONTADORAS (AULA 6) =====
    function initBrandColorCards() {
        const cards = document.querySelectorAll('.brand-card');
        const detailDiv = document.getElementById('brand-color-detail');
        if (!detailDiv) return;

        const brandData = {
            vw: { title: 'Volkswagen / Audi', colors: '🔴 Vermelho: alimentação contínua (+BAT)<br>⚫ Preto / 🟤 Marrom: aterramento (massa)<br>🟡 Amarelo: positivo chaveado (IGN)<br>🔵 Azul: sinal de controle / comunicação<br>🟢 Verde: sensores (temperatura, nível)<br>⚠️ <strong class="text-accent">Dica:</strong> Em VW, o fio MARROM (BR) é o aterramento, diferente do padrão geral que usa preto.' },
            ford: { title: 'Ford', colors: '🔴 Vermelho: alimentação positiva (alta corrente)<br>⚫ Preto: aterramento (massa)<br>⚫⚪ Preto com listra branca: negativo<br>🟡⚪ Amarelo com listra verde: ligado à ignição<br>🔵 Azul: sistemas de controle eletrônico (ECU, sensores)<br>🟢 Verde: sistemas de ignição ou circuitos de relé.' },
            gm: { title: 'GM (Chevrolet)', colors: '🔴 Vermelho: alimentação constante (+BAT)<br>⚫ Preto: aterramento (massa)<br>🟡 Amarelo: alimentação chaveada (ACC/IGN)<br>🔵 Azul: sinal de sensores<br>🟢 Verde: sinal de atuadores<br>⚠️ <strong class="text-accent">Dica:</strong> Em muitos modelos GM, a cor da massa pode variar entre preto, marrom ou até cinza. Sempre confira o diagrama do modelo específico.' },
            fiat: { title: 'Fiat', colors: '🔴 Vermelho: alimentação positiva (+BAT)<br>⚫ Preto: aterramento (massa)<br>🟡 Amarelo: alimentação chaveada<br>🔵 Azul/Branco: sistemas de controle<br>🟢 Verde: sinal de sensores<br>⚠️ <strong class="text-accent">Dica:</strong> Fiat utiliza padrões semelhantes à Ford em muitos modelos, mas sempre consulte o diagrama elétrico específico para evitar erros.' },
            toyota: { title: 'Toyota / Honda', colors: '🔴 Vermelho: alimentação constante (+BAT)<br>⚫ Preto: aterramento (massa)<br>🟡 Amarelo: alimentação chaveada (IGN)<br>🔵 Azul: sinal de sensores / comunicação<br>🟢 Verde: sinal de sensores de temperatura<br>⚪ Branco: sinal de sensores de posição<br>⚠️ <strong class="text-accent">Dica:</strong> Fabricantes japoneses são altamente padronizados, mas ainda exigem verificação com base no ano e modelo.' }
        };

        cards.forEach(card => {
            card.addEventListener('click', () => {
                cards.forEach(c => c.classList.remove('active'));
                card.classList.add('active');
                const brand = card.dataset.brand;
                const data = brandData[brand];
                if (data) {
                    detailDiv.innerHTML = `
                        <div class="result-line"><span class="result-label">Montadora:</span><span class="result-value">${data.title}</span></div>
                        <div class="result-line"><span class="result-label">Cores de aterramento:</span><span class="result-value">${data.colors}</span></div>
                        <div class="result-line"><span class="result-label">⚠️ Importante:</span><span class="result-value">As cores podem variar com o ano e modelo. Sempre confira o diagrama elétrico específico do veículo antes de qualquer intervenção.</span></div>
                    `;
                }
            });
        });
    }

    // ===== CARDS INTERATIVOS DE MONTADORAS (AULA 8) =====
    function initBrandCardsPage8() {
        const cards = document.querySelectorAll('.brand-card-interactive');
        const detailDiv = document.getElementById('brand-detail-p8');
        if (!detailDiv) return;

        const brandData = {
            vw: { title: 'Volkswagen / Audi', colors: '🔴 Vermelho (RO): alimentação contínua (+BAT)<br>⚫ Preto (SW): aterramento (massa)<br>🟤 Marrom (BR): aterramento (também usado)<br>🟡 Amarelo (GE): positivo chaveado (IGN)<br>🔵 Azul (BL): sinal de controle / comunicação<br>🟢 Verde (GN): sensores (temperatura, nível)<br>⚠️ <strong class="text-accent">Dica:</strong> Em VW, o fio MARROM (BR) é o aterramento, diferente do padrão geral que usa preto.' },
            ford: { title: 'Ford', colors: '🔴 Vermelho (RD): alimentação positiva (alta corrente)<br>⚫ Preto (BK): aterramento (massa)<br>⚫⚪ Preto com listra branca: negativo<br>🟡⚪ Amarelo com listra verde: ligado à ignição<br>🔵 Azul (BU): sistemas de controle eletrônico (ECU, sensores)<br>🟢 Verde (GN): sistemas de ignição ou circuitos de relé.' },
            gm: { title: 'GM (Chevrolet)', colors: '🔴 Vermelho (RED): alimentação constante (+BAT)<br>⚫ Preto (BLK): aterramento (massa)<br>🟡 Amarelo (YEL): alimentação chaveada (ACC/IGN)<br>🔵 Azul (BLU): sinal de sensores<br>🟢 Verde (GRN): sinal de atuadores<br>⚠️ <strong class="text-accent">Dica:</strong> Em muitos modelos GM, a cor da massa pode variar entre preto, marrom ou até cinza. Sempre confira o diagrama do modelo específico.' },
            toyota: { title: 'Toyota / Honda', colors: '🔴 Vermelho (R): alimentação constante (+BAT)<br>⚫ Preto (B): aterramento (massa)<br>🟡 Amarelo (Y): alimentação chaveada (IGN)<br>🔵 Azul (L): sinal de sensores / komunikasi<br>🟢 Verde (G): sinal de sensores de temperatura<br>⚪ Branco (W): sinal de sensores de posição<br>⚠️ <strong class="text-accent">Dica:</strong> Fabricantes japoneses são altamente padronizados, mas ainda exigem verificação com base no ano e modelo.' },
            fiat: { title: 'Fiat', colors: '🔴 Vermelho (R): alimentação positiva (+BAT)<br>⚫ Preto (N): aterramento (massa)<br>🟡 Amarelo (G): alimentação chaveada<br>🔵 Azul (A/L): sistemas de controle<br>🟢 Verde (V): sinal de sensores<br>⚠️ <strong class="text-accent">Dica:</strong> Fiat utiliza padrões semelhantes à Ford em muitos modelos, mas sempre consulte o diagrama elétrico específico para evitar erros.' },
            renault: { title: 'Renault', colors: '🔴 Vermelho (RG): alimentação positiva (+BAT)<br>⚫ Preto (NO): aterramento (massa)<br>🟡 Amarelo (JA): alimentação chaveada<br>🔵 Azul (BE): sistemas de controle<br>🟢 Verde (VE): sinal de sensores<br>⚪ Branco (BA): sinal de sensores<br>⚠️ <strong class="text-accent">Dica:</strong> A Renault utiliza um sistema de numeração de componentes (ex: 147 = sensor de temperatura). Familiarize-se com a lista de componentes do veículo.' }
        };

        cards.forEach(card => {
            card.addEventListener('click', () => {
                cards.forEach(c => c.classList.remove('active'));
                card.classList.add('active');
                const brand = card.dataset.brand;
                const data = brandData[brand];
                if (data) {
                    detailDiv.innerHTML = `
                        <div class="result-line"><span class="result-label">Montadora:</span><span class="result-value">${data.title}</span></div>
                        <div class="result-line"><span class="result-label">Cores e funções:</span><span class="result-value">${data.colors}</span></div>
                        <div class="result-line"><span class="result-label">⚠️ Importante:</span><span class="result-value">As cores podem variar com o ano e modelo. Sempre confira o diagrama elétrico específico do veículo antes de qualquer intervenção.</span></div>
                    `;
                }
            });
        });
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

        // Bateria
        ['batt-v', 'batt-cca', 'batt-cca-nom', 'batt-age'].forEach(id => {
            const el = document.getElementById(id);
            if (el) el.addEventListener('input', updateBatterySim);
        });
        // Alternador
        ['alt-v', 'alt-rip'].forEach(id => {
            const el = document.getElementById(id);
            if (el) el.addEventListener('input', updateAltSim);
        });
        // Partida
        const starterCurrent = document.getElementById('starter-current');
        const starterVoltage = document.getElementById('starter-voltage');
        if (starterCurrent) starterCurrent.addEventListener('input', updateStarterSim);
        if (starterVoltage) starterVoltage.addEventListener('input', updateStarterSim);
        // Queda de tensão (página 6)
        ['vdrop-i', 'vdrop-r'].forEach(id => {
            const el = document.getElementById(id);
            if (el) el.addEventListener('input', updateVDropSim);
        });
        // Corrente parasita (página 10)
        ['parasite-current', 'parasite-battery'].forEach(id => {
            const el = document.getElementById(id);
            if (el) el.addEventListener('input', updateParasiteSim);
        });
        // Calculadora de bitola (Aula 8)
        ['wire-current', 'wire-length', 'wire-drop'].forEach(id => {
            const el = document.getElementById(id);
            if (el) el.addEventListener('input', updateWireCalc);
        });
        // Simulador de queda de tensão (página 9)
        ['vdrop-current-sim', 'vdrop-resistance-sim'].forEach(id => {
            const el = document.getElementById(id);
            if (el) el.addEventListener('input', updateVDropSimulator);
        });
        // Fusível
        const checkFuseBtn = document.getElementById('check-fuse');
        if (checkFuseBtn) checkFuseBtn.addEventListener('click', checkFuse);
        // Quiz final
        const submitFinal = document.getElementById('submit-final');
        if (submitFinal) submitFinal.addEventListener('click', checkFinalQuiz);

        updateBatterySim();
        updateAltSim();
        updateStarterSim();
        updateVDropSim();
        updateParasiteSim();
        updateWireCalc();
        updateVDropSimulator();

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
})();