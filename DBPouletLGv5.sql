-- Tạo cơ sở dữ liệu
CREATE DATABASE DBPouletLGv5;
GO

USE DBPouletLGv5;
GO

-- Bảng phân quyền
CREATE TABLE Roles (
    RoleId INT PRIMARY KEY IDENTITY(1,1),
    RoleName NVARCHAR(50) UNIQUE NOT NULL
);
-- Bảng người dùng
CREATE TABLE Users (
    UserId INT PRIMARY KEY IDENTITY(1,1),
    FullName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100) UNIQUE NOT NULL,
	Phone VARCHAR(10) NULL,
	Gender NVARCHAR(10) NULL,
    DateOfBirth DATETIME NULL,
    UserPassword NVARCHAR(100) NULL,
    RoleId INT NOT NULL,
    UserStatus NVARCHAR(50) DEFAULT N'Hoạt động' CHECK (UserStatus IN (N'Hoạt động', N'Không hoạt động')),
    CreatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (RoleId) REFERENCES Roles(RoleId)
);

-- Tìm các ràng buộc CHECK trên bảng Users
SELECT name
FROM sys.check_constraints
WHERE parent_object_id = OBJECT_ID('Users');

ALTER TABLE Users
DROP CONSTRAINT CK__Users__UserStatu__3C69FB99;

ALTER TABLE Users
ADD CONSTRAINT CK__Users__UserStatu__3C69FB99 CHECK (UserStatus IN (N'Hoạt động', N'Tạm khóa'));

-- Bảng hồ sơ khách hàng (riêng role 'Customer')
CREATE TABLE CustomerProfiles (
    CustomerId INT PRIMARY KEY,
	DiscountCode NVARCHAR(20) NULL,
    CustomerRank NVARCHAR(20) CHECK (CustomerRank IN (N'Đồng', N'Bạc', N'Vàng')),
    FOREIGN KEY (CustomerId) REFERENCES Users(UserId)
);
-- Bảng danh mục
CREATE TABLE Categories (
    CategoryId INT PRIMARY KEY IDENTITY(1,1),
    CategoryName NVARCHAR(100) NOT NULL,
	CreatedBy INT NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (CreatedBy) REFERENCES Users(UserId)
);
ALTER TABLE Categories
ADD 
    CategoryDescription NVARCHAR(MAX) NULL,
    ImageUrl NVARCHAR(255) NULL,
    BackgroundColor NVARCHAR(20) NULL,
    ButtonColor NVARCHAR(20) NULL;

-- Bảng sản phẩm
CREATE TABLE Products (
    ProductId INT PRIMARY KEY IDENTITY(1,1),
    ProductName NVARCHAR(100) NOT NULL,
    ProductDes NVARCHAR(MAX),
    Price DECIMAL(10,2) NOT NULL,
    AgeRange NVARCHAR(50),
    PieceCount INT,
    Rating DECIMAL(2,1) DEFAULT 0,
    Sold INT DEFAULT 0,
    StockQuantity INT DEFAULT 0,
    IsFeatured BIT DEFAULT 0,
    ProductStatus NVARCHAR(50) DEFAULT N'Hoạt động' CHECK (ProductStatus IN (N'Hoạt động', N'Không hoạt động')),
    CategoryId INT,
    CreatedAt DATETIME DEFAULT GETDATE(),
	DiscountPrice DECIMAL(10,2) NULL,
    CreatedBy INT NOT NULL,
    FOREIGN KEY (CategoryId) REFERENCES Categories(CategoryId),
    FOREIGN KEY (CreatedBy) REFERENCES Users(UserId)
);

CREATE TABLE Promotions (
    PromotionId INT PRIMARY KEY IDENTITY(1,1),
    PromotionName NVARCHAR(100) NOT NULL,
    StartDate DATETIME NOT NULL,
    EndDate DATETIME NOT NULL,
	 Status NVARCHAR(50) NULL,
    DiscountPercent DECIMAL(5,2) NOT NULL CHECK (DiscountPercent >= 0 AND DiscountPercent <= 100)
);

ALTER TABLE Products
ADD PromotionId INT NULL;

ALTER TABLE Products
ADD CONSTRAINT FK_Products_Promotions
FOREIGN KEY (PromotionId) REFERENCES Promotions(PromotionId); 



-- Bảng ảnh sản phẩm
CREATE TABLE ProductImages (
    ImageId INT PRIMARY KEY IDENTITY(1,1),
    ProductId INT,
    ImageUrl NVARCHAR(255),
    IsMain BIT DEFAULT 0,
    FOREIGN KEY (ProductId) REFERENCES Products(ProductId)
);


-- Bảng sản phẩm yêu thích
CREATE TABLE Favorites (
    UserId INT,
    ProductId INT,
    PRIMARY KEY (UserId, ProductId),
    FOREIGN KEY (UserId) REFERENCES Users(UserId),
    FOREIGN KEY (ProductId) REFERENCES Products(ProductId)
);

-- Bảng giỏ hàng
CREATE TABLE Cart (
    CartID INT PRIMARY KEY IDENTITY(1,1),
    UserID INT NOT NULL,
    ProductID INT NOT NULL,
    Quantity INT NOT NULL DEFAULT 1,
    AddedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (UserID) REFERENCES Users(UserID),
    FOREIGN KEY (ProductID) REFERENCES Products(ProductID),
    CONSTRAINT UQ_Cart_User_Product UNIQUE (UserID, ProductID)
);

-- Bảng địa chỉ người dùng
CREATE TABLE UserAddresses (
    AddressId INT PRIMARY KEY IDENTITY(1,1),
    UserId INT NOT NULL,
    FullName NVARCHAR(100) NOT NULL,
    Phone NVARCHAR(20) NOT NULL,
    Province NVARCHAR(100) NOT NULL,
    District NVARCHAR(100) NOT NULL,
    Ward NVARCHAR(100) NOT NULL,
    SpecificAddress NVARCHAR(255) NOT NULL,
    AddressType NVARCHAR(50) CHECK (AddressType IN (N'Nhà riêng', N'Văn phòng')),
    IsDefault BIT DEFAULT 0,
    CreatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);

-- Bảng đơn hàng
CREATE TABLE Orders (
    OrderId INT PRIMARY KEY IDENTITY(1,1),
    UserId INT NOT NULL,
    OrderDate DATETIME DEFAULT GETDATE(),
    OrderStatus NVARCHAR(50) DEFAULT N'Chờ xác nhận',
	ShippingFee DECIMAL(10,2) DEFAULT 15000,
    Discount DECIMAL(10,2) DEFAULT 0,
    TotalAmount DECIMAL(10,2),
    PaymentMethod NVARCHAR(50),
    PaymentStatus NVARCHAR(50) DEFAULT N'Chưa thanh toán' CHECK (PaymentStatus IN (N'Chưa thanh toán', N'Đã thanh toán')),
	AddressId INT NOT NULL,  -- địa chỉ giao hàng vào bảng Order
	VnpTransactionNo NVARCHAR(50),
	VnpTransactionDate DATETIME,
	CONSTRAINT FK_Orders_Users FOREIGN KEY (UserId) REFERENCES Users(UserId),
	CONSTRAINT FK_Orders_Addresses FOREIGN KEY (AddressId) REFERENCES UserAddresses(AddressId),
	CONSTRAINT CK_Orders_PaymentStatus CHECK (PaymentStatus IN (N'Chưa thanh toán', N'Đã thanh toán', N'Đã hoàn tiền'))
);

ALTER TABLE Orders
ADD 
    ShipperId INT NULL,
    CONSTRAINT FK_Orders_Shipper FOREIGN KEY (ShipperId) REFERENCES Users(UserId);

CREATE TABLE OrderDetails (
    OrderDetailId INT PRIMARY KEY IDENTITY(1,1),
    OrderId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(10,2) NOT NULL,
    TotalPrice AS (Quantity * UnitPrice) PERSISTED,
    FOREIGN KEY (OrderId) REFERENCES Orders(OrderId),
    FOREIGN KEY (ProductId) REFERENCES Products(ProductId)
);

-- Bảng đổi/trả sản phẩm
CREATE TABLE ProductReturns (
    ReturnId INT PRIMARY KEY IDENTITY(1,1),
    UserId INT NOT NULL,
    OrderId INT NOT NULL,
    RequestType NVARCHAR(50) CHECK (RequestType IN (N'Đổi sản phẩm', N'Hoàn tiền', N'Khác')),
    TotalRefundAmount DECIMAL(18, 2) DEFAULT 0,
    ImageUrl NVARCHAR(255),
    Note NVARCHAR(255),
    ReturnStatus NVARCHAR(50) DEFAULT N'Đang xử lý',
    RequestedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (OrderId) REFERENCES Orders(OrderId),
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);

CREATE TABLE ReturnDetails (
    ReturnDetailId INT PRIMARY KEY IDENTITY(1,1),
    ReturnId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity INT NOT NULL,
    Reason NVARCHAR(MAX),
    ReplacementProductId INT,
    FOREIGN KEY (ReturnId) REFERENCES ProductReturns(ReturnId),
    FOREIGN KEY (ProductId) REFERENCES Products(ProductId),
    FOREIGN KEY (ReplacementProductId) REFERENCES Products(ProductId)
);


-- Bảng đánh giá sản phẩm
CREATE TABLE ProductReviews (
    ReviewId INT PRIMARY KEY IDENTITY(1,1),
    UserId INT NOT NULL,
    ProductId INT NOT NULL,
    OrderId INT,
    Rating INT CHECK (Rating BETWEEN 1 AND 5),
    Comment NVARCHAR(MAX),
    ImageUrl NVARCHAR(255),
    CreatedAt DATETIME DEFAULT GETDATE(),
    IsFlagged BIT DEFAULT 0,
    ReviewStatus NVARCHAR(50) DEFAULT N'Chưa phản hồi',
    AdminReply NVARCHAR(MAX),
    AdminReplyAt DATETIME NULL,
    FOREIGN KEY (UserId) REFERENCES Users(UserId),
    FOREIGN KEY (ProductId) REFERENCES Products(ProductId),
    FOREIGN KEY (OrderId) REFERENCES Orders(OrderId)
);

ALTER TABLE ProductReviews
ADD CONSTRAINT UQ_ProductReviews_User_Product_Order UNIQUE (UserId, ProductId, OrderId);

ALTER TABLE ProductReviews
ADD CONSTRAINT CK_ProductReviews_ReviewStatus
CHECK (ReviewStatus IN (N'Chưa phản hồi', N'Đã phản hồi', N'Bị ẩn'));

ALTER TABLE ProductReviews
ADD IsUpdated BIT DEFAULT 0,
    UpdatedAt DATETIME NULL;

UPDATE ProductReviews SET IsFlagged = 0 WHERE IsFlagged IS NULL;

CREATE TABLE HomeBanners (
    BannerId INT PRIMARY KEY IDENTITY(1,1),
    ImageUrl NVARCHAR(255) NOT NULL, -- Đường dẫn ảnh
    IsActive BIT DEFAULT 1, -- Đang hoạt động hay không
    CreatedAt DATETIME DEFAULT GETDATE(),
    CreatedBy INT NOT NULL, -- Người thêm
    FOREIGN KEY (CreatedBy) REFERENCES Users(UserId)
);

CREATE TABLE ContactInformations (
    ContactId INT PRIMARY KEY IDENTITY(1,1),
    PhoneNumber NVARCHAR(20) NOT NULL, -- Số điện thoại
    Address NVARCHAR(500) NOT NULL,    -- Địa chỉ
    Email NVARCHAR(255) NOT NULL,      -- Email
    IsActive BIT DEFAULT 1,           -- Đang hoạt động hay không
    CreatedAt DATETIME DEFAULT GETDATE(),
    CreatedBy INT NOT NULL,           -- Người thêm
    FOREIGN KEY (CreatedBy) REFERENCES Users(UserId)
);

ALTER TABLE ContactInformations
ADD Latitude DECIMAL(9,6) NULL,
    Longitude DECIMAL(9,6) NULL;

-- Bảng thông tin trang Giới thiệu
CREATE TABLE AboutUsSections (
    SectionId INT PRIMARY KEY IDENTITY(1,1),
    Title NVARCHAR(100) NOT NULL, -- Tiêu đề của section
    Description NVARCHAR(MAX) NOT NULL, -- Mô tả của section
    ImageUrl NVARCHAR(255) NOT NULL, -- Đường dẫn ảnh
    IsActive BIT DEFAULT 1, -- Trạng thái hoạt động
    DisplayOrder INT NOT NULL, -- Thứ tự hiển thị
    CreatedAt DATETIME DEFAULT GETDATE(),
    CreatedBy INT NOT NULL, -- Người tạo
    FOREIGN KEY (CreatedBy) REFERENCES Users(UserId)
);

-- Bảng Contests: Lưu trữ thông tin về các cuộc thi
CREATE TABLE Contests (
    ContestId INT PRIMARY KEY IDENTITY(1,1), -- Mã cuộc thi
    Title NVARCHAR(100) NOT NULL, -- Tiêu đề cuộc thi
    Description NVARCHAR(MAX), -- Mô tả cuộc thi
    StartDate DATETIME NOT NULL, -- Ngày bắt đầu
    EndDate DATETIME NOT NULL, -- Ngày kết thúc
    CreatedAt DATETIME DEFAULT GETDATE(), -- Thời gian tạo
    CreatedBy INT NOT NULL, -- Người tạo (admin)
    IsActive BIT DEFAULT 1, -- Trạng thái hoạt động
    FOREIGN KEY (CreatedBy) REFERENCES Users(UserId) -- Khóa ngoại tới bảng Users
);

ALTER TABLE Contests
ADD RewardProductId INT NULL,
    ImageUrl NVARCHAR(255) NULL;

ALTER TABLE Contests
ADD ContestStatus NVARCHAR(50) NULL;

ALTER TABLE Contests
ADD CONSTRAINT FK_Contests_RewardProductId FOREIGN KEY (RewardProductId)
REFERENCES Products(ProductId);

-- Bảng ContestWinner: Lưu trữ thông tin người chiến thắng cuộc thi
CREATE TABLE ContestWinners (
    WinnerId INT PRIMARY KEY IDENTITY(1,1), -- Mã người chiến thắng
    ContestId INT NOT NULL, -- Mã cuộc thi
    UserId INT NOT NULL, -- Mã người dùng (người chiến thắng)
    RewardProductId INT NOT NULL, -- Mã sản phẩm thưởng
    OrderId INT NULL, -- Mã đơn hàng thưởng (nếu có)
    WonAt DATETIME DEFAULT GETDATE(), -- Thời gian chiến thắng
    Status NVARCHAR(50) DEFAULT N'Chưa gửi', -- Trạng thái gửi thưởng
    FOREIGN KEY (ContestId) REFERENCES Contests(ContestId), -- Khóa ngoại tới bảng Contests
    FOREIGN KEY (UserId) REFERENCES Users(UserId), -- Khóa ngoại tới bảng Users
    FOREIGN KEY (RewardProductId) REFERENCES Products(ProductId), -- Khóa ngoại tới bảng Products
    FOREIGN KEY (OrderId) REFERENCES Orders(OrderId), -- Khóa ngoại tới bảng Orders
    CONSTRAINT UQ_ContestWinners_Contest UNIQUE (ContestId) -- Ràng buộc duy nhất cho mỗi cuộc thi chỉ có một người chiến thắng
);

-- Bảng bài đăng cộng đồng: Lưu trữ các bài đăng chia sẻ sản phẩm từ khách hàng
CREATE TABLE CommunityPosts (
    PostId INT PRIMARY KEY IDENTITY(1,1), -- Mã bài đăng
    UserId INT NOT NULL, -- Mã người dùng
    OrderId INT NOT NULL, -- Mã đơn hàng
    ProductId INT NOT NULL, -- Mã sản phẩm
    ContestId INT NULL, -- Mã cuộc thi (nếu có)
    ImageUrl NVARCHAR(255) NOT NULL, -- Đường dẫn hình ảnh
    Description NVARCHAR(MAX), -- Mô tả bài đăng
    CreatedAt DATETIME DEFAULT GETDATE(), -- Thời gian tạo
    CommentCount INT DEFAULT 0, -- Số lượng bình luận
    IsFlagged BIT DEFAULT 0, -- Trạng thái đánh cờ (0: bình thường, 1: bị ẩn)
    FOREIGN KEY (UserId) REFERENCES Users(UserId), -- Khóa ngoại tới bảng Users
    FOREIGN KEY (OrderId) REFERENCES Orders(OrderId), -- Khóa ngoại tới bảng Orders
    FOREIGN KEY (ProductId) REFERENCES Products(ProductId), -- Khóa ngoại tới bảng Products
    FOREIGN KEY (ContestId) REFERENCES Contests(ContestId), -- Khóa ngoại tới bảng Contests
    CONSTRAINT UQ_CommunityPosts_Order_Product UNIQUE (OrderId, ProductId) -- Ràng buộc duy nhất cho cặp OrderId và ProductId
);

-- Bảng bình luận bài đăng cộng đồng: Lưu trữ các bình luận của người dùng trên bài đăng
CREATE TABLE CommunityComments (
    CommentId INT PRIMARY KEY IDENTITY(1,1), -- Mã bình luận
    PostId INT NOT NULL, -- Mã bài đăng
    UserId INT NOT NULL, -- Mã người dùng
    CommentText NVARCHAR(MAX) NOT NULL, -- Nội dung bình luận
    CreatedAt DATETIME DEFAULT GETDATE(), -- Thời gian tạo
    IsFlagged BIT DEFAULT 0, -- Trạng thái đánh cờ (0: bình thường, 1: bị ẩn)
    FOREIGN KEY (PostId) REFERENCES CommunityPosts(PostId), -- Khóa ngoại tới bảng CommunityPosts
    FOREIGN KEY (UserId) REFERENCES Users(UserId) -- Khóa ngoại tới bảng Users
);

-- Bảng lượt vote bài đăng cuộc thi: Lưu trữ các lượt vote cho bài đăng trong cuộc thi
CREATE TABLE ContestVotes (
    VoteId INT PRIMARY KEY IDENTITY(1,1), -- Mã lượt vote
    PostId INT NOT NULL, -- Mã bài đăng cuộc thi
    UserId INT NOT NULL, -- Mã người dùng
    CreatedAt DATETIME DEFAULT GETDATE(), -- Thời gian vote
    FOREIGN KEY (PostId) REFERENCES CommunityPosts(PostId), -- Khóa ngoại tới bảng CommunityPosts
    FOREIGN KEY (UserId) REFERENCES Users(UserId), -- Khóa ngoại tới bảng Users
    CONSTRAINT UQ_ContestVotes_Post_User UNIQUE (PostId, UserId) -- Ràng buộc duy nhất cho cặp PostId và UserId
);

ALTER TABLE CommunityPosts
ALTER COLUMN OrderId INT NULL;

ALTER TABLE CommunityPosts
ALTER COLUMN ProductId INT NULL;

SELECT COLUMN_NAME, IS_NULLABLE, DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'CommunityPosts' AND COLUMN_NAME = 'OrderId';

SELECT COLUMN_NAME, IS_NULLABLE, DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'CommunityPosts' AND COLUMN_NAME = 'ProductId';

SELECT name, type_desc
FROM sys.objects
WHERE parent_object_id = OBJECT_ID('CommunityPosts') AND type = 'UQ';

SELECT c.name AS column_name
FROM sys.indexes i
JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE i.object_id = OBJECT_ID('CommunityPosts') AND i.name = 'UQ_CommunityPosts_Order_Product';

ALTER TABLE CommunityPosts
DROP CONSTRAINT UQ_CommunityPosts_Order_Product;

INSERT INTO Contests (Title, Description, StartDate, EndDate, CreatedBy, RewardProductId, ImageUrl)
VALUES (
    N'Cuộc thi xây dựng LEGO sáng tạo 2025', -- Tiêu đề
    N'Mời các bạn tham gia cuộc thi xây dựng mô hình LEGO sáng tạo nhất. Hãy gửi bài dự thi của bạn!', -- Mô tả
    '2025-07-27 18:00:00', -- Ngày bắt đầu (hiện tại là 06:31 PM +07, 27/07/2025)
    '2025-08-10 23:59:59', -- Ngày kết thúc
    1, -- CreatedBy (ID của admin)
    3, -- RewardProductId (ID của sản phẩm phần thưởng)
    N'/images/contests/sample-contest-image.jpg' -- ImageUrl (đường dẫn mẫu)
);

UPDATE Contests SET ContestStatus = N'Đang diễn ra' WHERE ContestId = 3

select * from Roles
select * from Users
select * from CustomerProfiles
select * from Contests
select * from ContestWinners
select * from ContestVotes

delete Contests where ContestId = 6
delete ContestWinners where WinnerId = 2

select * from Orders
select * from OrderDetails
select * from CustomerProfiles

select * from CommunityPosts
select * from CommunityComments



SELECT ProductId, ProductName, Price FROM Products WHERE ProductId = 3;
SELECT ContestId, Title, RewardProductId FROM Contests WHERE RewardProductId = 3 AND IsActive = 1;
------------------------------------------------------------------
CREATE TRIGGER trg_ValidateCustomerRole
ON CustomerProfiles
INSTEAD OF INSERT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN Users u ON u.UserId = i.CustomerId
        JOIN Roles r ON r.RoleId = u.RoleId
        WHERE r.RoleName != 'Khách hàng'
    )
    BEGIN
        RAISERROR(N'Chỉ người dùng có vai trò Customer mới được tạo hồ sơ khách hàng.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END

    INSERT INTO CustomerProfiles (CustomerId, CustomerRank, DiscountCode)
    SELECT CustomerId, N'Đồng', NULL
    FROM inserted;
END;
GO
------------------------------------------------------------------
CREATE TRIGGER trg_UpdateRankAndDiscount
ON Orders
AFTER INSERT, UPDATE
AS
BEGIN
    -- Cập nhật Rank & DiscountCode cho mỗi khách hàng sau khi đơn hàng mới được tạo hoặc cập nhật
    -- Chỉ tính các đơn hàng có trạng thái 'Hoàn thành'
    UPDATE cp
    SET 
        CustomerRank = 
            CASE 
                WHEN o.TotalSpend >= 10000000 THEN N'Vàng'
                WHEN o.TotalSpend >= 5000000 THEN N'Bạc'
                ELSE N'Đồng'
            END,
        DiscountCode =
            CASE 
                WHEN o.TotalSpend >= 10000000 THEN N'GIAM10'
                WHEN o.TotalSpend >= 5000000 THEN N'GIAM5'
                ELSE NULL
            END
    FROM CustomerProfiles cp
    JOIN (
        SELECT o.UserId, SUM(o.TotalAmount) AS TotalSpend
        FROM Orders o
        WHERE o.OrderStatus = N'Hoàn thành'
        GROUP BY o.UserId
    ) o ON cp.CustomerId = o.UserId;
END;
GO
------------------------------------------------------------------
-- Trigger cập nhật trung bình đánh giá sản phẩm
CREATE OR ALTER TRIGGER trg_UpdateProductRating
ON ProductReviews
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE Products
    SET Rating = ISNULL((
        SELECT AVG(CAST(Rating AS FLOAT))
        FROM ProductReviews
        WHERE ProductReviews.ProductId = Products.ProductId
        AND ProductReviews.IsFlagged = 0
    ), 0)
    FROM Products
    WHERE ProductId IN (
        SELECT DISTINCT ProductId FROM inserted
        UNION
        SELECT DISTINCT ProductId FROM deleted
    );
END;
GO
------------------------------------------------------------------
CREATE OR ALTER TRIGGER trg_UpdateStock_OnOrderStatus
ON Orders
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Tăng Sold khi trạng thái thay đổi thành Hoàn thành
    UPDATE p
    SET p.Sold = ISNULL(p.Sold, 0) + od.TotalQuantity
    FROM Products p
    INNER JOIN (
        SELECT 
            od.ProductId,
            SUM(od.Quantity) AS TotalQuantity
        FROM OrderDetails od
        INNER JOIN inserted i ON od.OrderId = i.OrderId
        INNER JOIN deleted d ON i.OrderId = d.OrderId
        WHERE i.OrderStatus = N'Hoàn thành'
        AND d.OrderStatus <> N'Hoàn thành'
        GROUP BY od.ProductId
    ) od ON p.ProductId = od.ProductId;

    -- Tăng lại StockQuantity khi trạng thái thay đổi thành Đã hủy
    UPDATE p
    SET p.StockQuantity = p.StockQuantity + od.TotalQuantity
    FROM Products p
    INNER JOIN (
        SELECT 
            od.ProductId,
            SUM(od.Quantity) AS TotalQuantity
        FROM OrderDetails od
        INNER JOIN inserted i ON od.OrderId = i.OrderId
        INNER JOIN deleted d ON i.OrderId = d.OrderId
        WHERE i.OrderStatus = N'Đã hủy'
        AND d.OrderStatus <> N'Đã hủy'
        GROUP BY od.ProductId
    ) od ON p.ProductId = od.ProductId;
END;
---------------------------------------------------------------------------
select * from Roles

select * from Users

select * from UserAddresses

select * from Categories

select * from Products

select * from ProductImages

select * from Cart

select * from Favorites

select * from CustomerProfiles

select * from Orders

select * from OrderDetails

select * from ProductReviews

select * from HomeBanners

select * from ContactInformations

select * from AboutUsSections

select * from Contests

select * from ContestWinners

select * from CommunityPosts

select * from CommunityComments

delete from Contests

delete from ContactInformations

delete ProductReviews where ReviewId = 7

delete OrderDetails where OrderDetailId = 42
delete Orders where OrderId = 38

SELECT UserId, FullName, RoleId FROM Users WHERE RoleId = 3;
SELECT * FROM Roles WHERE RoleId = 3;

update Products set StockQuantity = 9 where ProductId = 3
update Products set StockQuantity = 0 where ProductId = 2

update Products set Rating = 3.9 where ProductId = 40

UPDATE Orders SET OrderDate = '2025-03-09' WHERE OrderId = 8;

UPDATE Orders SET OrderDate = DATEADD(hour, -24, GETDATE()) WHERE OrderId = 1

UPDATE Orders SET OrderStatus = N'Hoàn thành' WHERE OrderId = 6
 
DELETE FROM Categories;
DBCC CHECKIDENT ('Categories', RESEED, 0);

DELETE FROM ProductImages;
DBCC CHECKIDENT ('ProductImages', RESEED, 0);
DELETE FROM Products;
DBCC CHECKIDENT ('Products', RESEED, 0);

DELETE FROM Cart;
DBCC CHECKIDENT ('Cart', RESEED, 0);

DELETE FROM OrderDetails;
DBCC CHECKIDENT ('OrderDetails', RESEED, 0);
DELETE FROM Orders;
DBCC CHECKIDENT ('Orders', RESEED, 0);

DELETE FROM ProductReviews;
DBCC CHECKIDENT ('ProductReviews', RESEED, 0);
---------------------------------------------------------------------------
INSERT INTO Roles (RoleName) VALUES 
(N'Khách hàng'), 
(N'Nhân viên bán hàng'), 
(N'Quản lý'),
(N'Nhân viên giao hàng');

INSERT INTO Users (FullName, Email, Phone, Gender, DateOfBirth, UserPassword, RoleId)
VALUES 
    (N'Duy Uyen', 'ql@gmail.com', '0922222222', N'Nữ', '2004-11-04', '123456', 3),    
    (N'Kim Ngan', 'nv@gmail.com', '0933333333', N'Nữ', '2004-06-30', '123456', 2),
	(N'Vinh Hung', 'gh@gmail.com', '0955555555', N'Nam', '2004-05-17', '123456', 4),
	(N'Dinh Khoi', 'gh1@gmail.com', '0966666666', N'Nam', '2004-09-25', '123456', 4);

INSERT INTO Categories (CategoryName, CreatedBy) VALUES 
(N'Khu vườn kì diệu', 1), 
(N'Kỳ quan kiến trúc', 1), 
(N'Phép thuật Hogwarts', 1), 
(N'Đường đua siêu xe', 1), 
(N'Nhịp đập công trường', 1);

INSERT INTO Products (
    ProductName, ProductDes, Price, AgeRange, PieceCount, Rating, Sold, 
    StockQuantity, IsFeatured, ProductStatus, CategoryId, CreatedBy, DiscountPrice) 
VALUES 
(N'Xe máy Kawasaki Ninja H2R', N'Mô hình Kawasaki Ninja H2R với kiểu dáng khí động học và màu sắc nổi bật.', 2209000, N'10+', 643, 4.6, 115, 55, 0, N'Hoạt động', 4, 1, NULL),
(N'Nhà thờ Đức Bà Paris', N'Mô hình kiến trúc biểu tượng của Pháp', 5999000, N'18+', 4383, 4.9, 125, 50, 1, N'Hoạt động', 2, 1, NULL),
(N'Cây nhỏ', N'Cây nhỏ LEGO phù hợp cho những ai yêu thích cây cảnh.', 1299000, N'18+', 758, 4.4, 75, 50, 0, N'Hoạt động', 1, 1, NULL),
(N'Tàu tốc hành Hogwarts', N'Mô hình tàu tốc hành đưa học sinh đến Hogwarts.', 2599000, N'10+', 832, 4.8, 130, 45, 1, N'Hoạt động', 3, 1, NULL),
(N'Xe tải Burger', N'Mô hình xe tải bán đồ ăn nhanh với cửa mở, đầu bếp và thực đơn burger.', 519000, N'5+', 194, 4.6, 100, 60, 0, N'Hoạt động', 5, 1, NULL),
(N'Hoa lan mini', N'Mẫu hoa lan nhỏ xinh từ LEGO, lý tưởng để làm quà.', 779000, N'18+', 274, 4.6, 98, 60, 0, N'Hoạt động', 1, 1, NULL),
(N'Porsche 911', N'Mô hình xe thể thao Porsche 911 với thiết kế biểu tượng của hãng.', 4419000, N'18+', 1458, 4.8, 175, 40, 1, N'Hoạt động', 4, 1, NULL),
(N'Nhà Hagrid: Một chuyến viếng thăm bất ngờ', N'Ngôi nhà của Hagrid và cuộc ghé thăm thú vị.', 1949000, N'8+', 896, 4.7, 95, 60, 0, N'Hoạt động', 3, 1, NULL),
(N'Máy đào xây dựng màu vàng', N'Mô hình máy đào xây dựng với gầu xúc chi tiết, màu vàng đặc trưng.', 1429000, N'8+', 633, 4.7, 110, 45, 0, N'Hoạt động', 5, 1, NULL),
(N'Bó hoa hồng xinh xắn', N'Một bó hoa hồng LEGO tinh tế, món quà hoàn hảo cho người thân.', 1559000, N'18+', 749, 4.9, 375, 20, 1, N'Hoạt động', 1, 1, NULL),
(N'Tượng Nữ thần Tự do', N'Biểu tượng tự do của nước Mỹ', 3319000, N'16+', 1685, 4.8, 120, 40, 1, N'Hoạt động', 2, 1, NULL),
(N'Cuộc phiêu lưu Knight Bus™', N'Mô hình xe buýt 3 tầng màu tím đặc trưng trong phim.', 1299000, N'8+', 499, 4.6, 90, 40, 0, N'Hoạt động', 3, 1, NULL),
(N'Cần cẩu xây dựng di động màu vàng', N'Cần cẩu di động có thể nâng các chi tiết nhỏ và vận hành linh hoạt.', 2859000, N'9+', 1116, 4.9, 150, 35, 1, N'Hoạt động', 5, 1, NULL),
(N'Ferrari FXX K', N'Mô hình xe đua Ferrari FXX K với thiết kế khí động học đậm chất Ferrari.', 1689000, N'10+', 897, 4.7, 138, 50, 0, N'Hoạt động', 4, 1, NULL),
(N'Hoa Cúc', N'Sản phẩm LEGO tái hiện hình ảnh hoa cúc thanh tao.', 779000, N'18+', 278, 4.5, 85, 55, 0, N'Hoạt động', 1, 1, NULL),
(N'Lâu đài Himeji', N'Biểu tượng cổ kính của Nhật Bản', 4129000, N'18+', 2125, 4.7, 60, 30, 0, N'Hoạt động', 2, 1, NULL),
(N'Vespa 125', N'Mô hình Vespa cổ điển phong cách Ý cho các tín đồ yêu xe.', 2599000, N'18+', 1107, 4.5, 85, 60, 0, N'Hoạt động', 4, 1, NULL),
(N'Đồn cảnh sát', N'Bộ LEGO đồn cảnh sát cổ điển với các phòng giam và xe tuần tra.', 1819000, N'6+', 668, 4.7, 130, 45, 1, N'Hoạt động', 5, 1, NULL),
(N'Thành phố New York', N'Đường chân trời hiện đại và sống động', 1559000, N'12+', 598, 4.7, 72, 28, 0, N'Hoạt động', 2, 1, NULL),
(N'Cây Bonsai', N'Mẫu cây bonsai LEGO độc đáo, lý tưởng để trang trí.', 1299000, N'18+', 878, 4.9, 290, 25, 1, N'Hoạt động', 1, 1, NULL),
(N'Xe kéo cứu hộ hạng nặng có cần cẩu', N'Mô hình xe kéo cứu hộ chi tiết với cần cẩu và bánh xe lớn.', 2339000, N'8+', 793, 4.8, 120, 50, 1, N'Hoạt động', 5, 1, NULL),
(N'Kim tự tháp Giza vĩ đại', N'Kỳ quan huyền bí của Ai Cập', 3379000, N'18+', 1476, 4.6, 90, 35, 0, N'Hoạt động', 2, 1, NULL),
(N'The Burrow – Phiên bản dành cho nhà sưu tập', N'Ngôi nhà ấm cúng của gia đình Weasley được tái hiện sinh động.', 6749000, N'18+', 2405, 4.8, 170, 25, 1, N'Hoạt động', 3, 1, NULL),
(N'Bãi phế liệu với xe hơi', N'Tái hiện bãi phế liệu thực tế cùng các chi tiết như xe hơi, cần cẩu.', 2079000, N'7+', 871, 4.7, 90, 40, 0, N'Hoạt động', 5, 1, NULL),
(N'Siêu xe thể thao Lamborghini Revuelto', N'Phiên bản xe Lamborghini Revuelto cực chất với màu cam nổi bật.', 4930000, N'10+', 1135, 4.8, 190, 40, 1, N'Hoạt động', 4, 1, NULL),
(N'Hoa Lan', N'Mẫu hoa lan LEGO với màu sắc trang nhã.', 1299000, N'18+', 608, 4.7, 134, 30, 0, N'Hoạt động', 1, 1, NULL),
(N'Ngân hàng phù thủy Gringotts™ – Phiên bản dành cho nhà sưu tập', N'Mô hình chi tiết ngân hàng phù thủy nổi tiếng trong thế giới Harry Potter.', 11159000, N'18+', 4801, 4.9, 210, 30, 1, N'Hoạt động', 3, 1, NULL),
(N'Đài phun nước Trevi', N'Kiệt tác nước Ý giữa lòng Rome', 4199000, N'18+', 1880, 4.8, 88, 40, 0, N'Hoạt động', 2, 1, NULL),
(N'Cây Mandrake', N'Mô hình cây Mandrake kêu khóc được thiết kế ngộ nghĩnh.', 1819000, N'10+', 579, 4.5, 75, 50, 0, N'Hoạt động', 3, 1, NULL),
(N'Cuộc rượt đuổi của thuyền cảnh sát', N'Bộ đồ chơi hành động với thuyền cảnh sát và tên tội phạm bỏ trốn.', 909000, N'6+', 264, 4.6, 95, 40, 0, N'Hoạt động', 5, 1, NULL),
(N'Chiếc mũ phân loại biết nói™', N'Mũ phân loại nổi tiếng được tái hiện chân thực.', 2599000, N'18+', 561, 4.7, 110, 30, 1, N'Hoạt động', 3, 1, NULL),
(N'McLaren P1', N'Mô hình siêu xe McLaren P1 chi tiết với cửa cắt kéo đặc trưng.', 11679000, N'18+', 3893, 4.9, 245, 30, 1, N'Hoạt động', 4, 1, NULL),
(N'Xe bay Ford Anglia™', N'Mô hình xe bay huyền thoại do Ron và Harry sử dụng.', 389000, N'7+', 165, 4.4, 60, 100, 0, N'Hoạt động', 3, 1, NULL),
(N'Yamaha MT-10 SP', N'Chiếc mô tô Yamaha MT-10 SP phiên bản LEGO đầy mạnh mẽ.', 6229000, N'18+', 1478, 4.7, 95, 35, 1, N'Hoạt động', 4, 1, NULL),
(N'Vòng hoa', N'Vòng hoa LEGO đẹp mắt, dùng trang trí dịp lễ.', 2599000, N'18+', 1194, 4.9, 298, 18, 1, N'Hoạt động', 1, 1, NULL),
(N'Đường Privet: Chuyến thăm của cô Marge', N'Phân cảnh đáng nhớ tại nhà Dursley được tái hiện.', 2339000, N'8+', 639, 4.6, 80, 50, 0, N'Hoạt động', 3, 1, NULL),
(N'Xe điện và nhà ga trung tâm thành phố', N'Thiết kế trung tâm thành phố hiện đại với xe điện chạy bằng pin.', 2339000, N'7+', 811, 4.8, 115, 35, 1, N'Hoạt động', 5, 1, NULL),
(N'Bó hoa hồng', N'Bó hoa hồng LEGO sang trọng, thích hợp để tặng dịp đặc biệt.', 1559000, N'18+', 822, 4.8, 210, 40, 1, N'Hoạt động', 1, 1, NULL),
(N'Dòng xe Mercedes-Benz G 500 PROFESSIONAL', N'Mô hình dòng xe địa hình hạng sang Mercedes-Benz G-Class chi tiết cao.', 6489000, N'18+', 2891, 4.9, 210, 35, 1, N'Hoạt động', 4, 1, NULL),
(N'Cây hạnh phúc', N'Cây hạnh phúc LEGO tuyệt đẹp để trang trí không gian sống.', 599000, N'9+', 217, 4.8, 210, 35, 1, N'Hoạt động', 1, 1, NULL),
(N'Land Rover cổ điển Defender 90', N'Chiếc xe địa hình Land Rover Defender 90 với các chi tiết cơ khí chân thực.', 6229000, N'18+', 2336, 4.7, 132, 25, 0, N'Hoạt động', 4, 1, NULL),
(N'Luân Đôn', N'Thành phố mang phong cách cổ điển nước Anh', 1039000, N'12+', 468, 4.5, 45, 25, 0, N'Hoạt động', 2, 1, NULL),
(N'Xe Chevrolet Camaro Z28', N'Phiên bản cổ điển của dòng xe cơ bắp Chevrolet Camaro Z28.', 4419000, N'18+', 1456, 4.6, 120, 45, 0, N'Hoạt động', 4, 1, NULL),
(N'Hoa Mận', N'Sản phẩm LEGO mô phỏng hoa mận nhẹ nhàng và tinh tế.', 779000, N'18+', 327, 4.7, 112, 48, 0, N'Hoạt động', 1, 1, NULL),
(N'Máy bay chở khách', N'Mô hình máy bay dân dụng với khoang hành khách và phi công.', 3119000, N'7+', 913, 4.8, 175, 30, 1, N'Hoạt động', 5, 1, NULL),
(N'Tấm đường', N'Tấm đường LEGO mở rộng thành phố với các chi tiết đường phố.', 519000, N'5+', 112, 4.5, 85, 60, 0, N'Hoạt động', 5, 1, NULL),
(N'Paris', N'Thành phố lãng mạn với những kiến trúc tráng lệ', 1299000, N'12+', 649, 4.6, 51, 22, 0, N'Hoạt động', 2, 1, NULL),
(N'Lâu đài Hogwarts', N'Mô hình chi tiết lâu đài Hogwarts đầy ấn tượng.', 12199000, N'16+', 6020, 4.9, 250, 20, 1, N'Hoạt động', 3, 1, NULL);

INSERT INTO ProductImages (ProductId, ImageUrl, IsMain)
VALUES 
(40, N'/images/products/CayHanhPhuc1.jpg', 1),
(40, N'/images/products/CayHanhPhuc2.jpg', 0),
(40, N'/images/products/CayHanhPhuc3.jpg', 0),
(10, N'/images/products/BoHoaHongXinhXan1.jpg', 1),
(10, N'/images/products/BoHoaHongXinhXan2.jpg', 0),
(10, N'/images/products/BoHoaHongXinhXan3.jpg', 0),
(6, N'/images/products/HoaLanMini1.jpg', 1),
(6, N'/images/products/HoaLanMini2.jpg', 0),
(6, N'/images/products/HoaLanMini3.jpg', 0),
(44, N'/images/products/HoaMan1.jpg', 1),
(44, N'/images/products/HoaMan2.jpg', 0),
(44, N'/images/products/HoaMan3.jpg', 0),
(15, N'/images/products/HoaCuc1.jpg', 1),
(15, N'/images/products/HoaCuc2.jpg', 0),
(15, N'/images/products/HoaCuc3.jpg', 0),
(20, N'/images/products/CayBonsai1.jpg', 1),
(20, N'/images/products/CayBonsai2.jpg', 0),
(20, N'/images/products/CayBonsai3.jpg', 0),
(3, N'/images/products/CayNho1.jpg', 1),
(3, N'/images/products/CayNho2.jpg', 0),
(3, N'/images/products/CayNho3.jpg', 0),
(26, N'/images/products/HoaLan1.jpg', 1),
(26, N'/images/products/HoaLan2.jpg', 0),
(26, N'/images/products/HoaLan3.jpg', 0),
(38, N'/images/products/BoHoaHong1.jpg', 1),
(38, N'/images/products/BoHoaHong2.jpg', 0),
(38, N'/images/products/BoHoaHong3.jpg', 0),
(35, N'/images/products/VongHoa1.jpg', 1),
(35, N'/images/products/VongHoa2.jpg', 0),
(35, N'/images/products/VongHoa3.jpg', 0),
(2, N'/images/products/NhaThoDucBaParis1.jpg', 1),
(2, N'/images/products/NhaThoDucBaParis2.jpg', 0),
(2, N'/images/products/NhaThoDucBaParis3.jpg', 0),
(28, N'/images/products/DaiPhunNuocTrevi1.jpg', 1),
(28, N'/images/products/DaiPhunNuocTrevi2.jpg', 0),
(28, N'/images/products/DaiPhunNuocTrevi3.jpg', 0),
(16, N'/images/products/LauDaiHimeji1.jpg', 1),
(16, N'/images/products/LauDaiHimeji2.jpg', 0),
(16, N'/images/products/LauDaiHimeji3.jpg', 0),
(22, N'/images/products/KimTuThapGiza1.jpg', 1),
(22, N'/images/products/KimTuThapGiza2.jpg', 0),
(22, N'/images/products/KimTuThapGiza3.jpg', 0),
(11, N'/images/products/NuThanTuDo1.jpg', 1),
(11, N'/images/products/NuThanTuDo2.jpg', 0),
(11, N'/images/products/NuThanTuDo3.jpg', 0),
(42, N'/images/products/LuanDon1.jpg', 1),
(42, N'/images/products/LuanDon2.jpg', 0),
(42, N'/images/products/LuanDon3.jpg', 0),
(19, N'/images/products/NewYork1.jpg', 1),
(19, N'/images/products/NewYork2.jpg', 0),
(19, N'/images/products/NewYork3.jpg', 0),
(47, N'/images/products/Paris1.jpg', 1),
(47, N'/images/products/Paris2.jpg', 0),
(47, N'/images/products/Paris3.jpg', 0),
(27, N'/images/products/NganHangPhuThuyGringotts-PhienBanDanhChoNhaSuuTap1.jpg', 1),
(27, N'/images/products/NganHangPhuThuyGringotts-PhienBanDanhChoNhaSuuTap2.jpg', 0),
(27, N'/images/products/NganHangPhuThuyGringotts-PhienBanDanhChoNhaSuuTap3.jpg', 0),
(23, N'/images/products/TheBurrow-PhienBanDanhChoNhaSuuTap1.jpg', 1),
(23, N'/images/products/TheBurrow-PhienBanDanhChoNhaSuuTap2.jpg', 0),
(23, N'/images/products/TheBurrow-PhienBanDanhChoNhaSuuTap3.jpg', 0),
(12, N'/images/products/CuocPhieuLuuKnightBus1.jpg', 1),
(12, N'/images/products/CuocPhieuLuuKnightBus2.jpg', 0),
(12, N'/images/products/CuocPhieuLuuKnightBus3.jpg', 0),
(31, N'/images/products/ChiecMuPhanLoaiBietNoi1.jpg', 1),
(31, N'/images/products/ChiecMuPhanLoaiBietNoi2.jpg', 0),
(31, N'/images/products/ChiecMuPhanLoaiBietNoi3.jpg', 0),
(29, N'/images/products/CayMandrake1.jpg', 1),
(29, N'/images/products/CayMandrake2.jpg', 0),
(29, N'/images/products/CayMandrake3.jpg', 0),
(33, N'/images/products/XeBayFordAnglia1.jpg', 1),
(33, N'/images/products/XeBayFordAnglia2.jpg', 0),
(33, N'/images/products/XeBayFordAnglia3.jpg', 0),
(36, N'/images/products/DuongPrivetChuyenThamCuaCoMarge1.jpg', 1),
(36, N'/images/products/DuongPrivetChuyenThamCuaCoMarge2.jpg', 0),
(36, N'/images/products/DuongPrivetChuyenThamCuaCoMarge3.jpg', 0),
(8, N'/images/products/NhaHagridMotChuyenViengThamBatNgo1.jpg', 1),
(8, N'/images/products/NhaHagridMotChuyenViengThamBatNgo2.jpg', 0),
(8, N'/images/products/NhaHagridMotChuyenViengThamBatNgo3.jpg', 0),
(4, N'/images/products/TauTocHanhHogwarts1.jpg', 1),
(4, N'/images/products/TauTocHanhHogwarts2.jpg', 0),
(4, N'/images/products/TauTocHanhHogwarts3.jpg', 0),
(48, N'/images/products/LauDaiHogwarts1.jpg', 1),
(48, N'/images/products/LauDaiHogwarts2.jpg', 0),
(48, N'/images/products/LauDaiHogwarts3.jpg', 0),
(32, N'/images/products/McLarenP1-1.jpg', 1),
(32, N'/images/products/McLarenP1-2.jpg', 0),
(32, N'/images/products/McLarenP1-3.jpg', 0),
(14, N'/images/products/FerrariFXXK1.jpg', 1),
(14, N'/images/products/FerrariFXXK2.jpg', 0),
(14, N'/images/products/FerrariFXXK3.jpg', 0),
(25, N'/images/products/SieuXeTheThaoLamborghiniRevuelto1.jpg', 1),
(25, N'/images/products/SieuXeTheThaoLamborghiniRevuelto2.jpg', 0),
(25, N'/images/products/SieuXeTheThaoLamborghiniRevuelto3.jpg', 0),
(39, N'/images/products/DongXeMercedes-BenzG500Professional1.jpg', 1),
(39, N'/images/products/DongXeMercedes-BenzG500Professional2.jpg', 0),
(39, N'/images/products/DongXeMercedes-BenzG500Professional3.jpg', 0),
(43, N'/images/products/XeChevrolet CamaroZ28-1.jpg', 1),
(43, N'/images/products/XeChevrolet CamaroZ28-2.jpg', 0),
(43, N'/images/products/XeChevrolet CamaroZ28-3.jpg', 0),
(7, N'/images/products/Porsche911-1.jpg', 1),
(7, N'/images/products/Porsche911-2.jpg', 0),
(7, N'/images/products/Porsche911-3.jpg', 0),
(41, N'/images/products/LandRoverCoDienDefender90-1.jpg', 1),
(41, N'/images/products/LandRoverCoDienDefender90-2.jpg', 0),
(41, N'/images/products/LandRoverCoDienDefender90-3.jpg', 0),
(17, N'/images/products/Vespa125-1.jpg', 1),
(17, N'/images/products/Vespa125-2.jpg', 0),
(17, N'/images/products/Vespa125-3.jpg', 0),
(34, N'/images/products/YamahaMT-10SP-1.jpg', 1),
(34, N'/images/products/YamahaMT-10SP-2.jpg', 0),
(34, N'/images/products/YamahaMT-10SP-3.jpg', 0),
(1, N'/images/products/XeMayKawasakiNinjaH2R-1.jpg', 1),
(1, N'/images/products/XeMayKawasakiNinjaH2R-2.jpg', 0),
(1, N'/images/products/XeMayKawasakiNinjaH2R-3.jpg', 0),
(21, N'/images/products/XeKeoCuuHoHangNangCoCanCau1.jpg', 1),
(21, N'/images/products/XeKeoCuuHoHangNangCoCanCau2.jpg', 0),
(21, N'/images/products/XeKeoCuuHoHangNangCoCanCau3.jpg', 0),
(30, N'/images/products/CuocRuotDuoiCuaThuyenCanhSat1.jpg', 1),
(30, N'/images/products/CuocRuotDuoiCuaThuyenCanhSat2.jpg', 0),
(30, N'/images/products/CuocRuotDuoiCuaThuyenCanhSat3.jpg', 0),
(9, N'/images/products/MayDaoXayDungMauVang1.jpg', 1),
(9, N'/images/products/MayDaoXayDungMauVang2.jpg', 0),
(9, N'/images/products/MayDaoXayDungMauVang3.jpg', 0),
(13, N'/images/products/CanCauXayDungDiDongMauVang1.jpg', 1),
(13, N'/images/products/CanCauXayDungDiDongMauVang2.jpg', 0),
(13, N'/images/products/CanCauXayDungDiDongMauVang3.jpg', 0),
(46, N'/images/products/TamDuong1.jpg', 1),
(46, N'/images/products/TamDuong2.jpg', 0),
(46, N'/images/products/TamDuong3.jpg', 0),
(45, N'/images/products/MayBayChoKhach1.jpg', 1),
(45, N'/images/products/MayBayChoKhach2.jpg', 0),
(45, N'/images/products/MayBayChoKhach3.jpg', 0),
(24, N'/images/products/BaiPheLieuVoiXeHoi1.jpg', 1),
(24, N'/images/products/BaiPheLieuVoiXeHoi2.jpg', 0),
(24, N'/images/products/BaiPheLieuVoiXeHoi3.jpg', 0),
(5, N'/images/products/XeTaiBurger1.jpg', 1),
(5, N'/images/products/XeTaiBurger2.jpg', 0),
(5, N'/images/products/XeTaiBurger3.jpg', 0),
(37, N'/images/products/XeDienVaNhaGaTrungTamThanhPho1.jpg', 1),
(37, N'/images/products/XeDienVaNhaGaTrungTamThanhPho2.jpg', 0),
(37, N'/images/products/XeDienVaNhaGaTrungTamThanhPho3.jpg', 0),
(18, N'/images/products/DonCanhSat1.jpg', 1),
(18, N'/images/products/DonCanhSat2.jpg', 0),
(18, N'/images/products/DonCanhSat3.jpg', 0);
