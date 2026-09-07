using FlaUI.Core.AutomationElements;
using FlaUI.Core;
using FlaUI.UIA3;

namespace PrimoAutoEletrica.UiTests.Helpers
{
    /// <summary>
    /// Helper para localizar elementos da UI usando AutomationId ou Name.
    /// </summary>
    public static class UiElementFinder
    {
        /// <summary>
        /// Busca um elemento dentro da janela principal pelo AutomationId.
        /// </summary>
        public static AutomationElement FindById(this AutomationElement root, string automationId)
        {
            return root.FindFirstDescendant(cf => cf.ByAutomationId(automationId));
        }

        /// <summary>
        /// Busca um elemento pelo texto exibido (Name).
        /// </summary>
        public static AutomationElement FindByName(this AutomationElement root, string name)
        {
            return root.FindFirstDescendant(cf => cf.ByName(name));
        }

        /// <summary>
        /// Clica em um botão identificado por AutomationId.
        /// </summary>
        public static void ClickButtonById(this AutomationElement root, string automationId)
        {
            var button = root.FindById(automationId);
            button?.AsButton()?.Invoke();
        }
    }
}
