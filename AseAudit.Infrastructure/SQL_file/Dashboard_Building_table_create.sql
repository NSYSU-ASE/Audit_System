CREATE TABLE [dbo].[Building]
(
    BuildingId      INT IDENTITY(1,1) PRIMARY KEY,
    BuildingCode    NVARCHAR(50)  NOT NULL UNIQUE,
    BuildingName    NVARCHAR(100) NOT NULL,
    AreaId          INT NOT NULL,
    OwnerName       NVARCHAR(100) NULL,
    CONSTRAINT FK_Building_Area FOREIGN KEY (AreaId) REFERENCES [dbo].[Area](AreaId)
);
GO

CREATE INDEX IX_Building_AreaId ON [dbo].[Building](AreaId);
GO
