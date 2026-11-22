$ErrorActionPreference = 'Stop'
$conexion = $null
$copiaTemporal = $null
$codigoSalida = 0

try {
    $opciones = [System.IO.File]::ReadAllLines("$PSScriptRoot\conexion.txt")
    $servidor = $opciones[0].Trim()
    $nombreBase = $opciones[1].Trim()

    $cadena = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
    $cadena['Data Source'] = $servidor
    $cadena['Initial Catalog'] = 'master'
    $cadena['Integrated Security'] = $true
    $cadena['TrustServerCertificate'] = $true
    $cadena['Connect Timeout'] = 10

    $conexion = New-Object System.Data.SqlClient.SqlConnection
    $conexion.ConnectionString = $cadena.ConnectionString
    $conexion.Open()

    $consulta = $conexion.CreateCommand()
    $consulta.CommandText = "SELECT CONVERT(nvarchar(128), SERVERPROPERTY('ComputerNamePhysicalNetBIOS'))"
    $equipoSql = $consulta.ExecuteScalar()

    if ($equipoSql -ne $env:COMPUTERNAME) {
        throw 'Para crear una base nueva, ejecuta el instalador en el equipo donde se encuentra SQL Server.'
    }

    $consulta.CommandText = "SELECT CONVERT(nvarchar(4000), SERVERPROPERTY('InstanceDefaultBackupPath'))"
    $carpetaBackup = [string]$consulta.ExecuteScalar()

    if ([string]::IsNullOrWhiteSpace($carpetaBackup)) {
        throw 'SQL Server no ha indicado su carpeta de copias de seguridad.'
    }

    # SQL Server leerá la copia desde su propia carpeta de backups.
    $nombreCopia = 'GenteFit_' + [guid]::NewGuid().ToString('N') + '.bak'
    $copiaTemporal = Join-Path $carpetaBackup $nombreCopia
    Copy-Item -LiteralPath "$PSScriptRoot\GenteFit_Inicial.bak" -Destination $copiaTemporal

    $consulta.CommandText = Get-Content -LiteralPath "$PSScriptRoot\RestaurarBase.sql" -Raw -Encoding UTF8
    $consulta.CommandTimeout = 300
    $null = $consulta.Parameters.Add('@NombreBase', [System.Data.SqlDbType]::NVarChar, 128)
    $null = $consulta.Parameters.Add('@RutaCopia', [System.Data.SqlDbType]::NVarChar, 4000)

    if ($nombreBase.Length -gt 128) {
        throw 'El nombre de la base supera los 128 caracteres.'
    }

    $consulta.Parameters['@NombreBase'].Value = [string]$nombreBase
    $consulta.Parameters['@RutaCopia'].Value = [string]$copiaTemporal
    $null = $consulta.ExecuteNonQuery()
}
catch {
    $linea = $_.InvocationInfo.ScriptLineNumber
    $instruccion = $_.InvocationInfo.Line.Trim()
    $mensaje = $_.Exception.GetBaseException().Message

    "Línea $linea | $instruccion | $mensaje" |
        Out-File -LiteralPath "$PSScriptRoot\error-restauracion.txt" -Encoding Unicode

    $codigoSalida = 1
}
finally {
    if ($null -ne $conexion) {
        $conexion.Dispose()
    }

    if ($copiaTemporal -and (Test-Path -LiteralPath $copiaTemporal)) {
        Remove-Item -LiteralPath $copiaTemporal -ErrorAction SilentlyContinue
    }
}

exit $codigoSalida