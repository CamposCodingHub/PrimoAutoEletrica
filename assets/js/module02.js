/**
 * module02.js - Lógica do Módulo 02 (Ferramentas e Equipamentos)
 */
(function() {
  // ===== PÁGINAS E NAVEGAÇÃO =====
  const pages = ['home', 'p1', 'p2', 'p3', 'p4', 'p5', 'p6', 'p7', 'p8', 'p9'];
  let visitedPages = new Set();

  function loadProgress() {
    const saved = localStorage.getItem('modulo02-visited');
    if (saved) visitedPages = new Set(JSON.parse(saved));
    updateProgressUI();
  }

  function saveProgress() {
    localStorage.setItem('modulo02-visited', JSON.stringify([...visitedPages]));
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
    // Inicializa canvas do osciloscópio se for a página p5
    if (pageId === 'p5') setTimeout(drawScope, 100);
    if (pageId === 'p6') setTimeout(updateBatt, 100);
  }

  // ===== MULTÍMETRO SIMULATOR =====
  const meterData = {
    vdc: { mode: 'TENSÃO CC (VDC)', reading: '12.46', unit: 'V', inst: '<strong>Como medir tensão CC:</strong> Seletor em DCV 20V. Ponta vermelha no positivo, preta na massa. Bateria em repouso: 12,4–12,8V = OK. Alternador ligado: 13,8–14,4V = carregando.' },
    vac: { mode: 'TENSÃO CA (VAC)', reading: '0.00', unit: 'V~', inst: '<strong>Tensão CA no veículo:</strong> Aparece como ripple do alternador. Ripple normal: menor que 50mV CA.' },
    adc: { mode: 'CORRENTE CC (ADC)', reading: '---', unit: 'A', inst: '<strong>⚠️ Atenção na medição de corrente:</strong> Mover ponta vermelha para o jack de 10A. Colocar o multímetro em série com o circuito.' },
    ohm: { mode: 'RESISTÊNCIA (OHM)', reading: '14.2', unit: 'Ω', inst: '<strong>Como medir resistência:</strong> Desligar o circuito completamente. Desconectar o componente. Injetores: 12–16Ω.' },
    cont: { mode: 'CONTINUIDADE ()))', reading: 'BIP', unit: '', inst: '<strong>Teste de continuidade:</strong> Bip = fio contínuo. Sem bip = circuito aberto.' },
    diode: { mode: 'DIODO (⤳)', reading: '0.58', unit: 'V', inst: '<strong>Teste de diodo:</strong> Sentido direto: 0,4–0,7V. Sentido inverso: OL.' },
    hz: { mode: 'FREQUÊNCIA (Hz)', reading: '75.0', unit: 'Hz', inst: '<strong>Medição de frequência:</strong> CKP a 1500 RPM ≈ 100Hz. PWM de ventilador: geralmente 100–1000Hz.' },
    temp: { mode: 'TEMPERATURA (°C)', reading: '82.4', unit: '°C', inst: '<strong>Medição de temperatura:</strong> Requer termopar tipo K. Temperatura normal de um fio em carga: até 70°C.' }
  };

  function setMeter(key, btn) {
    document.querySelectorAll('.sel-btn[data-meter]').forEach(b => b.classList.remove('active'));
    if (btn) btn.classList.add('active');
    const d = meterData[key];
    document.getElementById('m-mode').textContent = d.mode;
    document.getElementById('m-reading').textContent = d.reading;
    document.getElementById('m-unit').textContent = d.unit;
    document.getElementById('m-instruction').innerHTML = d.inst;
  }

  // ===== DTC DECODER =====
  const dtcDB = {
    'P0300': { name: 'Falha de ignição aleatória / múltiplos cilindros', body: 'Verificar velas, bobinas, injetores, compressão.', chips: ['Motor', 'Ignição', 'Injeção'] },
    'P0336': { name: 'CKP — Sinal fora de faixa', body: 'Verificar folga do sensor (0,5–1,5mm), anel fônico, conector.', chips: ['Motor', 'Sensor CKP', 'Osciloscópio'] },
    'P0171': { name: 'Sistema de combustível lean (magro) — banco 1', body: 'Vazamento de admissão, MAF sujo, lambda lento.', chips: ['Motor', 'Lambda', 'Smoke test'] },
    'P0420': { name: 'Eficiência do catalisador abaixo do limite', body: 'Catalisador envelhecido ou sonda lambda traseira com defeito.', chips: ['Motor', 'Catalisador', 'Lambda'] },
    'U0100': { name: 'Comunicação perdida com ECM/PCM', body: 'Barramento CAN com falha, fusível do módulo, aterramento.', chips: ['Rede CAN', 'ECU', 'Comunicação'] },
    'C0035': { name: 'Sensor de velocidade da roda — dianteiro esquerdo', body: 'Verificar conector, folga do sensor (0,5–1mm), anel magnético.', chips: ['ABS', 'Sensor de roda', 'Rolamento'] }
  };

  function decodeDTC() {
    const input = document.getElementById('dtc-in');
    let code = input.value.trim().toUpperCase();
    if (!code) return;
    const entry = dtcDB[code];
    const resultDiv = document.getElementById('dtc-result');
    if (!entry) {
      document.getElementById('dtc-code').textContent = code;
      document.getElementById('dtc-name').textContent = 'Código não encontrado';
      document.getElementById('dtc-body').innerHTML = 'Verifique o código e tente novamente.';
      document.getElementById('dtc-chips').innerHTML = '';
    } else {
      document.getElementById('dtc-code').textContent = code;
      document.getElementById('dtc-name').textContent = entry.name;
      document.getElementById('dtc-body').textContent = entry.body;
      document.getElementById('dtc-chips').innerHTML = entry.chips.map(ch => `<span class="dtc-chip">${ch}</span>`).join('');
    }
    resultDiv.classList.add('show');
  }

  // ===== OSCILOSCÓPIO =====
  let scopeAnimId = null;
  function drawScope() {
    const canvas = document.getElementById('scope-canvas');
    if (!canvas) return;
    if (scopeAnimId) cancelAnimationFrame(scopeAnimId);
    const type = document.getElementById('sig-type').value;
    const rpm = parseInt(document.getElementById('rpm-slider').value) || 1500;
    const descriptions = {
      ckp: 'CKP Hall: sinal digital 0–12V, frequência proporcional ao RPM. Cada dente do anel fônico cria um pulso.',
      tps: 'TPS: sinal analógico DC que varia com a posição do acelerador (0,5V fechado a 4,5V aberto).',
      injector: 'Injetor: pulso de acionamento com pico de indução no desligamento.',
      coil: 'Bobina primário: queda abrupta na abertura → pico de tensão.',
      lambda: 'Lambda banda estreita: oscila entre 0,1V (lean) e 0,9V (rich).',
      pwm: 'PWM: duty cycle variável — controla velocidade do ventilador, intensidade de LED, etc.'
    };
    document.getElementById('scope-info').textContent = descriptions[type] || '';
    function frame() {
      const ctx = canvas.getContext('2d');
      const W = canvas.width, H = canvas.height;
      ctx.fillStyle = '#050a05';
      ctx.fillRect(0, 0, W, H);
      ctx.strokeStyle = 'rgba(0,80,0,0.3)';
      ctx.lineWidth = 0.5;
      for (let x = 0; x <= W; x += W/10) { ctx.beginPath(); ctx.moveTo(x,0); ctx.lineTo(x,H); ctx.stroke(); }
      for (let y = 0; y <= H; y += H/5) { ctx.beginPath(); ctx.moveTo(0,y); ctx.lineTo(W,y); ctx.stroke(); }
      ctx.strokeStyle = '#22C55E';
      ctx.lineWidth = 2;
      ctx.beginPath();
      const mid = H/2;
      const cycles = Math.max(3, Math.floor(rpm / 300));
      if (type === 'ckp') {
        const pw = W / (cycles * 60);
        let x = 0;
        for (let c = 0; c < cycles; c++) {
          for (let t = 0; t < 60; t++) {
            const isMissing = (t === 0);
            const yLow = H - 20, yHigh = 20;
            if (t === 0 && c === 0) ctx.moveTo(x, yLow);
            ctx.lineTo(x, yLow);
            if (!isMissing) {
              ctx.lineTo(x, yHigh);
              ctx.lineTo(x + pw*0.7, yHigh);
              ctx.lineTo(x + pw*0.7, yLow);
            }
            x += pw;
          }
        }
      } else if (type === 'tps') {
        const steps = [0.5, 1.2, 2.0, 3.1, 4.2, 4.5, 3.8, 2.5, 1.0, 0.5];
        const sw = W / steps.length;
        steps.forEach((v, i) => {
          const y = 20 + (H - 40) - (v / 5 * (H - 40));
          if (i === 0) ctx.moveTo(0, y);
          else ctx.bezierCurveTo(i*sw - sw*0.5, y + 5, i*sw - sw*0.5, y, i*sw, y);
        });
      } else if (type === 'injector') {
        const pw = W / cycles;
        let x = 0;
        ctx.moveTo(0, H-20);
        for (let c = 0; c < cycles; c++) {
          ctx.lineTo(x + pw*0.1, H-20);
          ctx.lineTo(x + pw*0.1, 30);
          ctx.lineTo(x + pw*0.35, 30);
          ctx.lineTo(x + pw*0.35, H-20);
          x += pw;
        }
      } else if (type === 'coil') {
        const pw = W / cycles;
        let x = 0;
        ctx.moveTo(0, mid);
        for (let c = 0; c < cycles; c++) {
          ctx.lineTo(x + pw*0.1, mid);
          ctx.lineTo(x + pw*0.1, 25);
          ctx.lineTo(x + pw*0.15, 25);
          ctx.lineTo(x + pw*0.17, H-30);
          ctx.lineTo(x + pw*0.22, 30);
          ctx.lineTo(x + pw*0.28, mid);
          x += pw;
        }
      } else if (type === 'lambda') {
        for (let i = 0; i <= W; i++) {
          const t = i / W * cycles * Math.PI * 2;
          const raw = Math.sin(t * 4);
          const v = raw > 0 ? 0.9 : 0.1;
          const y = 20 + (H - 40) - (v / 1 * (H - 40));
          i === 0 ? ctx.moveTo(i, y) : ctx.lineTo(i, y);
        }
      } else if (type === 'pwm') {
        const dc = 0.3 + (rpm / 10000);
        const pw = W / (cycles * 2);
        let x = 0;
        ctx.moveTo(0, H-20);
        for (let c = 0; c < cycles * 2; c++) {
          const hi = pw * Math.min(0.95, dc);
          ctx.lineTo(x, H-20);
          ctx.lineTo(x, 30);
          ctx.lineTo(x + hi, 30);
          ctx.lineTo(x + hi, H-20);
          x += pw;
        }
      }
      ctx.stroke();
      scopeAnimId = requestAnimationFrame(frame);
    }
    frame();
  }

  // ===== TESTADOR DE BATERIA =====
  function updateBatt() {
    const bv = parseFloat(document.getElementById('bv-slider').value);
    const soh = parseInt(document.getElementById('soh-slider').value);
    const alt = parseFloat(document.getElementById('alt-slider').value);
    const rip = parseInt(document.getElementById('rip-slider').value);
    document.getElementById('bv-val').textContent = bv.toFixed(1);
    document.getElementById('soh-val').textContent = soh;
    document.getElementById('alt-val').textContent = alt.toFixed(1);
    document.getElementById('rip-val').textContent = rip;
    const segs = 10;
    const gauge = document.getElementById('batt-gauge');
    gauge.innerHTML = '';
    for (let i = 0; i < segs; i++) {
      const seg = document.createElement('div');
      seg.className = 'batt-seg';
      const lit = (i / segs) < (soh / 100);
      seg.style.background = lit ? (soh > 70 ? '#10b981' : soh > 40 ? '#f59e0b' : '#ef4444') : 'var(--bg-body)';
      gauge.appendChild(seg);
    }
    let msgs = [];
    if (bv >= 12.4) msgs.push('<span style="color:#10b981">✅ Tensão em repouso OK</span>');
    else if (bv >= 12.0) msgs.push('<span style="color:#f59e0b">⚠️ Bateria parcialmente descarregada</span>');
    else msgs.push('<span style="color:#ef4444">❌ Bateria descarregada ou com defeito</span>');
    if (soh >= 80) msgs.push('<span style="color:#10b981">✅ Saúde da bateria OK (' + soh + '%)</span>');
    else if (soh >= 50) msgs.push('<span style="color:#f59e0b">⚠️ Bateria com saúde reduzida (' + soh + '%) — monitorar</span>');
    else msgs.push('<span style="color:#ef4444">❌ Bateria com falha interna — trocar (' + soh + '%)</span>');
    if (alt >= 13.8 && alt <= 14.4) msgs.push('<span style="color:#10b981">✅ Alternador carregando corretamente</span>');
    else if (alt > 14.4) msgs.push('<span style="color:#ef4444">❌ Tensão alta — regulador com defeito (sobrecarga)</span>');
    else if (alt >= 13.0) msgs.push('<span style="color:#f59e0b">⚠️ Alternador com carga reduzida</span>');
    else msgs.push('<span style="color:#ef4444">❌ Alternador não está carregando</span>');
    if (rip <= 50) msgs.push('<span style="color:#10b981">✅ Ripple do alternador OK (' + rip + 'mV)</span>');
    else msgs.push('<span style="color:#ef4444">❌ Ripple alto (' + rip + 'mV) — diodo do alternador com defeito</span>');
    document.getElementById('batt-status').innerHTML = msgs.join('<br>');
  }

  // ===== QUIZZES =====
  const quizExplain = {
    q1: 'OL significa circuito aberto ou tensão presente. Medir resistência com circuito energizado não funciona e pode danificar o multímetro.',
    q2: 'Nunca use lâmpada incandescente em pinos de sinal ou módulos. O consumo de 2A queima o driver da ECU.',
    q3: '180mA é mais de 3x o limite de 50mA. Com o carro desligado, todos os módulos devem estar em modo de sono.',
    q4: '"U" indica Network. U0100 é "Lost Communication with ECM/PCM". Verificar resistência CAN (deve ser 60Ω).',
    q5: 'Pulsos faltando aleatoriamente indicam anel fônico danificado ou sensor com folga maior que 1,5mm.',
    q6: 'Uma bateria nova deve ter SOH acima de 95%. 45% indica célula morta ou manufatura defeituosa.',
    q7: 'Para verificar referência de 5V de um sensor, use sempre o multímetro em tensão DC. A lâmpada de teste destruiria o driver.',
    q8: 'O smoke tester é a ferramenta ideal para localizar vazamentos no sistema EVAP. A fumaça escapa pelo ponto do vazamento.'
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
      const pageMap = { q1: 'p1', q2: 'p2', q3: 'p3', q4: 'p4', q5: 'p5', q6: 'p6', q7: 'p7', q8: 'p8' };
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

    // Multímetro
    document.querySelectorAll('.sel-btn[data-meter]').forEach(btn => {
      btn.addEventListener('click', () => setMeter(btn.dataset.meter, btn));
    });
    setMeter('vdc', document.querySelector('.sel-btn[data-meter="vdc"]'));

    // DTC Decoder
    document.getElementById('dtc-decode')?.addEventListener('click', decodeDTC);
    document.getElementById('dtc-in')?.addEventListener('keypress', (e) => { if (e.key === 'Enter') decodeDTC(); });
    document.querySelectorAll('.qdtc').forEach(el => {
      el.addEventListener('click', () => {
        document.getElementById('dtc-in').value = el.dataset.dtc;
        decodeDTC();
      });
    });

    // Osciloscópio
    document.getElementById('sig-type')?.addEventListener('change', drawScope);
    document.getElementById('rpm-slider')?.addEventListener('input', drawScope);
    drawScope();

    // Testador de bateria
    ['bv-slider', 'soh-slider', 'alt-slider', 'rip-slider'].forEach(id => {
      document.getElementById(id)?.addEventListener('input', updateBatt);
    });
    updateBatt();

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
    // ===== SIMULADOR DE CORRENTE PARASITA =====
  function updateParasite() {
    const current = parseFloat(document.getElementById('parasite-current').value);
    const ah = parseFloat(document.getElementById('battery-ah').value);
    const resultDiv = document.getElementById('parasite-result');
    if (isNaN(current) || isNaN(ah)) return;
    const hours = (ah * 1000) / current;
    const days = hours / 24;
    let status = '';
    if (current <= 50) status = '<span style="color:#10b981">✅ Consumo normal. Bateria saudável.</span>';
    else if (current <= 80) status = '<span style="color:#f59e0b">⚠️ Consumo elevado. Monitorar.</span>';
    else status = '<span style="color:#ef4444">❌ Corrente parasita detectada! Verifique módulos.</span>';
    resultDiv.innerHTML = `${status}<br>📊 Corrente: ${current} mA | Bateria: ${ah} Ah<br>⏱️ Autonomia aproximada: <strong>${hours.toFixed(1)} horas (${days.toFixed(1)} dias)</strong> até descarga total.`;
  }

  // Adicionar listeners para os novos inputs
  const parasiteCurrent = document.getElementById('parasite-current');
  const batteryAh = document.getElementById('battery-ah');
  if (parasiteCurrent && batteryAh) {
    parasiteCurrent.addEventListener('input', updateParasite);
    batteryAh.addEventListener('input', updateParasite);
    updateParasite();
  }

  // ===== NOVOS QUIZZES =====
  // Quiz diodo (q9a)
  const quizExplainExtra = {
    q9a: 'Diodo em curto conduz nos dois sentidos — leitura 0V em ambos os testes.',
    q9b: '180mA indica corrente parasita. O método do fusível identifica qual circuito está consumindo excesso.'
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
      // Marcar página p9 como visitada ao acertar qualquer quiz extra
      if (!visitedPages.has('p9')) {
        visitedPages.add('p9');
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

  // Registrar os quizzes extras (se existirem)
  const extraQuizIds = ['q9a', 'q9b'];
  extraQuizIds.forEach(qid => {
    const opts = document.getElementById(qid + '-opts');
    if (opts) {
      opts.querySelectorAll('.quiz-option').forEach(opt => {
        opt.addEventListener('click', () => handleExtraQuiz(qid, opt));
      });
    }
  });
})();