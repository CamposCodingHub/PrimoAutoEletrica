/**
 * modulo01.js - Lógica do Módulo 01 (Fundamentos de Eletricidade)
 * Sem onclick inline, usando event listeners.
 */

(function() {
  // ===== PÁGINAS E NAVEGAÇÃO =====
  const pages = ['home', 'p1', 'p2', 'p3', 'p4', 'p5', 'p6', 'p7', 'p8', 'p9', 'p10'];
  let visitedPages = new Set();

  function loadProgress() {
    const saved = localStorage.getItem('modulo01-visited');
    if (saved) visitedPages = new Set(JSON.parse(saved));
    updateProgressUI();
  }

  function saveProgress() {
    localStorage.setItem('modulo01-visited', JSON.stringify([...visitedPages]));
    updateProgressUI();
  }

  function updateProgressUI() {
    const total = pages.filter(p => p !== 'home').length;
    const pct = Math.round((visitedPages.size / total) * 100);
    const progBar = document.getElementById('prog-bar');
    const progText = document.getElementById('prog-pct');
    if (progBar) progBar.style.width = pct + '%';
    if (progText) progText.textContent = pct + '%';

    // Marcar itens do menu como "done"
    document.querySelectorAll('.nav-item[data-page]').forEach(item => {
      const page = item.dataset.page;
      if (visitedPages.has(page)) {
        item.classList.add('done');
      } else {
        item.classList.remove('done');
      }
    });
  }

  function showPage(pageId) {
    // Esconder todas as páginas
    document.querySelectorAll('.lesson-page').forEach(el => el.classList.remove('active'));
    const targetPage = document.getElementById(`page-${pageId}`);
    if (targetPage) targetPage.classList.add('active');

    // Atualizar menu ativo
    document.querySelectorAll('.nav-item').forEach(item => item.classList.remove('active'));
    const activeMenuItem = document.querySelector(`.nav-item[data-page="${pageId}"]`);
    if (activeMenuItem) activeMenuItem.classList.add('active');

    // Scroll to top
    document.querySelector('.main').scrollTo({ top: 0, behavior: 'smooth' });

    // Registrar página visitada (exceto home)
    if (pageId !== 'home' && !visitedPages.has(pageId)) {
      visitedPages.add(pageId);
      saveProgress();
    }
  }

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
    const v = parseFloat(document.getElementById('ohm-v').value);
    const i = parseFloat(document.getElementById('ohm-i').value);
    const r = parseFloat(document.getElementById('ohm-r').value);
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
    const p = parseFloat(document.getElementById('pow-p').value);
    const v = parseFloat(document.getElementById('pow-v').value);
    const i = parseFloat(document.getElementById('pow-i').value);
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
      const suggested = fuseValues.find(f => f >= result * 1.25) || fuseValues[fuseValues.length-1];
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
    const p = parseFloat(document.getElementById('jw-p').value);
    const v = parseFloat(document.getElementById('jw-v').value) || 12;
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

  // ===== SIMULADOR DE CIRCUITOS SÉRIE/PARALELO =====
  function setCirc(type, btn) {
    document.querySelectorAll('.circ-tab').forEach(t => t.classList.remove('active'));
    if (btn) btn.classList.add('active');
    document.querySelectorAll('.circ-panel').forEach(p => p.classList.remove('active'));
    const panel = document.getElementById(`cpanel-${type}`);
    if (panel) panel.classList.add('active');
  }

  function updateSerie() {
    const r1 = +document.getElementById('sr1').value;
    const r2 = +document.getElementById('sr2').value;
    document.getElementById('sv-r1').textContent = r1;
    document.getElementById('sv-r2').textContent = r2;
    const rt = r1 + r2;
    const it = 12 / rt;
    const v1 = it * r1;
    const v2 = it * r2;
    document.getElementById('s-rt').textContent = rt + ' Ω';
    document.getElementById('s-it').textContent = it.toFixed(2) + ' A';
    document.getElementById('s-vd').textContent = v1.toFixed(1) + 'V / ' + v2.toFixed(1) + 'V';
  }

  function updatePara() {
    const r1 = +document.getElementById('pr1').value;
    const r2 = +document.getElementById('pr2').value;
    document.getElementById('pv-r1').textContent = r1;
    document.getElementById('pv-r2').textContent = r2;
    const rt = (r1 * r2) / (r1 + r2);
    const i1 = 12 / r1;
    const i2 = 12 / r2;
    const it = i1 + i2;
    document.getElementById('p-rt').textContent = rt.toFixed(2) + ' Ω';
    document.getElementById('p-it').textContent = it.toFixed(2) + ' A';
    document.getElementById('p-ib').textContent = i1.toFixed(1) + 'A / ' + i2.toFixed(1) + 'A';
  }

  // ===== QUIZZES =====
  const quizExplain = {
    q1: '0,3Ω é normal em fios curtos. O problema de "não ligar" está em outro ponto (interruptor, fusível).',
    q6: 'Lei dos Nós de Kirchhoff: 8A entram no nó, 4,5A saem para um farol, então sobram 3,5A para o outro.',
    q7: 'Os pinos 85 e 86 energizam a bobina interna com baixa corrente (o "comando").',
    q8: 'Capacitores agem como amortecedores super-rápidos, estabilizando pequenas flutuações e ruídos.',
    q9: 'Tensão é sempre medida em paralelo. Para veículos de passeio, a escala 20V DC é a ideal.',
    q10: 'O osciloscópio mostra eventos ultrarrápidos que o multímetro apenas tira a média.'
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
      if (id !== 'q1' && id !== 'q2' && id !== 'q3' && id !== 'q4' && id !== 'q5') {
        // Marcar página como visitada se o quiz for de uma página de conteúdo
        const pageMap = { q6: 'p6', q7: 'p7', q8: 'p8', q9: 'p9', q10: 'p10' };
        if (pageMap[id] && !visitedPages.has(pageMap[id])) {
          visitedPages.add(pageMap[id]);
          saveProgress();
        }
      }
    } else {
      fb.className = 'quiz-feedback show fail';
      fb.textContent = '❌ Não é essa. ' + (quizExplain[id] || '');
      opts.querySelectorAll('.quiz-option').forEach(o => {
        if (o.dataset.correct === 'true') o.classList.add('correct');
      });
    }
  }

  // ===== INICIALIZAÇÃO =====
  function attachEventListeners() {
    // Navegação sidebar
    document.querySelectorAll('.nav-item[data-page]').forEach(item => {
      item.addEventListener('click', (e) => {
        e.preventDefault();
        const page = item.dataset.page;
        if (page) showPage(page);
      });
    });

    // Botões de navegação (anterior/próximo)
    document.querySelectorAll('[data-nav]').forEach(btn => {
      btn.addEventListener('click', () => {
        const target = btn.dataset.nav;
        if (target) showPage(target);
      });
    });

    // Botão iniciar módulo
    const startBtn = document.querySelector('[data-action="start"]');
    if (startBtn) {
      startBtn.addEventListener('click', () => {
        const page = startBtn.dataset.page;
        if (page) showPage(page);
      });
    }

    // Tabs da calculadora Ohm
    document.querySelectorAll('.solve-tab[data-ohm]').forEach(btn => {
      btn.addEventListener('click', () => setOhmMode(btn.dataset.ohm, btn));
    });
    const ohmCalcBtn = document.getElementById('ohm-calc-btn');
    if (ohmCalcBtn) ohmCalcBtn.addEventListener('click', calcOhm);

    // Tabs da calculadora Potência
    document.querySelectorAll('.solve-tab[data-pow]').forEach(btn => {
      btn.addEventListener('click', () => setPowMode(btn.dataset.pow, btn));
    });
    const powCalcBtn = document.getElementById('pow-calc-btn');
    if (powCalcBtn) powCalcBtn.addEventListener('click', calcPow);

    // Dimensionamento de cabo
    const jwP = document.getElementById('jw-p');
    const jwV = document.getElementById('jw-v');
    if (jwP) jwP.addEventListener('input', calcJoule);
    if (jwV) jwV.addEventListener('input', calcJoule);

    // Circuitos série/paralelo
    document.querySelectorAll('.circ-tab[data-circ]').forEach(btn => {
      btn.addEventListener('click', () => setCirc(btn.dataset.circ, btn));
    });
    const sr1 = document.getElementById('sr1');
    const sr2 = document.getElementById('sr2');
    if (sr1 && sr2) {
      sr1.addEventListener('input', updateSerie);
      sr2.addEventListener('input', updateSerie);
    }
    const pr1 = document.getElementById('pr1');
    const pr2 = document.getElementById('pr2');
    if (pr1 && pr2) {
      pr1.addEventListener('input', updatePara);
      pr2.addEventListener('input', updatePara);
    }

    // Inicializar valores padrão
    if (sr1 && sr2) updateSerie();
    if (pr1 && pr2) updatePara();

    // Tema (usando o mesmo theme-manager do index)
    const themeToggle = document.getElementById('theme-toggle');
    if (themeToggle && typeof themeManager !== 'undefined') {
      themeToggle.addEventListener('click', () => themeManager.toggleTheme());
      themeManager.updateThemeButton();
    }

    // Mobile sidebar toggle
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
    // Exibir página inicial correta (home)
    showPage('home');
  });
})();