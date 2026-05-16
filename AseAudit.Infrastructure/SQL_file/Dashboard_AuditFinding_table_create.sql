CREATE TABLE [dbo].[AuditFinding]
(
    FindingId       INT IDENTITY(1,1) PRIMARY KEY,
    AuditResultId   INT NOT NULL,
    FRCode          NVARCHAR(10) NOT NULL,
    Reason          NVARCHAR(MAX) NULL,
    CONSTRAINT FK_AuditFinding_AuditResult FOREIGN KEY (AuditResultId) REFERENCES [dbo].[AuditResult](AuditResultId)
);
GO

CREATE INDEX IX_AuditFinding_AuditResultId ON [dbo].[AuditFinding](AuditResultId);
GO
