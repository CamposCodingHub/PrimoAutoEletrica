/**
 * module01.js - Lógica do Módulo 01 (Fundamentos de Eletricidade)
 * Versão Final Corrigida - Com simulação interativa da Aula 2 e novos quizzes
 */
(function () {
  'use strict';

  // ===== CONFIGURAÇÃO =====
  const PAGES = ['home', 'p1', 'p2', 'p3', 'p4', 'p5', 'p6', 'p7', 'p8', 'p9', 'p10'];
  const STORAGE_KEY = 'modulo01-visited';

  let visitedPages = new Set();

  // ===== PROGRESSO =====
  function loadProgress() {
    try {
      const saved = localStorage.getItem(STORAGE_KEY);
      if (saved) visitedPages = new Set(JSON.parse(saved));
      visitedPages.delete('home');
    } catch (e) {
      console.warn('Erro ao carregar progresso:', e);
    }
    updateProgressUI();
  }

  function saveProgress() {
    try {
      const toSave = [...visitedPages].filter(p => p !== 'home');
      localStorage.setItem(STORAGE_KEY, JSON.stringify(toSave));
    } catch (e) {
      console.warn('Erro ao salvar progresso:', e);
    }
    updateProgressUI();
  }

  function updateProgressUI() {
    const totalLessons = PAGES.filter(p => p !== 'home').length;
    const completedCount = visitedPages.size;
    const percent = Math.round((completedCount / totalLessons) * 100) || 0;

    const progBar = document.getElementById('prog-bar');
    const progText = document.getElementById('prog-pct');
    if (progBar) progBar.style.width = percent + '%';
    if (progText) progText.textContent = percent + '%';

    document.querySelectorAll('.nav-item[data-page]').forEach(item => {
      const page = item.dataset.page;
      if (page && page !== 'home' && visitedPages.has(page)) {
        item.classList.add('done');
      } else {
        item.classList.remove('done');
      }
    });
  }

  // ===== NAVEGAÇÃO =====
  let originalShowPage; // será definida depois

  function showPage(pageId) {
    document.querySelectorAll('.lesson-page').forEach(el => el.classList.remove('active'));
    const targetPage = document.getElementById(`page-${pageId}`);
    if (!targetPage) {
      console.error(`Página com ID "page-${pageId}" não encontrada.`);
      return;
    }
    targetPage.classList.add('active');

    document.querySelectorAll('.nav-item').forEach(item => item.classList.remove('active'));
    const activeMenuItem = document.querySelector(`.nav-item[data-page="${pageId}"]`);
    if (activeMenuItem) activeMenuItem.classList.add('active');

    const mainElement = document.getElementById('main') || document.querySelector('.main');
    if (mainElement) {
      mainElement.scrollTop = 0;
    } else {
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }

    if (pageId !== 'home' && !visitedPages.has(pageId)) {
      visitedPages.add(pageId);
      saveProgress();
    }

    if (pageId === 'p4') {
      if (typeof updateSerie === 'function') updateSerie();
      if (typeof updatePara === 'function') updatePara();
    }

    // Inicializar simulação da Lei de Ohm quando a página p2 for carregada
    if (pageId === 'p2' && typeof updateOhmSim === 'function') {
      setTimeout(updateOhmSim, 50);
    }
  }

  // ===== SIMULAÇÃO INTERATIVA DA LEI DE OHM (AULA 2) =====
  window.updateOhmSim = function () {
    const i = parseFloat(document.getElementById('sim-i')?.value) || 5;
    const r = parseFloat(document.getElementById('sim-r')?.value) || 0.1;

    const iVal = document.getElementById('sim-i-val');
    const rVal = document.getElementById('sim-r-val');
    if (iVal) iVal.textContent = i.toFixed(1) + ' A';
    if (rVal) rVal.textContent = r.toFixed(2) + ' Ω';

    const vDrop = i * r;
    const vLamp = 12.6 - vDrop;

    const vdropSpan = document.getElementById('sim-vdrop');
    const vlampSpan = document.getElementById('sim-vlamp');
    if (vdropSpan) vdropSpan.textContent = vDrop.toFixed(2) + ' V';
    if (vlampSpan) vlampSpan.textContent = vLamp.toFixed(2) + ' V';

    const goodPct = Math.max(0, (vLamp / 12.6) * 100);
    const badPct = Math.min(100, (vDrop / 12.6) * 100);

    const barGood = document.getElementById('sim-bar-good');
    const barBad = document.getElementById('sim-bar-bad');
    if (barGood) barGood.style.width = goodPct + '%';
    if (barBad) barBad.style.width = badPct + '%';

    const diag = document.getElementById('sim-diagnosis');
    if (diag) {
      if (vDrop < 0.2) {
        diag.innerHTML = '<span style="color: var(--green);">✅ Circuito em ótimo estado. Queda de tensão dentro do limite aceitável (<0,2V).</span>';
      } else if (vDrop < 0.5) {
        diag.innerHTML = '<span style="color: var(--yellow);">⚠️ Queda de tensão moderada. Verifique conectores e pontos de massa.</span>';
      } else {
        diag.innerHTML = '<span style="color: var(--red);">❌ Queda de tensão excessiva! Há resistência anormal no circuito. Localize e corrija o mau contato.</span>';
      }
    }
  };

  // ===== CALCULADORA LEI DE OHM =====
  let ohmSolve = 'v';

  function setOhmMode(mode, btn) {
    ohmSolve = mode;
    document.querySelectorAll('.solve-tab[data-ohm]').forEach(tab => tab.classList.remove('active'));
    if (btn) btn.classList.add('active');
    ['v', 'i', 'r'].forEach(x => {
      const field = document.getElementById(`field-${x}`);
      const input = document.getElementById(`ohm-${x}`);
      if (!field || !input) return;
      if (x === mode) {
        field.classList.add('result');
        input.disabled = true;
        input.value = '';
        input.placeholder = '← resultado';
      } else {
        field.classList.remove('result');
        input.disabled = false;
      }
    });
    const resultBox = document.getElementById('ohm-result');
    if (resultBox) resultBox.classList.remove('show');
  }

  function calcOhm() {
    const v = parseFloat(document.getElementById('ohm-v')?.value);
    const i = parseFloat(document.getElementById('ohm-i')?.value);
    const r = parseFloat(document.getElementById('ohm-r')?.value);
    const res = document.getElementById('ohm-result');
    if (!res) return;

    let result;
    if (ohmSolve === 'v') {
      if (isNaN(i) || isNaN(r)) {
        res.innerHTML = '⚠ Preencha I e R.';
        res.classList.add('show');
        return;
      }
      result = i * r;
      document.getElementById('ohm-v').value = result.toFixed(3);
      res.innerHTML = `⚡ Resultado: <strong>${result.toFixed(3)} V</strong>`;
    } else if (ohmSolve === 'i') {
      if (isNaN(v) || isNaN(r) || r === 0) {
        res.innerHTML = '⚠ Preencha V e R (R ≠ 0).';
        res.classList.add('show');
        return;
      }
      result = v / r;
      document.getElementById('ohm-i').value = result.toFixed(4);
      res.innerHTML = `⚡ Resultado: <strong>${result.toFixed(4)} A</strong>`;
    } else {
      if (isNaN(v) || isNaN(i) || i === 0) {
        res.innerHTML = '⚠ Preencha V e I (I ≠ 0).';
        res.classList.add('show');
        return;
      }
      result = v / i;
      document.getElementById('ohm-r').value = result.toFixed(3);
      res.innerHTML = `⚡ Resultado: <strong>${result.toFixed(3)} Ω</strong>`;
    }
    res.classList.add('show');
  }

  // ===== CALCULADORA DE POTÊNCIA =====
  let powSolve = 'p';

  function setPowMode(mode, btn) {
    powSolve = mode;
    document.querySelectorAll('.solve-tab[data-pow]').forEach(tab => tab.classList.remove('active'));
    if (btn) btn.classList.add('active');
    ['p', 'v', 'i'].forEach(x => {
      const field = document.getElementById(`pfield-${x}`);
      const input = document.getElementById(`pow-${x}`);
      if (!field || !input) return;
      if (x === mode) {
        field.classList.add('result');
        input.disabled = true;
        input.value = '';
        input.placeholder = '← resultado';
      } else {
        field.classList.remove('result');
        input.disabled = false;
      }
    });
    const resultBox = document.getElementById('pow-result');
    if (resultBox) resultBox.classList.remove('show');
  }

  function calcPow() {
    const p = parseFloat(document.getElementById('pow-p')?.value);
    const v = parseFloat(document.getElementById('pow-v')?.value);
    const i = parseFloat(document.getElementById('pow-i')?.value);
    const res = document.getElementById('pow-result');
    if (!res) return;

    let result;
    if (powSolve === 'p') {
      if (isNaN(v) || isNaN(i)) {
        res.innerHTML = '⚠ Preencha V e I.';
        res.classList.add('show');
        return;
      }
      result = v * i;
      document.getElementById('pow-p').value = result.toFixed(2);
      res.innerHTML = `⚡ Resultado: <strong>${result.toFixed(2)} W</strong>`;
    } else if (powSolve === 'i') {
      if (isNaN(p) || isNaN(v) || v === 0) {
        res.innerHTML = '⚠ Preencha P e V.';
        res.classList.add('show');
        return;
      }
      result = p / v;
      document.getElementById('pow-i').value = result.toFixed(3);
      const fuseValues = [5, 7.5, 10, 15, 20, 25, 30, 40, 50, 60];
      const suggested = fuseValues.find(f => f >= result * 1.25) || fuseValues[fuseValues.length - 1];
      res.innerHTML = `⚡ Resultado: <strong>${result.toFixed(3)} A</strong> → Fusível sugerido: <strong>${suggested} A</strong>`;
    } else {
      if (isNaN(p) || isNaN(i) || i === 0) {
        res.innerHTML = '⚠ Preencha P e I.';
        res.classList.add('show');
        return;
      }
      result = p / i;
      document.getElementById('pow-v').value = result.toFixed(2);
      res.innerHTML = `⚡ Resultado: <strong>${result.toFixed(2)} V</strong>`;
    }
    res.classList.add('show');
  }

  // ===== DIMENSIONAMENTO DE CABO =====
  function calcJoule() {
    const p = parseFloat(document.getElementById('jw-p')?.value);
    const v = parseFloat(document.getElementById('jw-v')?.value) || 12;
    const res = document.getElementById('joule-result');
    if (!res) return;
    if (isNaN(p) || p <= 0) {
      res.innerHTML = '<span>Preencha a potência acima...</span>';
      res.classList.add('show');
      return;
    }
    const i = p / v;
    const sizes = [
      { mm: 0.5, max: 8, fuse: 7.5, name: '0,5 mm²' },
      { mm: 1.0, max: 13, fuse: 10, name: '1,0 mm²' },
      { mm: 1.5, max: 17, fuse: 15, name: '1,5 mm²' },
      { mm: 2.5, max: 23, fuse: 20, name: '2,5 mm²' },
      { mm: 4.0, max: 32, fuse: 30, name: '4,0 mm²' },
      { mm: 6.0, max: 41, fuse: 40, name: '6,0 mm²' },
      { mm: 10, max: 57, fuse: 50, name: '10 mm²' },
      { mm: 25, max: 100, fuse: 100, name: '25 mm²' }
    ];
    const chosen = sizes.find(s => s.max >= i * 1.25);
    if (!chosen) {
      res.innerHTML = `Corrente: <strong>${i.toFixed(2)} A</strong> — Use cabo especial.`;
      res.classList.add('show');
      return;
    }
    res.innerHTML = `Corrente: <strong>${i.toFixed(2)} A</strong><br>Bitola mínima: <strong>${chosen.name}</strong><br>Fusível indicado: <strong>${chosen.fuse} A</strong>`;
    res.classList.add('show');
  }

  // ===== SIMULADOR DE CIRCUITOS =====
  function setCirc(type, btn) {
    document.querySelectorAll('.circ-tab').forEach(t => t.classList.remove('active'));
    if (btn) btn.classList.add('active');
    document.querySelectorAll('.circ-panel').forEach(p => p.classList.remove('active'));
    const panel = document.getElementById(`cpanel-${type}`);
    if (panel) panel.classList.add('active');
  }

  function updateSerie() {
    const r1 = +document.getElementById('sr1')?.value || 5;
    const r2 = +document.getElementById('sr2')?.value || 10;
    const sv_r1 = document.getElementById('sv-r1');
    const sv_r2 = document.getElementById('sv-r2');
    if (sv_r1) sv_r1.textContent = r1;
    if (sv_r2) sv_r2.textContent = r2;
    const rt = r1 + r2;
    const it = 12 / rt;
    const v1 = it * r1;
    const v2 = it * r2;
    const s_rt = document.getElementById('s-rt');
    const s_it = document.getElementById('s-it');
    const s_vd = document.getElementById('s-vd');
    if (s_rt) s_rt.textContent = rt + ' Ω';
    if (s_it) s_it.textContent = it.toFixed(2) + ' A';
    if (s_vd) s_vd.textContent = v1.toFixed(1) + 'V / ' + v2.toFixed(1) + 'V';
  }

  function updatePara() {
    const r1 = +document.getElementById('pr1')?.value || 4;
    const r2 = +document.getElementById('pr2')?.value || 6;
    const pv_r1 = document.getElementById('pv-r1');
    const pv_r2 = document.getElementById('pv-r2');
    if (pv_r1) pv_r1.textContent = r1;
    if (pv_r2) pv_r2.textContent = r2;
    const rt = (r1 * r2) / (r1 + r2);
    const i1 = 12 / r1;
    const i2 = 12 / r2;
    const it = i1 + i2;
    const p_rt = document.getElementById('p-rt');
    const p_it = document.getElementById('p-it');
    const p_ib = document.getElementById('p-ib');
    if (p_rt) p_rt.textContent = rt.toFixed(2) + ' Ω';
    if (p_it) p_it.textContent = it.toFixed(2) + ' A';
    if (p_ib) p_ib.textContent = i1.toFixed(1) + 'A / ' + i2.toFixed(1) + 'A';
  }

  // ===== SIMULAÇÃO DE EFEITO JOULE E DIMENSIONAMENTO (AULA 5) =====
  window.updateJouleSim = function () {
    const p = parseFloat(document.getElementById('joule-p')?.value) || 110;
    const l = parseFloat(document.getElementById('joule-l')?.value) || 4;
    const v = parseFloat(document.getElementById('joule-v')?.value) || 12;

    const pVal = document.getElementById('joule-p-val');
    const lVal = document.getElementById('joule-l-val');
    if (pVal) pVal.textContent = p + ' W';
    if (lVal) lVal.textContent = l.toFixed(1) + ' m';

    const i = p / v;
    const iCalc = document.getElementById('joule-icalc');
    if (iCalc) iCalc.textContent = i.toFixed(2) + ' A';

    const vDropMax = v * 0.03; // 3% de queda máxima
    const vDropMaxElem = document.getElementById('joule-vdropmax');
    if (vDropMaxElem) vDropMaxElem.textContent = vDropMax.toFixed(2) + ' V';

    const rho = 0.0175; // resistividade do cobre
    const aMin = (2 * rho * l * i) / vDropMax;

    // Determinar bitola comercial
    const sizes = [
      { mm2: 0.5, awg: '20 AWG' },
      { mm2: 0.75, awg: '18 AWG' },
      { mm2: 1.0, awg: '16 AWG' },
      { mm2: 1.5, awg: '14 AWG' },
      { mm2: 2.5, awg: '14 AWG' },
      { mm2: 4.0, awg: '12 AWG' },
      { mm2: 6.0, awg: '10 AWG' },
      { mm2: 10, awg: '8 AWG' },
      { mm2: 16, awg: '6 AWG' },
      { mm2: 25, awg: '4 AWG' },
      { mm2: 35, awg: '2 AWG' }
    ];

    let selected = sizes.find(s => s.mm2 >= aMin) || sizes[sizes.length - 1];

    const awgElem = document.getElementById('joule-awg');
    if (awgElem) awgElem.textContent = selected.mm2 + ' mm² (' + selected.awg + ')';

    // Barra de aquecimento relativo
    const heatBar = document.getElementById('joule-heat-bar');
    if (heatBar) {
      const ratio = Math.min(100, (aMin / selected.mm2) * 100);
      heatBar.style.width = ratio + '%';
    }

    const diag = document.getElementById('joule-diag');
    if (diag) {
      if (aMin < 2.5) {
        diag.innerHTML = '<span style="color: var(--green);">✅ Cabo adequado. A bitola selecionada suporta a corrente com folga. Queda de tensão dentro do limite de 3%.</span>';
      } else if (aMin < 6.0) {
        diag.innerHTML = '<span style="color: var(--yellow);">⚠️ Corrente moderada. Certifique-se de usar terminais adequados e conexões firmes.</span>';
      } else {
        diag.innerHTML = '<span style="color: var(--red);">🔥 Alta corrente! Use cabos de bitola grossa, conectores de qualidade e verifique o aperto regularmente.</span>';
      }
    }
  };

  // Inicializar simulação quando a página p5 for carregada
  const originalShowPage2 = showPage;
  showPage = function (pageId) {
    if (typeof originalShowPage2 === 'function') originalShowPage2(pageId);
    else {
      // fallback (caso a sobrescrita anterior não tenha ocorrido)
      document.querySelectorAll('.lesson-page').forEach(el => el.classList.remove('active'));
      const targetPage = document.getElementById(`page-${pageId}`);
      if (targetPage) targetPage.classList.add('active');
    }
    if (pageId === 'p5' && typeof updateJouleSim === 'function') {
      setTimeout(updateJouleSim, 50);
    }
  };

  // ===== SIMULAÇÃO KVL – LEI DAS MALHAS (AULA 6) =====
  window.updateKVLSim = function () {
    const vBat = parseFloat(document.getElementById('kvl-vbat')?.value) || 12;
    const r1 = parseFloat(document.getElementById('kvl-r1')?.value) || 2.5;
    const r2 = parseFloat(document.getElementById('kvl-r2')?.value) || 0.3;

    document.getElementById('kvl-vbat-val').textContent = vBat.toFixed(1) + ' V';
    document.getElementById('kvl-r1-val').textContent = r1.toFixed(1) + ' Ω';
    document.getElementById('kvl-r2-val').textContent = r2.toFixed(2) + ' Ω';
    document.getElementById('kvl-vmax').textContent = vBat.toFixed(1) + ' V';

    const rTotal = r1 + r2;
    const i = vBat / rTotal;
    const vR1 = i * r1;
    const vR2 = i * r2;

    document.getElementById('kvl-i').textContent = i.toFixed(2) + ' A';
    document.getElementById('kvl-vr1').textContent = vR1.toFixed(2) + ' V';
    document.getElementById('kvl-vr2').textContent = vR2.toFixed(2) + ' V';
    document.getElementById('kvl-vsum').innerHTML = `${vBat.toFixed(2)} V (Verificação KVL: ${vBat.toFixed(1)} = ${vR1.toFixed(2)} + ${vR2.toFixed(2)} ✓)`;

    const goodPct = (vR1 / vBat) * 100;
    const badPct = (vR2 / vBat) * 100;
    document.getElementById('kvl-bar-good').style.width = goodPct + '%';
    document.getElementById('kvl-bar-bad').style.width = badPct + '%';

    const diag = document.getElementById('kvl-diag');
    if (diag) {
      if (r2 < 0.1) {
        diag.innerHTML = '<span style="color: var(--green);">✅ Circuito em ótimo estado. Resistência extra desprezível. Tensão praticamente toda disponível para a carga.</span>';
      } else if (r2 < 0.5) {
        diag.innerHTML = '<span style="color: var(--yellow);">⚠️ Resistência extra moderada. Verifique conectores e pontos de massa. A carga está recebendo menos tensão que o ideal.</span>';
      } else {
        diag.innerHTML = '<span style="color: var(--red);">❌ Resistência extra excessiva! Mau contato grave. A carga está recebendo muito menos tensão que o necessário. Localize e corrija a alta resistência.</span>';
      }
    }
  };

  // ===== SIMULAÇÃO KCL – LEI DOS NÓS (AULA 6) =====
  window.updateKCLSim = function () {
    const vBat = 12.0;
    const r1 = parseFloat(document.getElementById('kcl-r1')?.value) || 3;
    const r2 = parseFloat(document.getElementById('kcl-r2')?.value) || 6;

    document.getElementById('kcl-r1-val').textContent = r1.toFixed(1) + ' Ω';
    document.getElementById('kcl-r2-val').textContent = r2.toFixed(1) + ' Ω';

    const i1 = vBat / r1;
    const i2 = vBat / r2;
    const iTotal = i1 + i2;

    document.getElementById('kcl-itotal').textContent = iTotal.toFixed(2) + ' A';
    document.getElementById('kcl-i1').textContent = i1.toFixed(2) + ' A';
    document.getElementById('kcl-i2').textContent = i2.toFixed(2) + ' A';

    const pct1 = (i1 / iTotal) * 100;
    const pct2 = (i2 / iTotal) * 100;
    document.getElementById('kcl-pct1').textContent = pct1.toFixed(0) + '%';
    document.getElementById('kcl-pct2').textContent = pct2.toFixed(0) + '%';
    document.getElementById('kcl-bar1').style.width = pct1 + '%';
    document.getElementById('kcl-bar2').style.width = pct2 + '%';

    const diag = document.getElementById('kcl-diag');
    if (diag) {
      diag.innerHTML = `✅ KCL verificada: I_total (${iTotal.toFixed(2)} A) = I1 (${i1.toFixed(2)} A) + I2 (${i2.toFixed(2)} A)`;
    }
  };

  // Inicializar simulações quando a página p6 for carregada
  const originalShowPage3 = showPage;
  showPage = function (pageId) {
    if (typeof originalShowPage3 === 'function') originalShowPage3(pageId);
    else {
      document.querySelectorAll('.lesson-page').forEach(el => el.classList.remove('active'));
      const targetPage = document.getElementById(`page-${pageId}`);
      if (targetPage) targetPage.classList.add('active');
    }
    if (pageId === 'p6') {
      if (typeof updateKVLSim === 'function') setTimeout(updateKVLSim, 50);
      if (typeof updateKCLSim === 'function') setTimeout(updateKCLSim, 50);
    }
  };

  // ===== SIMULAÇÃO DE FORÇA DO ELETROÍMÃ (AULA 7) =====
  window.updateMagSim = function () {
    const i = parseFloat(document.getElementById('mag-i')?.value) || 2;
    const n = parseFloat(document.getElementById('mag-n')?.value) || 200;

    document.getElementById('mag-i-val').textContent = i.toFixed(1) + ' A';
    document.getElementById('mag-n-val').textContent = n;

    // Força é proporcional a I * N (ampere-espiras), normalizado para um máximo de 10A * 1000 espiras = 10000
    const maxAmpTurns = 10000;
    const ampTurns = i * n;
    const forcePercent = Math.min(100, (ampTurns / maxAmpTurns) * 100);

    document.getElementById('mag-force').textContent = forcePercent.toFixed(0) + '%';
    document.getElementById('mag-bar').style.width = forcePercent + '%';

    const diag = document.getElementById('mag-diag');
    if (diag) {
      if (forcePercent < 5) {
        diag.innerHTML = '<span style="color: var(--yellow);">⚠️ Força muito fraca. Insuficiente para acionar até mesmo um pequeno relé.</span>';
      } else if (forcePercent < 20) {
        diag.innerHTML = '<span style="color: var(--green);">✅ Força moderada, adequada para um relé típico ou um pequeno solenoide.</span>';
      } else if (forcePercent < 50) {
        diag.innerHTML = '<span style="color: var(--green);">✅ Força forte, capaz de acionar solenoides maiores (ex: válvula de injeção).</span>';
      } else {
        diag.innerHTML = '<span style="color: var(--orange);">🔥 Força muito intensa! Nível de um solenoide de partida ou eletroímã industrial.</span>';
      }
    }
  };

  // Inicializar simulação quando a página p7 for carregada
  const originalShowPage4 = showPage;
  showPage = function (pageId) {
    if (typeof originalShowPage4 === 'function') originalShowPage4(pageId);
    else {
      document.querySelectorAll('.lesson-page').forEach(el => el.classList.remove('active'));
      const targetPage = document.getElementById(`page-${pageId}`);
      if (targetPage) targetPage.classList.add('active');
    }
    if (pageId === 'p7' && typeof updateMagSim === 'function') {
      setTimeout(updateMagSim, 50);
    }
  };

  // ===== SIMULAÇÃO DE CAPACITOR (AULA 8) =====
  let capV = 0;
  let capTargetV = 0;
  let capAnimFrame = null;
  let capData = [];

  window.updateCapSim = function () {
    const c = parseFloat(document.getElementById('cap-c')?.value) || 100; // µF
    const r = parseFloat(document.getElementById('cap-r')?.value) || 1000; // Ω

    document.getElementById('cap-c-val').textContent = c + ' µF';
    document.getElementById('cap-r-val').textContent = r + ' Ω';

    // Recalcular constante de tempo e atualizar diagnóstico
    const tau = (r * c) / 1000000; // em segundos
    const diag = document.getElementById('cap-diag');
    if (diag && capTargetV === 0) {
      diag.innerHTML = `<span style="color: var(--txt3);">Constante de tempo τ = R × C = ${tau.toFixed(3)} s. Após ${tau.toFixed(3)}s, a tensão atinge 63% do valor final. Após ${(5 * tau).toFixed(3)}s, estará >99% carregado.</span>`;
    }
  };

  function drawCapCanvas() {
    const canvas = document.getElementById('cap-canvas');
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    const W = canvas.width, H = canvas.height;

    ctx.fillStyle = '#050a05';
    ctx.fillRect(0, 0, W, H);

    // Desenhar grade
    ctx.strokeStyle = 'rgba(0, 80, 0, 0.2)';
    ctx.lineWidth = 0.5;
    for (let y = 0; y < H; y += H / 4) {
      ctx.beginPath();
      ctx.moveTo(0, y);
      ctx.lineTo(W, y);
      ctx.stroke();
    }

    // Desenhar curva de dados
    if (capData.length > 1) {
      ctx.strokeStyle = '#3B8BFF';
      ctx.lineWidth = 2;
      ctx.shadowColor = '#3B8BFF';
      ctx.shadowBlur = 4;
      ctx.beginPath();

      const step = W / (capData.length - 1);
      capData.forEach((v, i) => {
        const x = i * step;
        const y = H - (v / 12) * H * 0.8 - 10;
        if (i === 0) ctx.moveTo(x, y);
        else ctx.lineTo(x, y);
      });
      ctx.stroke();
      ctx.shadowBlur = 0;
    }

    // Labels
    ctx.fillStyle = '#5a5a68';
    ctx.font = '9px IBM Plex Mono, monospace';
    ctx.fillText('12V', 2, 12);
    ctx.fillText('0V', 2, H - 4);
    ctx.fillText('t (tempo)', W - 40, H - 4);
  }

  function capAnimate() {
    const c = parseFloat(document.getElementById('cap-c')?.value) || 100;
    const r = parseFloat(document.getElementById('cap-r')?.value) || 1000;
    const tau = (r * c) / 1000000;

    const step = 0.05;
    if (Math.abs(capV - capTargetV) > 0.1) {
      if (capTargetV > capV) {
        // Carregando
        capV = capV + (12 - capV) * (1 - Math.exp(-step / tau));
      } else {
        // Descarregando
        capV = capV * Math.exp(-step / tau);
      }

      document.getElementById('cap-v-val').textContent = capV.toFixed(2) + ' V';
      document.getElementById('cap-bar').style.width = (capV / 12 * 100) + '%';

      capData.push(capV);
      if (capData.length > 150) capData.shift();
      drawCapCanvas();

      capAnimFrame = requestAnimationFrame(capAnimate);
    } else {
      const diag = document.getElementById('cap-diag');
      if (diag) {
        diag.innerHTML = `<span style="color: var(--green);">✅ Simulação concluída. Constante de tempo τ = ${tau.toFixed(3)} s. Tensão final: ${capV.toFixed(2)} V.</span>`;
      }
      capAnimFrame = null;
    }
  }

  window.capCharge = function () {
    if (capAnimFrame) cancelAnimationFrame(capAnimFrame);
    capTargetV = 12;
    capData = [capV];
    capAnimFrame = requestAnimationFrame(capAnimate);
    document.getElementById('cap-diag').innerHTML = '<span style="color: var(--blue);">🔌 Carregando... O capacitor se opõe à variação de tensão, carregando gradualmente.</span>';
  };

  window.capDischarge = function () {
    if (capAnimFrame) cancelAnimationFrame(capAnimFrame);
    capTargetV = 0;
    capData = [capV];
    capAnimFrame = requestAnimationFrame(capAnimate);
    document.getElementById('cap-diag').innerHTML = '<span style="color: var(--orange);">⚡ Descarregando... O capacitor libera a energia armazenada, mantendo a tensão por um tempo.</span>';
  };

  window.capReset = function () {
    if (capAnimFrame) cancelAnimationFrame(capAnimFrame);
    capV = 0;
    capTargetV = 0;
    capData = [0];
    document.getElementById('cap-v-val').textContent = '0.0 V';
    document.getElementById('cap-bar').style.width = '0%';
    drawCapCanvas();
    const c = parseFloat(document.getElementById('cap-c')?.value) || 100;
    const r = parseFloat(document.getElementById('cap-r')?.value) || 1000;
    const tau = (r * c) / 1000000;
    document.getElementById('cap-diag').innerHTML = `<span style="color: var(--txt3);">Constante de tempo τ = R × C = ${tau.toFixed(3)} s. Clique em "Carregar" ou "Descarregar".</span>`;
  };

  // Inicializar simulação quando a página p8 for carregada
  const originalShowPage5 = showPage;
  showPage = function (pageId) {
    if (typeof originalShowPage5 === 'function') originalShowPage5(pageId);
    else {
      document.querySelectorAll('.lesson-page').forEach(el => el.classList.remove('active'));
      const targetPage = document.getElementById(`page-${pageId}`);
      if (targetPage) targetPage.classList.add('active');
    }
    if (pageId === 'p8') {
      setTimeout(() => {
        updateCapSim();
        drawCapCanvas();
      }, 50);
    }
  };

  // ===== SIMULAÇÃO DE QUEDA DE TENSÃO (AULA 9) =====
  window.updateVDropSim = function () {
    const rBad = parseFloat(document.getElementById('vdrop-sim-r')?.value) || 0.5;
    const rLoad = 2.6; // resistência fixa do farol (~55W em 12V)
    const vBat = 12.6;

    document.getElementById('vdrop-sim-val').textContent = rBad.toFixed(1) + ' Ω';
    document.getElementById('vdrop-r-val').textContent = rBad.toFixed(1) + ' Ω';

    const rTotal = rBad + rLoad;
    const i = vBat / rTotal;
    const vLoad = i * rLoad;
    const vDrop = i * rBad;

    document.getElementById('vload-sim-display').textContent = vLoad.toFixed(1) + ' V';
    document.getElementById('vdrop-amount').textContent = vDrop.toFixed(1) + ' V';

    const diag = document.getElementById('vdrop-diag');
    if (diag) {
      if (rBad < 0.1) {
        diag.innerHTML = `<span style="color: var(--green);">✅ Circuito em ótimo estado. Queda de tensão de apenas ${vDrop.toFixed(2)} V. O farol recebe ${vLoad.toFixed(1)} V.</span>`;
      } else if (rBad < 0.5) {
        diag.innerHTML = `<span style="color: var(--yellow);">⚠️ Queda de tensão moderada (${vDrop.toFixed(2)} V). O farol recebe ${vLoad.toFixed(1)} V. Verifique conectores e pontos de massa.</span>`;
      } else {
        diag.innerHTML = `<span style="color: var(--red);">❌ Queda de tensão excessiva (${vDrop.toFixed(2)} V)! O farol recebe apenas ${vLoad.toFixed(1)} V. Há um mau contato grave no circuito.</span>`;
      }
    }
  };

  // Inicializar simulação quando a página p9 for carregada
  const originalShowPage6 = showPage;
  showPage = function (pageId) {
    if (typeof originalShowPage6 === 'function') originalShowPage6(pageId);
    else {
      document.querySelectorAll('.lesson-page').forEach(el => el.classList.remove('active'));
      const targetPage = document.getElementById(`page-${pageId}`);
      if (targetPage) targetPage.classList.add('active');
    }
    if (pageId === 'p9' && typeof updateVDropSim === 'function') {
      setTimeout(updateVDropSim, 50);
    }
  };

  // ============================================================
  //  SIMULADOR DE OSCILOSCÓPIO (CANVAS) + INTERATIVIDADE DOS CARDS
  //  (Para a Aula 10)
  // ============================================================
  (function initOscilloscopeSim() {
    // Aguarda o DOM estar pronto e a página p10 visível
    function startSimulator() {
      const canvas = document.getElementById('oscilloscopeCanvas');
      if (!canvas) return;

      const ctx = canvas.getContext('2d');
      let animationId = null;
      let phase = 0;

      const waveSelect = document.getElementById('wave-select');
      const rpmSlider = document.getElementById('rpm_slider');
      const rpmDisplay = document.getElementById('rpm_display');
      const waveDesc = document.getElementById('wave_desc');

      function resizeCanvas() {
        const container = canvas.parentElement;
        const width = container.clientWidth;
        canvas.width = Math.min(width, 800);
        canvas.height = 200;
      }
      window.addEventListener('resize', resizeCanvas);
      resizeCanvas();

      function drawWaveform(type, rpm) {
        if (!ctx) return;
        const w = canvas.width;
        const h = canvas.height;
        if (w === 0) return;

        ctx.clearRect(0, 0, w, h);
        ctx.fillStyle = '#0a0f1a';
        ctx.fillRect(0, 0, w, h);
        ctx.strokeStyle = '#22C55E';
        ctx.lineWidth = 2;
        ctx.beginPath();

        const centerY = h / 2;
        const amplitude = Math.min(centerY - 10, 40 + (rpm / 7000) * 60);
        const cycles = Math.max(2, Math.floor(rpm / 600));

        if (type === 'ckp_indutivo') {
          for (let x = 0; x <= w; x++) {
            const t = phase + (x / w) * Math.PI * 4 * cycles;
            const y = centerY + Math.sin(t) * amplitude;
            if (x === 0) ctx.moveTo(x, y);
            else ctx.lineTo(x, y);
          }
          ctx.stroke();
          if (waveDesc) waveDesc.innerHTML = '<strong>Sinal CKP indutivo:</strong> forma senoidal, amplitude aumenta com a rotação. Em baixas rotações (< 200 RPM) o sinal pode ser muito fraco para a ECU detectar.';
        }
        else if (type === 'ckp_hall') {
          const pulses = Math.max(4, Math.floor(rpm / 150));
          const step = w / pulses;
          for (let i = 0; i < pulses; i++) {
            const x1 = i * step;
            const x2 = (i + 0.45) * step;
            const x3 = (i + 0.55) * step;
            const x4 = (i + 1) * step;
            if (i === 0) ctx.moveTo(x1, centerY - 40);
            ctx.lineTo(x1, centerY - 40);
            ctx.lineTo(x2, centerY - 40);
            ctx.lineTo(x2, centerY + 40);
            ctx.lineTo(x3, centerY + 40);
            ctx.lineTo(x3, centerY - 40);
            ctx.lineTo(x4, centerY - 40);
          }
          ctx.stroke();
          if (waveDesc) waveDesc.innerHTML = '<strong>Sinal CKP Hall:</strong> onda quadrada (0–12V), amplitude constante independente da rotação. Excelente em baixas rotações. Um tempo de subida lento indica mau contato.';
        }
        else if (type === 'injetor') {
          const pulsesPerSec = rpm / 60 / 2;
          const pulseWidthMs = 5 + (rpm / 2000) * 3;
          const periodMs = 1000 / pulsesPerSec;
          const duty = pulseWidthMs / periodMs;
          const steps = 20;
          const stepX = w / steps;
          let x = 0;
          ctx.moveTo(0, centerY + 30);
          for (let i = 0; i < steps; i++) {
            const on = (i % 2 === 0);
            const y = on ? centerY - 40 : centerY + 30;
            ctx.lineTo(x, y);
            x += stepX;
            ctx.lineTo(x, y);
          }
          ctx.stroke();
          if (waveDesc) waveDesc.innerHTML = `<strong>Sinal do injetor:</strong> pulso de comando com largura variável (duty cycle ≈ ${(duty * 100).toFixed(0)}% a ${rpm} RPM). Um bom injetor mostra um pulso nítido, sem picos de indução anormais.`;
        }
        else if (type === 'bobina') {
          const freq = rpm / 60 / 2;
          const periodPx = w / (freq * 0.2);
          ctx.moveTo(0, centerY);
          for (let x = 0; x <= w; x++) {
            const t = (x / w) * Math.PI * 2 * freq * 0.2;
            let y = centerY;
            if (Math.sin(t) > 0.8) y = centerY - 50;
            else if (Math.sin(t) < -0.8) y = centerY + 30;
            else y = centerY;
            ctx.lineTo(x, y);
          }
          ctx.stroke();
          if (waveDesc) waveDesc.innerHTML = '<strong>Sinal da bobina primária:</strong> queda abrupta quando o transistor de potência corta a corrente, seguida de um pico de indução (flyback) de centenas de volts. Picos assimétricos indicam problema na bobina ou no módulo de ignição.';
        }
        else if (type === 'lambda') {
          const freqOsc = 2 + (rpm / 2000);
          for (let x = 0; x <= w; x++) {
            const t = phase + (x / w) * Math.PI * 4 * freqOsc;
            const val = (Math.sin(t) + 1) / 2;
            const y = centerY + (0.4 - val) * 60;
            if (x === 0) ctx.moveTo(x, y);
            else ctx.lineTo(x, y);
          }
          ctx.stroke();
          if (waveDesc) waveDesc.innerHTML = '<strong>Sonda Lambda (banda estreita):</strong> alterna entre 0,1V (magro) e 0,9V (rico). Uma onda estacionária ou muito lenta indica sensor sujo ou envelhecido.';
        }
        else if (type === 'can') {
          ctx.beginPath();
          ctx.strokeStyle = '#FFB700';
          for (let x = 0; x <= w; x++) {
            const t = phase + (x / w) * Math.PI * 8;
            const y = centerY - 30 + Math.sin(t) * 15;
            if (x === 0) ctx.moveTo(x, y);
            else ctx.lineTo(x, y);
          }
          ctx.stroke();
          ctx.beginPath();
          ctx.strokeStyle = '#3B82F6';
          for (let x = 0; x <= w; x++) {
            const t = phase + (x / w) * Math.PI * 8;
            const y = centerY + 30 + Math.sin(t + Math.PI) * 15;
            if (x === 0) ctx.moveTo(x, y);
            else ctx.lineTo(x, y);
          }
          ctx.stroke();
          if (waveDesc) waveDesc.innerHTML = '<strong>Rede CAN Bus:</strong> sinais diferenciais (CAN_H e CAN_L). Uma tela limpa e simétrica indica boa comunicação. Ruídos ou assimetria sugerem terminação errada ou curto no barramento.';
        }
      }

      function animate() {
        const type = waveSelect ? waveSelect.value : 'ckp_indutivo';
        const rpm = rpmSlider ? parseInt(rpmSlider.value) : 850;
        drawWaveform(type, rpm);
        phase += 0.05;
        animationId = requestAnimationFrame(animate);
      }

      function updateRpm() {
        if (rpmDisplay && rpmSlider) {
          rpmDisplay.textContent = rpmSlider.value;
        }
      }

      if (rpmSlider) rpmSlider.addEventListener('input', updateRpm);
      if (waveSelect) waveSelect.addEventListener('change', () => { /* apenas para não travar */ });
      animate();
    }

    // Inicia o simulador somente quando a página p10 for exibida
    const originalShowPageBackup = window.showPage;
    if (typeof originalShowPageBackup === 'function') {
      window.showPage = function (pageId) {
        originalShowPageBackup(pageId);
        if (pageId === 'p10') {
          setTimeout(startSimulator, 100);
        }
      };
    } else {
      // Fallback: se showPage não estiver definida, inicia imediatamente
      startSimulator();
    }

    // Interatividade dos cards de parâmetros
    const paramCards = document.querySelectorAll('.comp-card[data-param]');
    const paramDetailDiv = document.getElementById('param-detail');
    const paramDetails = {
      vertical: '<strong>📏 Escala vertical (Volts/Div)</strong><br>Define quantos volts cada quadrado vertical representa. Ajuste para que o sinal ocupe cerca de 60-80% da tela. Ex: para um sensor de 5V, use 1V/div; para a bateria, 5V/div.',
      horizontal: '<strong>⏱️ Base de tempo (Time/Div)</strong><br>Define o intervalo de tempo por divisão horizontal. Para injetores (pulsos de ms), use 1-2ms/div; para o sinal do CKP em marcha lenta, 20ms/div; para CAN Bus, 1µs/div.',
      trigger: '<strong>🎯 Gatilho (Trigger)</strong><br>Estabiliza a imagem. O osciloscópio começa a desenhar o sinal quando ele cruza um nível de tensão definido (trigger level). Use modo "Normal" para sinais periódicos.',
      coupling: '<strong>🔌 Acoplamento (Coupling)</strong><br>DC (Direct Coupling): vê o sinal completo. AC (Alternating Coupling): bloqueia a componente contínua, útil para medir pequenas ondulações (ripple) de até 100mV sobrepostas a 12V.'
    };
    if (paramCards.length && paramDetailDiv) {
      paramCards.forEach(card => {
        card.addEventListener('click', () => {
          const param = card.getAttribute('data-param');
          if (param && paramDetails[param]) {
            paramDetailDiv.innerHTML = paramDetails[param];
            paramDetailDiv.style.display = 'block';
          } else {
            paramDetailDiv.style.display = 'none';
          }
        });
      });
    }
  })();

  // ============================================================
  //  REGISTRO DOS NOVOS QUIZZES (qosc1, qosc2, qosc3)
  //  (Integra ao sistema de quizzes existente do module01.js)
  // ============================================================
  (function registerOscQuizzes() {
    // Adiciona as explicações ao objeto quizExplain (que já existe no module01.js)
    if (window.quizExplain) {
      window.quizExplain.qosc1 = 'O osciloscópio mostra a forma de onda ao longo do tempo, capturando eventos rápidos que o multímetro, por tirar uma média, não consegue registrar.';
      window.quizExplain.qosc2 = 'Um tempo de subida lento indica que o sinal está sendo “arrastado” por uma resistência extra no caminho (mau contato, fio partido internamente, conector oxidado).';
      window.quizExplain.qosc3 = 'Os pulsos de um injetor duram poucos milissegundos (ms). Para visualizar um ou dois pulsos completos, a base de tempo mais adequada é 1 ms/div.';
    }

    // Função auxiliar para conectar os novos quizzes ao sistema de clique
    function setupNewQuizzes() {
      const quizIds = ['qosc1', 'qosc2', 'qosc3'];
      quizIds.forEach(id => {
        const optsDiv = document.getElementById(id + '-opts');
        if (optsDiv && !optsDiv.dataset.registered) {
          optsDiv.dataset.registered = 'true';
          const options = optsDiv.querySelectorAll('.quiz-option');
          options.forEach(opt => {
            opt.addEventListener('click', () => {
              // Se já respondido, não faz nada
              if (optsDiv.dataset.done) return;
              optsDiv.dataset.done = '1';
              options.forEach(o => o.style.pointerEvents = 'none');
              const correct = opt.dataset.correct === 'true';
              opt.classList.add(correct ? 'correct' : 'wrong');
              const fb = document.getElementById(id + '-fb');
              if (fb) {
                fb.className = 'quiz-feedback show ' + (correct ? 'ok' : 'fail');
                fb.textContent = (correct ? '✅ Correto! ' : '❌ Não é essa. ') + (window.quizExplain ? (window.quizExplain[id] || '') : '');
              }
              // Marca a página p10 como visitada ao acertar qualquer quiz (opcional)
              if (correct && window.visitedPages && !window.visitedPages.has('p10')) {
                window.visitedPages.add('p10');
                if (typeof window.saveProgress === 'function') window.saveProgress();
              }
            });
          });
        }
      });
    }

    // Executa após o DOM carregar e também sempre que a página p10 for mostrada
    if (document.readyState === 'loading') {
      document.addEventListener('DOMContentLoaded', setupNewQuizzes);
    } else {
      setupNewQuizzes();
    }

    // Se existir a função showPage do módulo, chama o setup novamente ao mostrar p10
    const originalShowPage = window.showPage;
    if (typeof originalShowPage === 'function') {
      window.showPage = function (pageId) {
        originalShowPage(pageId);
        if (pageId === 'p10') {
          setTimeout(setupNewQuizzes, 50);
        }
      };
    }
  })();

  // ============================================================
  //  SIMULADOR DE OSCILOSCÓPIO (AULA 10) - VERSÃO CORRIGIDA
  // ============================================================
  (function initOscilloscope() {
    let oscAnimationId = null;
    let oscPhase = 0;

    function startOscSimulator() {
      const canvas = document.getElementById('oscilloscopeCanvas');
      if (!canvas) {
        console.warn('Canvas não encontrado');
        return;
      }
      const ctx = canvas.getContext('2d');
      const waveSelect = document.getElementById('wave-select');
      const rpmSlider = document.getElementById('rpm_slider');
      const rpmDisplay = document.getElementById('rpm_display');
      const waveDesc = document.getElementById('wave_desc');

      if (!waveSelect || !rpmSlider) {
        console.warn('Elementos do simulador não encontrados');
        return;
      }

      function resizeCanvas() {
        const container = canvas.parentElement;
        if (!container) return;
        const width = container.clientWidth;
        canvas.width = Math.max(300, Math.min(width, 800));
        canvas.height = 200;
      }

      window.addEventListener('resize', resizeCanvas);
      resizeCanvas();

      function drawWaveform(type, rpm) {
        if (!ctx) return;
        const w = canvas.width;
        const h = canvas.height;
        if (w === 0 || h === 0) return;

        ctx.clearRect(0, 0, w, h);
        ctx.fillStyle = '#0a0f1a';
        ctx.fillRect(0, 0, w, h);
        ctx.strokeStyle = '#22C55E';
        ctx.lineWidth = 2;
        ctx.beginPath();

        const centerY = h / 2;
        const amplitude = Math.min(centerY - 10, 30 + (rpm / 7000) * 50);
        const cycles = Math.max(1, Math.floor(rpm / 500));

        if (type === 'ckp_indutivo') {
          for (let x = 0; x <= w; x++) {
            const t = oscPhase + (x / w) * Math.PI * 4 * cycles;
            const y = centerY + Math.sin(t) * amplitude;
            if (x === 0) ctx.moveTo(x, y);
            else ctx.lineTo(x, y);
          }
          ctx.stroke();
          if (waveDesc) waveDesc.innerHTML = '<strong>Sinal CKP indutivo:</strong> forma senoidal, amplitude aumenta com a rotação. Em baixas rotações (< 200 RPM) o sinal pode ser muito fraco.';
        }
        else if (type === 'ckp_hall') {
          const pulses = Math.max(4, Math.floor(rpm / 150));
          const step = w / pulses;
          for (let i = 0; i < pulses; i++) {
            const x1 = i * step;
            const x2 = (i + 0.45) * step;
            const x3 = (i + 0.55) * step;
            const x4 = (i + 1) * step;
            if (i === 0) ctx.moveTo(x1, centerY - 40);
            ctx.lineTo(x1, centerY - 40);
            ctx.lineTo(x2, centerY - 40);
            ctx.lineTo(x2, centerY + 40);
            ctx.lineTo(x3, centerY + 40);
            ctx.lineTo(x3, centerY - 40);
            ctx.lineTo(x4, centerY - 40);
          }
          ctx.stroke();
          if (waveDesc) waveDesc.innerHTML = '<strong>Sinal CKP Hall:</strong> onda quadrada (0–12V), amplitude constante. Um tempo de subida lento indica mau contato.';
        }
        else if (type === 'injetor') {
          const pulsesPerSec = rpm / 60 / 2;
          const pulseWidthMs = 3 + (rpm / 3000) * 5;
          const periodMs = pulsesPerSec > 0 ? 1000 / pulsesPerSec : 100;
          const duty = pulseWidthMs / periodMs;
          const steps = 20;
          const stepX = w / steps;
          let x = 0;
          ctx.moveTo(0, centerY + 30);
          for (let i = 0; i < steps; i++) {
            const on = (i % 2 === 0);
            const y = on ? centerY - 40 : centerY + 30;
            ctx.lineTo(x, y);
            x += stepX;
            ctx.lineTo(x, y);
          }
          ctx.stroke();
          if (waveDesc) waveDesc.innerHTML = `<strong>Injetor:</strong> pulso de comando com largura variável (duty ≈ ${(duty * 100).toFixed(0)}% a ${rpm} RPM). Pulso nítido indica bom funcionamento.`;
        }
        else if (type === 'bobina') {
          const freq = Math.max(0.5, rpm / 60 / 2);
          for (let x = 0; x <= w; x++) {
            const t = (x / w) * Math.PI * 2 * freq * 0.5;
            let y = centerY;
            if (Math.sin(t) > 0.8) y = centerY - 50;
            else if (Math.sin(t) < -0.8) y = centerY + 30;
            else y = centerY;
            if (x === 0) ctx.moveTo(x, y);
            else ctx.lineTo(x, y);
          }
          ctx.stroke();
          if (waveDesc) waveDesc.innerHTML = '<strong>Bobina primária:</strong> queda abrupta seguida de pico de indução (flyback). Picos assimétricos indicam problema na bobina ou módulo.';
        }
        else if (type === 'lambda') {
          const freqOsc = 1.5 + (rpm / 3000);
          for (let x = 0; x <= w; x++) {
            const t = oscPhase + (x / w) * Math.PI * 4 * freqOsc;
            const val = (Math.sin(t) + 1) / 2;
            const y = centerY + (0.4 - val) * 60;
            if (x === 0) ctx.moveTo(x, y);
            else ctx.lineTo(x, y);
          }
          ctx.stroke();
          if (waveDesc) waveDesc.innerHTML = '<strong>Sonda Lambda:</strong> alterna entre 0,1V (magro) e 0,9V (rico). Onda estacionária indica sensor sujo.';
        }
        else if (type === 'can') {
          ctx.beginPath();
          ctx.strokeStyle = '#FFB700';
          for (let x = 0; x <= w; x++) {
            const t = oscPhase + (x / w) * Math.PI * 8;
            const y = centerY - 30 + Math.sin(t) * 15;
            if (x === 0) ctx.moveTo(x, y);
            else ctx.lineTo(x, y);
          }
          ctx.stroke();
          ctx.beginPath();
          ctx.strokeStyle = '#3B82F6';
          for (let x = 0; x <= w; x++) {
            const t = oscPhase + (x / w) * Math.PI * 8;
            const y = centerY + 30 + Math.sin(t + Math.PI) * 15;
            if (x === 0) ctx.moveTo(x, y);
            else ctx.lineTo(x, y);
          }
          ctx.stroke();
          if (waveDesc) waveDesc.innerHTML = '<strong>Rede CAN Bus:</strong> sinais diferenciais (CAN_H e CAN_L). Sinal limpo e simétrico indica boa comunicação.';
        }
      }

      function animate() {
        if (!document.getElementById('oscilloscopeCanvas')) {
          // Se o canvas sumir do DOM, cancela animação
          if (oscAnimationId) cancelAnimationFrame(oscAnimationId);
          return;
        }
        const type = waveSelect.value;
        const rpm = parseInt(rpmSlider.value);
        drawWaveform(type, rpm);
        oscPhase += 0.05;
        oscAnimationId = requestAnimationFrame(animate);
      }

      function updateRpm() {
        if (rpmDisplay) rpmDisplay.textContent = rpmSlider.value;
      }

      rpmSlider.addEventListener('input', updateRpm);
      waveSelect.addEventListener('change', function () {
        // Força redesenho imediato
        drawWaveform(waveSelect.value, parseInt(rpmSlider.value));
      });
      updateRpm();
      animate();
    }

    // Tenta iniciar o simulador quando a página p10 for ativada
    // Método 1: observar a mudança de classe active na página
    function checkAndStart() {
      const pageP10 = document.getElementById('page-p10');
      if (pageP10 && pageP10.classList.contains('active')) {
        startOscSimulator();
        return true;
      }
      return false;
    }

    // Se a página já estiver ativa, inicia
    if (!checkAndStart()) {
      // Configura um MutationObserver para detectar quando a página p10 se tornar ativa
      const observer = new MutationObserver(function (mutations) {
        mutations.forEach(function (mutation) {
          if (mutation.type === 'attributes' && mutation.attributeName === 'class') {
            const target = mutation.target;
            if (target.id === 'page-p10' && target.classList.contains('active')) {
              startOscSimulator();
              observer.disconnect();
            }
          }
        });
      });
      const pageP10 = document.getElementById('page-p10');
      if (pageP10) {
        observer.observe(pageP10, { attributes: true });
      } else {
        // Se a página ainda não existe no DOM, aguarda um pouco e tenta novamente
        setTimeout(() => {
          const p10 = document.getElementById('page-p10');
          if (p10 && p10.classList.contains('active')) startOscSimulator();
          else if (p10) observer.observe(p10, { attributes: true });
        }, 500);
      }
    }

    // Interatividade dos cards de parâmetros
    function initParamCards() {
      const paramCards = document.querySelectorAll('.comp-card[data-param]');
      const detailDiv = document.getElementById('param-detail');
      if (!detailDiv) return;
      const descriptions = {
        vertical: '<strong>📏 Escala vertical (Volts/Div)</strong><br>Define quantos volts cada quadrado vertical representa. Ex: sinal de 5V → use 1V/div.',
        horizontal: '<strong>⏱️ Base de tempo (Time/Div)</strong><br>Define o intervalo de tempo por divisão. Injetor: 1-2ms/div; CKP: 20ms/div.',
        trigger: '<strong>🎯 Gatilho (Trigger)</strong><br>Estabiliza a imagem. O sinal é desenhado quando cruza o nível definido.',
        coupling: '<strong>🔌 Acoplamento (Coupling)</strong><br>DC: sinal completo. AC: bloqueia componente contínua, útil para ripple.'
      };
      paramCards.forEach(card => {
        card.addEventListener('click', function () {
          const param = this.getAttribute('data-param');
          if (param && descriptions[param]) {
            detailDiv.innerHTML = descriptions[param];
            detailDiv.style.display = 'block';
          } else {
            detailDiv.style.display = 'none';
          }
        });
      });
    }

    // Aguarda o DOM carregar para inicializar os cards
    if (document.readyState === 'loading') {
      document.addEventListener('DOMContentLoaded', initParamCards);
    } else {
      initParamCards();
    }
  })();

  // ===== QUIZZES =====
  const quizExplain = {
    q1: '0,3Ω é um valor normal para um fio curto. Se o fio estivesse partido, a resistência seria infinita (OL no multímetro). O problema da lâmpada não acender está em outro lugar: verifique fusível, interruptor, soquete ou aterramento.',
    q1b: 'Um farol mais fraco com lâmpada boa indica que há resistência extra no circuito – geralmente corrosão no soquete ou mau contato no ponto de massa. Essa resistência "rouba" tensão, fazendo a lâmpada brilhar menos. Verifique a queda de tensão no circuito.',
    q1c: 'Corrente é medida EM SÉRIE – o multímetro precisa ser inserido no caminho da corrente. Use a entrada de 10A e a escala adequada. Medir corrente em paralelo causa curto-circuito e pode queimar o fusível do multímetro.',
    q2: 'Pela Lei de Ohm, I = V / R. Se V dobra e R é constante, I também dobra (são diretamente proporcionais).',
    q2b: 'P = V × I → I = P / V = 60W / 12V = 5A. Lembre-se: faróis comuns consomem entre 4A e 5A cada.',
    q2c: 'R = V / I = 1,2V / 8A = 0,15Ω. Uma resistência extra de 0,15Ω já é suficiente para causar queda de tensão significativa e aquecimento no conector.',
    q4: 'Em um circuito em série, há apenas um caminho para a corrente. Se uma lâmpada queima, esse caminho é interrompido (circuito aberto), e a corrente para de fluir, apagando todas as outras lâmpadas.',
    q4b: 'Os faróis são ligados em paralelo para garantir a segurança: se um queimar, o outro permanece aceso. Se fossem em série, a queima de um apagaria os dois, o que seria extremamente perigoso à noite.',
    q4c: 'Se todas as lâmpadas de um circuito em paralelo estão fracas, o problema não está em uma lâmpada específica, mas sim em um ponto comum a todas. A causa mais provável é uma resistência indesejada (mau contato, oxidação) no ponto de alimentação ou no aterramento, que está "roubando" tensão de todo o circuito.',
    q5: 'Pela fórmula do Efeito Joule, P = I² × R. Se a corrente dobra (2I), a potência dissipada (calor) é (2I)² × R = 4 × I² × R. O calor quadruplica! Por isso fios subdimensionados aquecem tanto.',
    q5b: 'Corrente = 180W / 12V = 15A. Queda máx. (3%) = 0,36V. A = (2 × 0,0175 × 3 × 15) / 0,36 = 1,575 / 0,36 = 4,38 mm². A bitola comercial superior é 4,0 mm² (12 AWG).',
    q5c: 'Farol fraco com fusível OK indica que a corrente está passando, mas a tensão que chega à lâmpada é menor que 12V. Isso é causado por resistência extra no circuito (mau contato, oxidação, massa ruim), que "rouba" tensão por Efeito Joule.',
    q6: 'Pela Lei das Malhas (KVL): V_bateria = V_farol + V_conector. Se V_bateria = 12V e V_farol = 10V, então V_conector = 12V - 10V = 2V. Esses 2V estão sendo "roubados" pelo mau contato.',
    q6b: 'Pela Lei dos Nós (KCL): I_total = I_A + I_B = 4,5A + 4,5A = 9A. A corrente que chega ao nó se divide igualmente entre os dois ramos.',
    q6c: 'KVL: V_fonte = V_carga + V_quedas. 12,4V - 9,8V = 2,6V de queda. Essa tensão "desaparecida" está sendo dissipada em resistências indesejadas (conectores oxidados, emendas ruins, massa corroída) ao longo do circuito.',
    q7: 'A força de um eletroímã é proporcional ao produto da corrente pelo número de espiras (Ampere-espiras). No entanto, núcleos de ferro têm um limite de saturação magnética – a partir de certo ponto, aumentar a corrente não aumenta mais o campo magnético na mesma proporção.',
    q7b: 'Pela Lei de Ohm: I = V / R = 12V / 80Ω = 0,15A (150 mA). Essa é a corrente típica da bobina de um relé automotivo, que é baixa o suficiente para ser controlada diretamente por transistores da ECU ou BCM.',
    q7c: 'Quando um relé é desenergizado, a bobina gera um pico de tensão (flyback) que pode danificar o transistor que a aciona. O diodo flyback (ou supressor) em paralelo com a bobina fornece um caminho para essa corrente se dissipar. Sem ele, o pico de tensão pode atingir centenas de volts e destruir o transistor do módulo.',
    q8: 'Em corrente contínua (regime permanente), um capacitor se comporta como um circuito aberto (bloqueia CC) porque suas placas são isoladas pelo dielétrico. Um indutor se comporta como um curto-circuito (permite CC) porque é apenas um fio enrolado, com resistência muito baixa.',
    q8b: 'A bobina de ignição é um transformador flyback. Quando a corrente no primário é interrompida, o campo magnético colapsa rapidamente, induzindo uma tensão de 20.000 a 40.000 volts no secundário – o suficiente para gerar a faísca na vela.',
    q8c: 'Ripple CA acima de 50-100mV indica que a tensão não está sendo adequadamente filtrada. As causas mais comuns são: capacitor de filtro do alternador com capacitância reduzida (seco/envelhecido) ou um ou mais diodos da ponte retificadora com defeito (aberto ou em curto).',
    q9: 'Para medir tensão (V), a ponta vermelha deve estar no terminal VΩmA. O terminal 10A é usado apenas para medir correntes altas. Usar o terminal errado pode danificar o multímetro ou causar leituras incorretas.',
    q9b: 'NUNCA meça resistência em um circuito energizado. A tensão externa pode danificar o multímetro e a leitura será incorreta. Sempre desligue a alimentação e, de preferência, desconecte o componente do chicote.',
    q9c: 'A diferença de tensão (12,4V - 10,8V = 1,6V) é a queda de tensão causada por uma resistência indesejada no circuito (mau contato, fio oxidado, conector frouxo). Essa resistência "rouba" tensão que deveria chegar ao farol.',
    q10: 'O osciloscópio é a ferramenta ideal para problemas intermitentes porque ele exibe a tensão em função do tempo, permitindo visualizar variações rápidas (milissegundos ou microssegundos) que o multímetro, por fazer uma média, não consegue capturar. Dessa forma, é possível ver, por exemplo, um pulso faltando no sensor CKP ou um pico de ruído na alimentação de um módulo.',
    q11: 'Um tempo de subida (rise time) mais lento em um dos pulsos indica que o sinal está sendo “arrastado” por uma resistência extra no caminho – mau contato no conector, fio parcialmente rompido ou oxidação no terminal. Isso não afeta a amplitude, mas distorce a forma de onda, podendo causar leitura incorreta pela ECU.',
    q12: 'Os pulsos de comando de um injetor duram poucos milissegundos (tipicamente entre 2 ms e 10 ms). Para visualizar um ou dois pulsos completos na tela, a base de tempo mais adequada é a faixa de milissegundos por divisão (ms/div), como 1 ms/div ou 2 ms/div. Escalas maiores (s/div) mostram apenas uma linha reta; escalas muito pequenas (µs/div) mostram apenas o início do pulso.'
  };

  function initQuizzes() {
    document.querySelectorAll('.quiz-options').forEach(opts => {
      const id = opts.id?.replace('-opts', '');
      if (!id) return;
      opts.querySelectorAll('.quiz-option').forEach(opt => {
        opt.addEventListener('click', () => handleQuiz(id, opt));
      });
    });
  }

  function handleQuiz(id, opt) {
    const opts = document.getElementById(id + '-opts');
    if (!opts || opts.dataset.done) return;
    opts.dataset.done = '1';
    opts.querySelectorAll('.quiz-option').forEach(o => o.style.pointerEvents = 'none');
    const correct = opt.dataset.correct === 'true';
    opt.classList.add(correct ? 'correct' : 'wrong');
    const fb = document.getElementById(id + '-fb');
    if (!fb) return;
    if (correct) {
      fb.className = 'quiz-feedback show ok';
      fb.textContent = '✅ Correto! ' + (quizExplain[id] || '');
      const pageMap = { q6: 'p6', q7: 'p7', q8: 'p8', q9: 'p9', q10: 'p10' };
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

  // ===== EVENT LISTENERS =====
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

    document.querySelector('[data-action="start"]')?.addEventListener('click', (e) => {
      const btn = e.currentTarget;
      const page = btn.dataset.page;
      if (page) showPage(page);
    });

    document.querySelectorAll('.solve-tab[data-ohm]').forEach(btn => {
      btn.addEventListener('click', () => setOhmMode(btn.dataset.ohm, btn));
    });
    document.getElementById('ohm-calc-btn')?.addEventListener('click', calcOhm);

    document.querySelectorAll('.solve-tab[data-pow]').forEach(btn => {
      btn.addEventListener('click', () => setPowMode(btn.dataset.pow, btn));
    });
    document.getElementById('pow-calc-btn')?.addEventListener('click', calcPow);

    document.getElementById('jw-p')?.addEventListener('input', calcJoule);
    document.getElementById('jw-v')?.addEventListener('input', calcJoule);

    document.querySelectorAll('.circ-tab[data-circ]').forEach(btn => {
      btn.addEventListener('click', () => setCirc(btn.dataset.circ, btn));
    });
    document.getElementById('sr1')?.addEventListener('input', updateSerie);
    document.getElementById('sr2')?.addEventListener('input', updateSerie);
    document.getElementById('pr1')?.addEventListener('input', updatePara);
    document.getElementById('pr2')?.addEventListener('input', updatePara);
    updateSerie();
    updatePara();

    const themeToggle = document.getElementById('theme-toggle');
    if (themeToggle && typeof themeManager !== 'undefined') {
      themeToggle.addEventListener('click', () => themeManager.toggleTheme());
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

  // ===== INICIALIZAÇÃO =====
  document.addEventListener('DOMContentLoaded', () => {
    loadProgress();
    attachEventListeners();
    initQuizzes();
    showPage('home');
  });

})();