[Setup]
AppName=GenteFit
AppVersion=1.0
DefaultDirName={autopf}\GenteFit
DefaultGroupName=GenteFit
OutputDir=Salida
OutputBaseFilename=GenteFit_Setup

[Files]
Source: "Aplicacion\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs
Source: "BaseDatos\GenteFit_Inicial.bak"; Flags: dontcopy
Source: "BaseDatos\RestaurarBase.sql"; Flags: dontcopy
Source: "BaseDatos\RestaurarBase.ps1"; Flags: dontcopy

[Icons]
Name: "{group}\GenteFit"; Filename: "{app}\GenteFit.WPF.exe"

[Registry]
Root: HKLM32; Subkey: "Software\GenteFit"; ValueType: string; ValueName: "Servidor"; ValueData: "{code:ObtenerServidor}"
Root: HKLM32; Subkey: "Software\GenteFit"; ValueType: string; ValueName: "BaseDatos"; ValueData: "{code:ObtenerBaseDatos}"
[Code]
var
  PaginaBaseDatos: TInputOptionWizardPage;
  PaginaConexion: TInputQueryWizardPage;

  function ObtenerServidor(Param: String): String;
begin
  Result := Trim(PaginaConexion.Values[0]);
end;

function ObtenerBaseDatos(Param: String): String;
begin
  Result := Trim(PaginaConexion.Values[1]);
end;

procedure InitializeWizard;
begin
  PaginaBaseDatos := CreateInputOptionPage(
    wpSelectDir,
    'Configuración de la base de datos',
    '¿Cómo quieres configurar GenteFit?',
    'Selecciona una opción para continuar.',
    True, False);

  PaginaBaseDatos.Add('Crear una base de datos nueva');
  PaginaBaseDatos.Add('Conectarse a una base de datos existente');
  PaginaBaseDatos.SelectedValueIndex := 0;

  PaginaConexion := CreateInputQueryPage(
    PaginaBaseDatos.ID,
    'Conexión con SQL Server',
    'Indica dónde se guardan los datos de GenteFit.',
    'La conexión utilizará la cuenta de Windows.');

  PaginaConexion.Add('Instancia de SQL Server:', False);
  PaginaConexion.Add('Nombre de la base de datos:', False);

  PaginaConexion.Values[0] := 'localhost\SQLEXPRESS';
  PaginaConexion.Values[1] := 'GenteFitDB';
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  if CurPageID = PaginaConexion.ID then
  begin
    if PaginaBaseDatos.SelectedValueIndex = 0 then
      PaginaConexion.SubCaptionLabel.Caption :=
        'Escribe la instancia y el nombre que tendrá la nueva base de datos.'
    else
      PaginaConexion.SubCaptionLabel.Caption :=
        'Escribe la instancia y el nombre de la base de GenteFit que ya existe.';
  end;
end;

function ComprobarBaseExistente(Servidor, BaseDatos: String): Boolean;
var
  Conexion, Consulta: Variant;
begin
  Result := False;

  try
    Conexion := CreateOleObject('ADODB.Connection');
    Conexion.Provider := 'SQLOLEDB';
    Conexion.Properties['Data Source'].Value := Servidor;
    Conexion.Properties['Initial Catalog'].Value := BaseDatos;
    Conexion.Properties['Integrated Security'].Value := 'SSPI';
    Conexion.ConnectionTimeout := 5;
    Conexion.CommandTimeout := 5;
    Conexion.Open;

    try
      Consulta := CreateOleObject('ADODB.Recordset');

      { Comprueba que las tablas y columnas del inicio de sesión son accesibles. }
      Consulta.Open(
        'SELECT TOP (0) u.Username, u.Active ' +
        'FROM dbo.usuarios AS u ' +
        'INNER JOIN dbo.administradores AS a ON a.Id = u.Id',
        Conexion, 0, 1, 1);

      Consulta.Close;
      Result := True;
    finally
      Conexion.Close;
    end;
  except
        MsgBox(
      'No se ha podido validar la base de datos seleccionada.' + #13#10 +
      'Comprueba la instancia, el nombre de la base y los permisos de Windows.' + #13#10#13#10 +
      'Detalle: ' + GetExceptionMessage,
      mbError, MB_OK);
  end;
end;

function ComprobarNombreDisponible(Servidor, BaseDatos: String): Boolean;
var
  Conexion, Consulta: Variant;
  NombreSQL: String;
begin
  Result := False;

  try
    Conexion := CreateOleObject('ADODB.Connection');
    Conexion.Provider := 'SQLOLEDB';
    Conexion.Properties['Data Source'].Value := Servidor;
    Conexion.Properties['Initial Catalog'].Value := 'master';
    Conexion.Properties['Integrated Security'].Value := 'SSPI';
    Conexion.ConnectionTimeout := 5;
    Conexion.CommandTimeout := 5;
    Conexion.Open;

    try
      NombreSQL := BaseDatos;
      StringChangeEx(NombreSQL, '''', '''''', True);

            Consulta := CreateOleObject('ADODB.Recordset');
      Consulta.Open(
        'SELECT CASE WHEN DB_ID(N''' + NombreSQL + ''') IS NULL ' +
        'THEN 1 ELSE 0 END AS Disponible',
        Conexion, 0, 1, 1);

      Result := (Consulta.Fields.Item[0].Value = 1);
      Consulta.Close;

      if Result then
        MsgBox(
          'El nombre "' + BaseDatos + '" está libre. Pulsa Aceptar para continuar.',
          mbInformation, MB_OK)
      else
        MsgBox(
          'Ya existe una base de datos con el nombre "' + BaseDatos + '".',
          mbInformation, MB_OK);
    finally
      Conexion.Close;
    end;
  except
    MsgBox(
      'No se ha podido comprobar el nombre en SQL Server.' + #13#10 +
      'Detalle: ' + GetExceptionMessage,
      mbError, MB_OK);
  end;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
var
  Servidor, BaseDatos: String;
begin
  Result := True;

  if CurPageID <> PaginaConexion.ID then
    Exit;

  Servidor := Trim(PaginaConexion.Values[0]);
  BaseDatos := Trim(PaginaConexion.Values[1]);

  PaginaConexion.Values[0] := Servidor;
  PaginaConexion.Values[1] := BaseDatos;

  if (Servidor = '') or (BaseDatos = '') then
  begin
    MsgBox('Introduce la instancia de SQL Server y el nombre de la base de datos.',
      mbInformation, MB_OK);
    Result := False;
    Exit;
  end;

    if PaginaBaseDatos.SelectedValueIndex = 1 then
    Result := ComprobarBaseExistente(Servidor, BaseDatos)
  else
    Result := ComprobarNombreDisponible(Servidor, BaseDatos);
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  Datos: TArrayOfString;
  Codigo: Integer;
  Carpeta, PowerShell, Argumentos, ArchivoError: String;
  Detalle: TArrayOfString;
begin
  Result := '';

  { La segunda opción utiliza una base que ya existe. }
  if PaginaBaseDatos.SelectedValueIndex = 1 then
    Exit;

  try
    Carpeta := ExpandConstant('{tmp}');
    ExtractTemporaryFile('GenteFit_Inicial.bak');
    ExtractTemporaryFile('RestaurarBase.sql');
    ExtractTemporaryFile('RestaurarBase.ps1');

    SetArrayLength(Datos, 2);
    Datos[0] := PaginaConexion.Values[0];
    Datos[1] := PaginaConexion.Values[1];

    if not SaveStringsToUTF8File(Carpeta + '\conexion.txt', Datos, False) then
    begin
      Result := 'No se han podido preparar los datos de conexión.';
      Exit;
    end;

    ArchivoError := Carpeta + '\error-restauracion.txt';
    DeleteFile(ArchivoError);

    PowerShell := ExpandConstant(
      '{sys}\WindowsPowerShell\v1.0\powershell.exe');
    Argumentos := '-NoProfile -NonInteractive -ExecutionPolicy RemoteSigned -File "' +
      Carpeta + '\RestaurarBase.ps1"';

    if not Exec(PowerShell, Argumentos, Carpeta, SW_HIDE,
      ewWaitUntilTerminated, Codigo) then
    begin
      Result := 'No se ha podido iniciar Windows PowerShell.';
      Exit;
    end;

    if Codigo <> 0 then
    begin
      Result := 'No se ha podido crear la base de datos.';
      if LoadStringsFromFile(ArchivoError, Detalle) then
        if GetArrayLength(Detalle) > 0 then
          Result := Result + ' ' + Detalle[0];
    end;
  except
    Result := 'Error al preparar la restauración. ' + GetExceptionMessage;
  end;
end;