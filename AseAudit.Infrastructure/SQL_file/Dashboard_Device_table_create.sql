CREATE TABLE [dbo].[Device]
(
    DeviceId        NVARCHAR(50) PRIMARY KEY,
    HostName        NVARCHAR(255) NULL,
    IP              NVARCHAR(50)  NULL,
    DeviceType      NVARCHAR(50)  NULL,
    BuildingId      INT NOT NULL,
    OwnerName       NVARCHAR(100) NULL,
    CONSTRAINT FK_Device_Building FOREIGN KEY (BuildingId) REFERENCES [dbo].[Building](BuildingId)
);
GO

CREATE INDEX IX_Device_BuildingId ON [dbo].[Device](BuildingId);
GO
