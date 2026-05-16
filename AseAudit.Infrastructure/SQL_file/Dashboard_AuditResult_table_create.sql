CREATE TABLE [dbo].[AuditResult]
(
    AuditResultId   INT IDENTITY(1,1) PRIMARY KEY,
    DeviceId        NVARCHAR(50) NOT NULL,
    AuditPeriod     NVARCHAR(20) NOT NULL,
    IAM             INT NOT NULL DEFAULT 0,
    SWI             INT NOT NULL DEFAULT 0,
    FWL             INT NOT NULL DEFAULT 0,
    EVT             INT NOT NULL DEFAULT 0,
    AUD             INT NOT NULL DEFAULT 0,
    DAT             INT NOT NULL DEFAULT 0,
    RES             INT NOT NULL DEFAULT 0,
    TotalScore      AS ((IAM + SWI + FWL + EVT + AUD + DAT + RES) / 7),
    CreatedAt       DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_AuditResult_Device FOREIGN KEY (DeviceId) REFERENCES [dbo].[Device](DeviceId)
);
GO

CREATE INDEX IX_AuditResult_DeviceId_Period ON [dbo].[AuditResult](DeviceId, AuditPeriod);
GO
