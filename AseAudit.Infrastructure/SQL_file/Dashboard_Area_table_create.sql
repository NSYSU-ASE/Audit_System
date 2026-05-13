CREATE TABLE [dbo].[Area]
(
    AreaId      INT IDENTITY(1,1) PRIMARY KEY,
    AreaCode    NVARCHAR(50)  NOT NULL UNIQUE,
    AreaName    NVARCHAR(100) NOT NULL,
    OwnerName   NVARCHAR(100) NULL
);
GO
