/**
 * index.js - Gerencia os módulos, filtros, progresso e tema
 */
(function() {
  // Dados dos módulos
  const MODULES = [
    { id: 1, name: "Fundamentos de Eletricidade", desc: "Lei de Ohm, potência, circuitos", level: "Básico", file: "modulo01_Fundamentos_eletricidade_completo.html", icon: "⚡", available: true },
    { id: 2, name: "Ferramentas e Equipamentos", desc: "Multímetro, scanner, osciloscópio", level: "Básico", file: "modulo02_Ferramentas_equipamentos_completo.html", icon: "🛠️", available: true },
    { id: 3, name: "Componentes Elétricos Fundamentais", desc: "Fusíveis, relés, diodos, conectores", level: "Básico", file: "modulo03_Componentes_eletricos_fundamentais_completo.html", icon: "🔧", available: true },
    { id: 4, name: "Sistema Elétrico do Veículo", desc: "Bateria, alternador, partida, diagramas", level: "Básico", file: "modulo04_Sistema_eletrico_veiculo_visao_geral_completo.html", icon: "🚗", available: true },
    { id: 5, name: "Sistemas de Iluminação", desc: "Halógeno, xenon, LED, DRL", level: "Intermediário", file: "modulo05_Sistemas_iluminacao_completo.html", icon: "💡", available: true },
    { id: 6, name: "Sensores Automotivos", desc: "CKP, CMP, MAP, MAF, TPS, lambda", level: "Intermediário", file: "modulo06_Sensores_automotivos_completo.html", icon: "📡", available: true },
    { id: 7, name: "Atuadores e Cargas", desc: "Injetores, bobinas, solenoides", level: "Intermediário", file: "modulo07_Atuadores_de_carga_completo.html", icon: "⚙️", available: true },
    { id: 8, name: "Redes de Comunicação Veicular", desc: "CAN Bus, LIN, OBD-II, K-Line", level: "Intermediário", file: "modulo08_Redes_comunicacao_veicular_completo.html", icon: "🌐", available: true },
    { id: 9, name: "Central de Injeção Eletrônica (ECU)", desc: "Funcionamento, alimentação, diagnóstico", level: "Intermediário", file: "modulo09_Central_injecao_eletronica_ECU_completo.html", icon: "🧠", available: true },
    { id: 10, name: "Segurança Ativa e Passiva", desc: "ABS, ESP, airbag, SRS, TCS", level: "Avançado", file: "modulo10_seguranca_ativa_passiva.html", icon: "🛡️", available: true },
    { id: 11, name: "Sistema de Ar-Condicionado", desc: "Elétrica do ciclo de refrigeração", level: "Avançado", file: "modulo11_ar_condicionado_automotivo.html", icon: "❄️", available: true },
    { id: 12, name: "Conforto e Carroceria", desc: "BCM, vidros, keyless, bancos", level: "Avançado", file: "modulo12_conforto_carroceria.html", icon: "🪑", available: true },
    { id: 13, name: "Áudio, Multimídia e Telemática", desc: "Rádio, câmera, CarPlay, cluster", level: "Avançado", file: "modulo13_audio_multimidia_telematica.html", icon: "🎵", available: true },
    { id: 14, name: "Elétrica em Caminhões e Pesados", desc: "24V, J1939, ABS/EBS, suspensão", level: "Avançado", file: "modulo14_eletrica_caminhoes_pesados.html", icon: "🚛", available: true },
    { id: 15, name: "Veículos Híbridos e Elétricos", desc: "HEV/BEV, alta tensão, BMS, OBC", level: "Especialista", file: "modulo15_hibridos_eletricos_hev_bev.html", icon: "🔋", available: true },
    { id: 16, name: "Diagnóstico Avançado e Análise de Sinal", desc: "Osciloscópio, voltage drop, parasita", level: "Especialista", file: null, icon: "📊", available: false },
    { id: 17, name: "Programação e Codificação", desc: "Flash, variant coding, transponders", level: "Especialista", file: null, icon: "💻", available: false },
    { id: 18, name: "Rastreamento, Alarmes e Segurança", desc: "GPS, alarmes, CAN tapping, cybersec", level: "Especialista", file: null, icon: "📍", available: false }
  ];

  const UPCOMING = [
    { name: "Diagnóstico Avançado com Osciloscópio", icon: "📈", date: "Em breve" },
    { name: "Programação de ECU e Injeção Eletrônica", icon: "🖥️", date: "Em breve" },
    { name: "Veículos Elétricos - Manutenção de Alta Tensão", icon: "⚡", date: "Em breve" }
  ];

  let currentFilter = 'all';
  let searchTerm = '';
  let completedModules = [];

  // Carregar progresso do localStorage
  function loadProgress() {
    const saved = localStorage.getItem('curso-progresso');
    completedModules = saved ? JSON.parse(saved) : [];
  }

  function saveProgress() {
    localStorage.setItem('curso-progresso', JSON.stringify(completedModules));
    updateProgressUI();
  }

  function isCompleted(id) {
    return completedModules.includes(id);
  }

  function markCompleted(id) {
    if (!completedModules.includes(id)) {
      completedModules.push(id);
      saveProgress();
    }
  }

  function resetProgress() {
    completedModules = [];
    saveProgress();
    renderModules();
    updateNavItems();
  }

  function updateProgressUI() {
    const total = MODULES.filter(m => m.available).length;
    const completedCount = MODULES.filter(m => m.available && completedModules.includes(m.id)).length;
    const percent = Math.round((completedCount / total) * 100);
    const fill = document.getElementById('progress-fill');
    const percentSpan = document.getElementById('progress-percent');
    if (fill) fill.style.width = percent + '%';
    if (percentSpan) percentSpan.textContent = percent + '%';
  }

  function updateNavItems() {
    document.querySelectorAll('.nav-item[data-id]').forEach(item => {
      const id = parseInt(item.dataset.id);
      if (completedModules.includes(id)) {
        item.classList.add('done');
      } else {
        item.classList.remove('done');
      }
    });
  }

  function renderModules() {
    const grid = document.getElementById('modules-grid');
    if (!grid) return;
    const filtered = MODULES.filter(m => {
      const matchFilter = currentFilter === 'all' || m.level === currentFilter;
      const matchSearch = m.name.toLowerCase().includes(searchTerm) || m.desc.toLowerCase().includes(searchTerm);
      return matchFilter && matchSearch;
    });
    if (filtered.length === 0) {
      grid.innerHTML = '<div class="no-results" style="grid-column:1/-1; text-align:center; padding:40px;">Nenhum módulo encontrado.</div>';
      return;
    }
    grid.innerHTML = filtered.map(m => {
      const levelClass = m.level === 'Básico' ? 'basico' : (m.level === 'Intermediário' ? 'inter' : (m.level === 'Avançado' ? 'avan' : 'esp'));
      const completed = isCompleted(m.id);
      const href = m.available && m.file ? m.file : '#';
      const disabledClass = !m.available ? 'disabled' : '';
      const completedClass = completed ? 'completed' : '';
      return `
        <a class="module-card ${disabledClass} ${completedClass}" href="${href}" data-id="${m.id}">
          <div class="module-header">
            <span class="module-icon">${m.icon}</span>
            <span class="module-badge ${levelClass}">${m.level}</span>
          </div>
          <h3 class="module-title">${m.id.toString().padStart(2,'0')} - ${m.name}</h3>
          <p class="module-desc">${m.desc}</p>
          <div class="module-meta">
            <span>📘 ${m.available ? 'Disponível' : 'Em breve'}</span>
            ${completed ? '<span>✅ Concluído</span>' : ''}
          </div>
        </a>
      `;
    }).join('');
  }

  function renderUpcoming() {
    const grid = document.getElementById('upcoming-grid');
    if (!grid) return;
    grid.innerHTML = UPCOMING.map(u => `
      <div class="upcoming-card">
        <div class="icon">${u.icon}</div>
        <div class="name">${u.name}</div>
        <div class="date">📅 ${u.date}</div>
      </div>
    `).join('');
  }

  function applyFilters() {
    renderModules();
  }

  function handleSearch(e) {
    searchTerm = e.target.value.toLowerCase();
    applyFilters();
  }

  function handleFilter(btn, filter) {
    document.querySelectorAll('.filter-btn').forEach(b => b.classList.remove('active'));
    btn.classList.add('active');
    currentFilter = filter;
    applyFilters();
  }

  function initTheme() {
    const savedTheme = localStorage.getItem('theme');
    if (savedTheme === 'dark') {
      document.documentElement.setAttribute('data-theme', 'dark');
      const themeBtn = document.getElementById('theme-toggle');
      if (themeBtn) themeBtn.textContent = '☀️';
    } else {
      document.documentElement.setAttribute('data-theme', 'light');
      const themeBtn = document.getElementById('theme-toggle');
      if (themeBtn) themeBtn.textContent = '🌙';
    }
  }

  function toggleTheme() {
    const current = document.documentElement.getAttribute('data-theme');
    if (current === 'dark') {
      document.documentElement.setAttribute('data-theme', 'light');
      localStorage.setItem('theme', 'light');
      const themeBtn = document.getElementById('theme-toggle');
      if (themeBtn) themeBtn.textContent = '🌙';
    } else {
      document.documentElement.setAttribute('data-theme', 'dark');
      localStorage.setItem('theme', 'dark');
      const themeBtn = document.getElementById('theme-toggle');
      if (themeBtn) themeBtn.textContent = '☀️';
    }
  }

  function initSidebarToggle() {
    const toggleBtn = document.getElementById('menu-toggle');
    const sidebar = document.getElementById('sidebar');
    if (toggleBtn && sidebar) {
      toggleBtn.addEventListener('click', () => {
        sidebar.classList.toggle('open');
      });
      // Fechar ao clicar em link no mobile
      document.querySelectorAll('.nav-item').forEach(link => {
        link.addEventListener('click', () => {
          if (window.innerWidth <= 768) sidebar.classList.remove('open');
        });
      });
    }
  }

  function attachCardEvents() {
    // Marcar como concluído ao clicar em card disponível (delegação)
    const grid = document.getElementById('modules-grid');
    if (grid) {
      grid.addEventListener('click', (e) => {
        const card = e.target.closest('.module-card');
        if (card && !card.classList.contains('disabled')) {
          const id = parseInt(card.dataset.id);
          if (!isNaN(id) && !isCompleted(id)) {
            markCompleted(id);
            renderModules();  // re-renderiza os cards para mostrar badge "Concluído"
            updateNavItems(); // atualiza sidebar
          }
          // Não impedir o clique para que o href funcione
        }
      });
    }

    // Sidebar nav também marca como concluído e NÃO impede a navegação
    document.querySelectorAll('.nav-item[data-id]').forEach(item => {
      // Remove qualquer listener anterior para evitar duplicação
      item.removeEventListener('click', handleNavClick);
      item.addEventListener('click', handleNavClick);
    });
  }

  function handleNavClick(e) {
    const id = parseInt(this.dataset.id);
    if (!isNaN(id) && !this.classList.contains('disabled') && !isCompleted(id)) {
      markCompleted(id);
      renderModules();
      updateNavItems();
    }
    // Permitir que o navegador siga o href normalmente
  }

  document.addEventListener('DOMContentLoaded', () => {
    loadProgress();
    renderModules();
    renderUpcoming();
    updateProgressUI();
    updateNavItems();
    initTheme();
    initSidebarToggle();
    attachCardEvents();

    const searchInput = document.getElementById('search-input');
    if (searchInput) searchInput.addEventListener('input', handleSearch);

    const filterBtns = document.querySelectorAll('.filter-btn');
    filterBtns.forEach(btn => {
      btn.addEventListener('click', () => handleFilter(btn, btn.dataset.filter));
    });

    const resetBtn = document.getElementById('reset-progress');
    if (resetBtn) resetBtn.addEventListener('click', resetProgress);

    const themeToggle = document.getElementById('theme-toggle');
    if (themeToggle) themeToggle.addEventListener('click', toggleTheme);
  });
})();