from pathlib import Path
import re

types = Path(r"PrimoAutoEletrica/Services/UiSmokeTestService.Types.cs")
t = types.read_text(encoding="utf-8")
if "if (IsSmokeHostWindow(window))" not in t:
    t2, n = re.subn(
        r"(if \(window is MainWindow\)\s*\{\s*continue;\s*\}\s*)(if \(window is OperacaoCaixaWindow\))",
        r"""\1
                    // C1.1.5: hosts do ExerciseInteractionSurface (Title=TypeName, Content=UserControl)
                    // nao sao dialogs — nunca clicar/fechar (protege contra supervisor stale).
                    if (IsSmokeHostWindow(window))
                    {
                        continue;
                    }

                    \2""",
        t,
        count=1,
        flags=re.S,
    )
    print("types_guard_subs", n)
    types.write_text(t2, encoding="utf-8")
else:
    print("types_guard_already")

helpers = Path(r"PrimoAutoEletrica/Services/UiSmokeTestService.Helpers.cs")
h = helpers.read_text(encoding="utf-8")
if "EnsureHostWindowOperational" not in h:
    method = (
        "        private static void EnsureHostWindowOperational(Window window, string context)\n"
        "        {\n"
        "            if (window == null)\n"
        "            {\n"
        '                throw new InvalidOperationException($"Host window nulo em {context}.");\n'
        "            }\n"
        "\n"
        "            if (window.Dispatcher.HasShutdownStarted || window.Dispatcher.HasShutdownFinished)\n"
        "            {\n"
        '                throw new InvalidOperationException($"Host window em shutdown em {context}.");\n'
        "            }\n"
        "\n"
        "            if (!window.IsLoaded || !window.IsVisible)\n"
        "            {\n"
        "                RestoreWindowForInteraction(window);\n"
        "            }\n"
        "\n"
        "            if (!window.IsLoaded || !window.IsVisible)\n"
        "            {\n"
        "                throw new InvalidOperationException(\n"
        '                    $"Host window fechada/invisivel antes da interacao em {context}. " +\n'
        '                    "Possivel race do AutomatedDialogSupervisor sobre host de smoke.");\n'
        "            }\n"
        "        }\n"
        "\n"
    )
    h2, n = re.subn(
        r"(private static void RestoreWindowForInteraction\(Window window\)\s*\{)",
        method + r"\1",
        h,
        count=1,
    )
    print("helpers_method_subs", n)
    h2, n2 = re.subn(
        r"(supervisor = new AutomatedDialogSupervisor\(surface\.HostWindow, _fixture\);\s*supervisor\.Start\(\);\s*)(if \(!targetButton\.IsEnabled\))",
        r"""\1
                    EnsureHostWindowOperational(surface.HostWindow, $"{rootType.Name}/{descriptor.DisplayName}");

                    \2""",
        h2,
        count=1,
        flags=re.S,
    )
    print("helpers_call_subs", n2)
    helpers.write_text(h2, encoding="utf-8")
else:
    print("helpers_already")
