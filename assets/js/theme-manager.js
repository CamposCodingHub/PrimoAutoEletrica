/**
 * theme-manager.js - Gerenciador de Tema Claro/Escuro
 * 
 * Funcionalidades:
 * - Alterna entre temas claro (light) e escuro (dark).
 * - Salva a preferência do usuário no localStorage.
 * - Atualiza o ícone do botão de alternância (🌙 / ☀️).
 * - Aplica o tema salvo automaticamente ao carregar a página.
 * 
 * Utilização:
 * - Basta incluir este script em qualquer página do curso.
 * - É necessário um botão com id="theme-toggle" para ativar a alternância.
 * - As variáveis de tema devem estar definidas no CSS (core.css).
 */

(function () {
    'use strict';

    // Constantes
    const THEME_STORAGE_KEY = 'primo-theme';   // chave usada no localStorage
    const LIGHT_THEME = 'light';
    const DARK_THEME = 'dark';
    const THEME_ATTRIBUTE = 'data-theme';      // atributo usado no elemento <html>

    /**
     * Obtém o tema salvo no localStorage.
     * Se nenhum tema estiver salvo, retorna o tema padrão (escuro).
     * @returns {string} 'light' ou 'dark'
     */
    function getSavedTheme() {
        const saved = localStorage.getItem(THEME_STORAGE_KEY);
        // Aceita apenas 'light' ou 'dark'; qualquer outro valor usa o padrão
        if (saved === LIGHT_THEME || saved === DARK_THEME) {
            return saved;
        }
        return DARK_THEME; // Tema padrão (escuro)
    }

    /**
     * Salva o tema no localStorage.
     * @param {string} theme - 'light' ou 'dark'
     */
    function saveTheme(theme) {
        localStorage.setItem(THEME_STORAGE_KEY, theme);
    }

    /**
     * Aplica o tema ao documento (atributo data-theme no <html>).
     * @param {string} theme - 'light' ou 'dark'
     */
    function applyTheme(theme) {
        document.documentElement.setAttribute(THEME_ATTRIBUTE, theme);
        updateToggleButton(theme);
    }

    /**
     * Atualiza o texto/ícone do botão de alternância de tema.
     * @param {string} currentTheme - Tema atualmente ativo
     */
    function updateToggleButton(currentTheme) {
        const toggleBtn = document.getElementById('theme-toggle');
        if (!toggleBtn) return;

        // No tema escuro, mostramos ☀️ (sol) para indicar que o usuário pode clicar para clarear.
        // No tema claro, mostramos 🌙 (lua) para indicar que pode escurecer.
        if (currentTheme === DARK_THEME) {
            toggleBtn.textContent = '☀️';
            toggleBtn.setAttribute('aria-label', 'Alternar para tema claro');
        } else {
            toggleBtn.textContent = '🌙';
            toggleBtn.setAttribute('aria-label', 'Alternar para tema escuro');
        }
    }

    /**
     * Alterna entre os temas claro e escuro.
     * Se estiver escuro, muda para claro; se estiver claro, muda para escuro.
     */
    function toggleTheme() {
        const currentTheme = document.documentElement.getAttribute(THEME_ATTRIBUTE) || DARK_THEME;
        const newTheme = currentTheme === DARK_THEME ? LIGHT_THEME : DARK_THEME;

        applyTheme(newTheme);
        saveTheme(newTheme);
    }

    /**
     * Inicializa o gerenciador de tema:
     * - Aplica o tema salvo (ou o padrão).
     * - Configura o evento de clique no botão de alternância.
     */
    function initThemeManager() {
        // 1. Aplicar o tema inicial
        const initialTheme = getSavedTheme();
        applyTheme(initialTheme);
        saveTheme(initialTheme); // Garante que o localStorage esteja sincronizado

        // 2. Configurar o botão de alternância (se existir)
        const toggleBtn = document.getElementById('theme-toggle');
        if (toggleBtn) {
            toggleBtn.addEventListener('click', toggleTheme);
        }
    }

    // Iniciar assim que o DOM estiver pronto
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initThemeManager);
    } else {
        // DOM já carregado
        initThemeManager();
    }

    // (Opcional) Expor funções no objeto global para debug ou uso avançado
    window.themeManager = {
        toggle: toggleTheme,
        getCurrent: () => document.documentElement.getAttribute(THEME_ATTRIBUTE) || DARK_THEME,
        set: (theme) => {
            if (theme === LIGHT_THEME || theme === DARK_THEME) {
                applyTheme(theme);
                saveTheme(theme);
            }
        }
    };

})();