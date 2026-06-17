@{
    Build = @{
        Configuration = 'Debug'
        Framework = 'net9.0-windows'
    }

    App = @{
        AppDataPath = 'C:\Users\SEU_USUARIO\AppData\Local\PrimoAutoEletrica'
        DatabaseSettingsPath = 'C:\Users\SEU_USUARIO\AppData\Local\PrimoAutoEletrica\database-settings.json'
        CredentialsFile = '.\credenciais-iniciais-admin.txt'
    }

    Environment = @{
        MinimumFreeDiskGb = 10
        NetworkProbeHost = 'www.microsoft.com'
        NetworkProbePort = 443
        RequiredFolders = @(
            'C:\Users\SEU_USUARIO\AppData\Local\PrimoAutoEletrica',
            'C:\Users\SEU_USUARIO\AppData\Local\PrimoAutoEletrica\Backups',
            'C:\Users\SEU_USUARIO\AppData\Local\PrimoAutoEletrica\Logs',
            'C:\Users\SEU_USUARIO\AppData\Local\PrimoAutoEletrica\Database',
            'C:\Users\SEU_USUARIO\AppData\Local\PrimoAutoEletrica\Config',
            'C:\Users\SEU_USUARIO\AppData\Local\PrimoAutoEletrica\Reports'
        )
    }

    Network = @{
        StationRole = 'Cliente'
        PeerAddress = ''
        SharedPath = ''
        SqlConnectionString = ''
        SqlPort = 1433
        LockTimeoutSeconds = 2
        RetryCount = 5
    }

    Printer = @{
        PrinterName = ''
        AskManualConfirmation = $true
        ManualConfirmation = ''
    }

    Backup = @{
        CriticalTables = @(
            'Clientes',
            'Veiculos',
            'Produtos',
            'Fornecedores',
            'Funcionarios',
            'Orcamentos',
            'OrdensServico',
            'Agendamentos',
            'MovimentacoesFinanceiras'
        )
    }

    Simulation = @{
        Days = 7
        SharedAutomationAppData = ''
    }

    CrashRecovery = @{
        KillDelaySeconds = 4
    }

    Visual = @{
        Resolutions = @(
            '1366x768',
            '1440x900',
            '1600x900',
            '1920x1080',
            '2560x1440'
        )
    }
}
