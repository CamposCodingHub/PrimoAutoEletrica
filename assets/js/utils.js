/**
 * Utilitários gerais
 */
const Utils = {
  // Formatar data
  formatDate(date) {
    return new Intl.DateTimeFormat('pt-BR').format(date);
  },
  
  // Debounce para busca
  debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
      const later = () => {
        clearTimeout(timeout);
        func(...args);
      };
      clearTimeout(timeout);
      timeout = setTimeout(later, wait);
    };
  },
  
  // Salvar preferência do usuário
  savePreference(key, value) {
    localStorage.setItem(`pref_${key}`, JSON.stringify(value));
  },
  
  loadPreference(key, defaultValue = null) {
    const saved = localStorage.getItem(`pref_${key}`);
    return saved ? JSON.parse(saved) : defaultValue;
  }
};