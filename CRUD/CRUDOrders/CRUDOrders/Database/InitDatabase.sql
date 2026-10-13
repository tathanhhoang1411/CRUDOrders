

-- 1. TAO DATABASE 
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'CRUDOrdersDb')
BEGIN
    CREATE DATABASE [CRUDOrdersDb];
    PRINT N'-> Da tao database CRUDOrdersDb thanh cong.';
END
ELSE
BEGIN
    PRINT N'-> Database CRUDOrdersDb da ton tai.';
END
GO

USE [CRUDOrdersDb];
GO

-- ============================================================================
-- 2. BANG USERS (Nguoi dung & Phan quyen Admin / Staff)
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'Users')
BEGIN
    CREATE TABLE [dbo].[Users] (
        [Id]           BIGINT         NOT NULL,
        [Username]     NVARCHAR(100)  NOT NULL,
        [Email]        NVARCHAR(255)  NOT NULL,
        [PasswordHash] NVARCHAR(MAX)  NOT NULL,
        [Role]         NVARCHAR(20)   NOT NULL CONSTRAINT [DF_Users_Role] DEFAULT ('Staff'),
        [IsActive]     BIT            NOT NULL CONSTRAINT [DF_Users_IsActive] DEFAULT (1),
        [CreatedAt]    DATETIME2(7)   NOT NULL CONSTRAINT [DF_Users_CreatedAt] DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
        [UpdatedAt]    DATETIME2(7)   NULL,
        [IsDeleted]    BIT            NOT NULL CONSTRAINT [DF_Users_IsDeleted] DEFAULT (0),
        CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    -- Indexes cho Users
    CREATE UNIQUE NONCLUSTERED INDEX [IX_Users_Username] ON [dbo].[Users]([Username] ASC) WHERE [IsDeleted] = 0;
    CREATE UNIQUE NONCLUSTERED INDEX [IX_Users_Email] ON [dbo].[Users]([Email] ASC) WHERE [IsDeleted] = 0;
    CREATE NONCLUSTERED INDEX [IX_Users_Role] ON [dbo].[Users]([Role] ASC);

    PRINT N'-> Da tao bang Users va cac Index.';
END
GO

-- ============================================================================
-- 3. BANG PRODUCTS (Quan ly san pham & ton kho)
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'Products')
BEGIN
    CREATE TABLE [dbo].[Products] (
        [Id]          BIGINT          NOT NULL,
        [Code]        NVARCHAR(50)    NOT NULL,
        [Name]        NVARCHAR(200)   NOT NULL,
        [Description] NVARCHAR(MAX)   NULL,
        [Price]       DECIMAL(18, 2)  NOT NULL,
        [Stock]       INT             NOT NULL CONSTRAINT [DF_Products_Stock] DEFAULT (0),
        [RowVersion]  ROWVERSION      NOT NULL, -- Concurrency Token chong overselling
        [CreatedAt]   DATETIME2(7)    NOT NULL CONSTRAINT [DF_Products_CreatedAt] DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
        [UpdatedAt]   DATETIME2(7)    NULL,
        [IsDeleted]   BIT             NOT NULL CONSTRAINT [DF_Products_IsDeleted] DEFAULT (0),
        CONSTRAINT [PK_Products] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [CK_Products_Price] CHECK ([Price] >= 0),
        CONSTRAINT [CK_Products_Stock] CHECK ([Stock] >= 0)
    );

    -- Indexes cho Products
    CREATE UNIQUE NONCLUSTERED INDEX [IX_Products_Code] ON [dbo].[Products]([Code] ASC) WHERE [IsDeleted] = 0;
    CREATE NONCLUSTERED INDEX [IX_Products_Name] ON [dbo].[Products]([Name] ASC);
    CREATE NONCLUSTERED INDEX [IX_Products_IsDeleted] ON [dbo].[Products]([IsDeleted] ASC);

    PRINT N'-> Da tao bang Products va cac Index (kem RowVersion).';
END
GO

-- ============================================================================
-- 4. BANG ORDERS (Don hang)
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'Orders')
BEGIN
    CREATE TABLE [dbo].[Orders] (
        [Id]        BIGINT         NOT NULL,
        [UserId]    BIGINT         NOT NULL,
        [Status]    NVARCHAR(50)   NOT NULL CONSTRAINT [DF_Orders_Status] DEFAULT ('Pending'),
        [Total]     DECIMAL(18, 2) NOT NULL CONSTRAINT [DF_Orders_Total] DEFAULT (0),
        [Note]      NVARCHAR(MAX)  NULL,
        [CreatedAt] DATETIME2(7)   NOT NULL CONSTRAINT [DF_Orders_CreatedAt] DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
        [UpdatedAt] DATETIME2(7)   NULL,
        [IsDeleted] BIT            NOT NULL CONSTRAINT [DF_Orders_IsDeleted] DEFAULT (0),
        CONSTRAINT [PK_Orders] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_Orders_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id])
    );

    -- Indexes cho Orders
    CREATE NONCLUSTERED INDEX [IX_Orders_UserId] ON [dbo].[Orders]([UserId] ASC);
    CREATE NONCLUSTERED INDEX [IX_Orders_Status] ON [dbo].[Orders]([Status] ASC);
    CREATE NONCLUSTERED INDEX [IX_Orders_CreatedAt] ON [dbo].[Orders]([CreatedAt] DESC);

    PRINT N'-> Da tao bang Orders va cac Foreign Key / Index.';
END
GO

-- ============================================================================
-- 5. BANG ORDERITEMS (Chi tiet mat hang trong don)
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'OrderItems')
BEGIN
    CREATE TABLE [dbo].[OrderItems] (
        [Id]        BIGINT         NOT NULL,
        [OrderId]   BIGINT         NOT NULL,
        [ProductId] BIGINT         NOT NULL,
        [Quantity]  INT            NOT NULL,
        [UnitPrice] DECIMAL(18, 2) NOT NULL,
        [CreatedAt] DATETIME2(7)   NOT NULL CONSTRAINT [DF_OrderItems_CreatedAt] DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
        [UpdatedAt] DATETIME2(7)   NULL,
        [IsDeleted] BIT            NOT NULL CONSTRAINT [DF_OrderItems_IsDeleted] DEFAULT (0),
        CONSTRAINT [PK_OrderItems] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_OrderItems_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Orders]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_OrderItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products]([Id]),
        CONSTRAINT [CK_OrderItems_Quantity] CHECK ([Quantity] > 0),
        CONSTRAINT [CK_OrderItems_UnitPrice] CHECK ([UnitPrice] >= 0)
    );

    -- Indexes cho OrderItems
    CREATE NONCLUSTERED INDEX [IX_OrderItems_OrderId] ON [dbo].[OrderItems]([OrderId] ASC);
    CREATE NONCLUSTERED INDEX [IX_OrderItems_ProductId] ON [dbo].[OrderItems]([ProductId] ASC);

    PRINT N'-> Da tao bang OrderItems va cac Foreign Key / Index.';
END
GO

-- ============================================================================
-- 6. DU LIEU MAU KHOI TAO (SEED DATA)
-- PasswordHash admin: admin123 (SHA-256 Base64: JAvlGPq9JyTdtvBO6x2llnRI1+gxwIyPqCKAn3THIKk=)
-- PasswordHash staff: staff123 (SHA-256 Base64: EBdue3sk0xes/PjSBkz9LyThVPe1qWYDB31e+BPWprY=)
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Username] = 'admin')
BEGIN
    INSERT INTO [dbo].[Users] ([Id], [Username], [Email], [PasswordHash], [Role], [IsActive])
    VALUES (
        1,
        N'admin',
        N'admin@example.com',
        N'JAvlGPq9JyTdtvBO6x2llnRI1+gxwIyPqCKAn3THIKk=', -- admin123
        N'Admin',
        1
    );

    INSERT INTO [dbo].[Users] ([Id], [Username], [Email], [PasswordHash], [Role], [IsActive])
    VALUES (
        2,
        N'staff',
        N'staff@example.com',
        N'EBdue3sk0xes/PjSBkz9LyThVPe1qWYDB31e+BPWprY=', -- staff123
        N'Staff',
        1
    );

    PRINT N'-> Da them tai khoan mau: admin (pass: admin123) va staff (pass: staff123).';
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[Products] WHERE [Code] = 'PROD001')
BEGIN
    INSERT INTO [dbo].[Products] ([Id], [Code], [Name], [Description], [Price], [Stock])
    VALUES 
        (101, N'PROD001', N'Laptop Dell XPS 15', N'Laptop chuyen nghiep Intel Core i7 32GB RAM', 25000000.00, 20),
        (102, N'PROD002', N'Chuot khong day Logitech MX Master 3S', N'Chuot quang cao cap cong thai hoc', 1850000.00, 100),
        (103, N'PROD003', N'Ban phim co Keychron K8 Pro', N'Ban phim co layout tenkeyless, switch Gateron Pro', 2200000.00, 50);

    PRINT N'-> Da them du lieu san pham mau.';
END
GO

PRINT N'============================================================';
PRINT N'HOAN TAT KHOI TAO CSDL CHO CRUDOrdersDb!';
PRINT N'============================================================';
