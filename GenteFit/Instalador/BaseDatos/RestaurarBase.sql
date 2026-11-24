-- El instalador proporcionará dos parámetros:
-- @NombreBase: nombre elegido por el usuario.
-- @RutaCopia: ruta del .bak accesible desde SQL Server.

IF DB_ID(@NombreBase) IS NOT NULL
BEGIN
    THROW 50001, 'Ya existe una base de datos con ese nombre.', 1;
END;

DECLARE @CarpetaDatos nvarchar(4000) =
    CONVERT(nvarchar(4000), SERVERPROPERTY('InstanceDefaultDataPath'));

DECLARE @CarpetaRegistro nvarchar(4000) =
    CONVERT(nvarchar(4000), SERVERPROPERTY('InstanceDefaultLogPath'));

IF NULLIF(@CarpetaDatos, N'') IS NULL
   OR NULLIF(@CarpetaRegistro, N'') IS NULL
BEGIN
    THROW 50002, 'No se han podido obtener las carpetas de datos de SQL Server.', 1;
END;

IF RIGHT(@CarpetaDatos, 1) <> N'\'
    SET @CarpetaDatos += N'\';

IF RIGHT(@CarpetaRegistro, 1) <> N'\'
    SET @CarpetaRegistro += N'\';

-- Cada restauración recibe sus propios archivos físicos.
DECLARE @ArchivoBase nvarchar(80) =
    N'GenteFit_' + CONVERT(nvarchar(36), NEWID());

DECLARE @ArchivoDatos nvarchar(4000) =
    @CarpetaDatos + @ArchivoBase + N'.mdf';

DECLARE @ArchivoRegistro nvarchar(4000) =
    @CarpetaRegistro + @ArchivoBase + N'_log.ldf';

RESTORE DATABASE @NombreBase
FROM DISK = @RutaCopia
WITH
    MOVE N'GenteFitDB_PruebaInstalacion' TO @ArchivoDatos,
    MOVE N'GenteFitDB_PruebaInstalacion_log' TO @ArchivoRegistro,
    RECOVERY;