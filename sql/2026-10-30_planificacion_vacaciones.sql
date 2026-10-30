-- 2026-10-30: Planificacion_Vacaciones + columnas de auditoria/saldo.
-- Idempotente. La BD local (ibr_management) ya tiene estos cambios aplicados.

IF OBJECT_ID(N'Planificacion_Vacaciones', N'U') IS NULL
BEGIN
    CREATE TABLE Planificacion_Vacaciones (
        Id INT IDENTITY(1,1) PRIMARY KEY,

        -- Llave Foránea (Solo el empleado al que se le planifica)
        EmployeeID INT NOT NULL,

        -- Datos de la Planificación
        FechaInicioPropuesta DATE NOT NULL,
        FechaFinPropuesta DATE NOT NULL,
        DiasPropuestos DECIMAL(10,2) NOT NULL,

        -- Control del Flujo
        -- Valores: 'Planificado', 'Solicitado en Buk', 'Aprobado en Buk', 'Cancelado'
        Estado VARCHAR(50) NOT NULL DEFAULT 'Planificado',

        Observaciones NVARCHAR(MAX) NULL,

        -- Auditoría
        CreatedAt DATETIME DEFAULT GETDATE(),
        UpdatedAt DATETIME DEFAULT GETDATE(),

        -- Restricciones
        FOREIGN KEY (EmployeeID) REFERENCES Empleados(EmployeeID)
    );
END
GO

IF COL_LENGTH('Planificacion_Vacaciones', 'CreatedByEmail') IS NULL
    ALTER TABLE Planificacion_Vacaciones ADD CreatedByEmail VARCHAR(150) NULL;
GO

IF COL_LENGTH('Planificacion_Vacaciones', 'CreatedByEmployeeID') IS NULL
    ALTER TABLE Planificacion_Vacaciones ADD CreatedByEmployeeID INT NULL;
GO

-- Llave foránea para el creador
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Planificacion_Creador')
    ALTER TABLE Planificacion_Vacaciones
        ADD CONSTRAINT FK_Planificacion_Creador FOREIGN KEY (CreatedByEmployeeID) REFERENCES Empleados(EmployeeID);
GO

-- Ya no necesitamos el SaldoMinimoHistorico que restringía.
-- Solo necesitamos mantener el control de auditoría en las solicitudes.
IF COL_LENGTH('Solicitudes_Vacaciones', 'RequiereAuditoria') IS NULL
    ALTER TABLE Solicitudes_Vacaciones ADD RequiereAuditoria BIT DEFAULT 0;
GO

IF COL_LENGTH('Solicitudes_Vacaciones', 'FechaDeteccionAnulacion') IS NULL
    ALTER TABLE Solicitudes_Vacaciones ADD FechaDeteccionAnulacion DATETIME NULL;
GO

-- Saldo de Buk en el momento de la sincronización
IF COL_LENGTH('Empleados', 'SaldoActualBuk') IS NULL
    ALTER TABLE Empleados ADD SaldoActualBuk DECIMAL(10,2) NULL;
GO
