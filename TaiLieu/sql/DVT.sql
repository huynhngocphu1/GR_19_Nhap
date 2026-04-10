USE [CAFEBOOKDB_v2]
GO

-- 1. Đổi tên cột 'soLuong' trong DinhLuong
EXEC sp_rename 'DinhLuong.soLuong', 'SoLuongSuDung', 'COLUMN';
GO

-- 2. Thêm cột mới vào DinhLuong
ALTER TABLE DinhLuong
ADD idDonViSuDung INT NULL;
GO

-- 3. Tạo Bảng Đơn Vị Chuyển Đổi
CREATE TABLE DonViChuyenDoi (
    idChuyenDoi INT PRIMARY KEY IDENTITY,
    idNguyenLieu INT NOT NULL,
    TenDonVi NVARCHAR(50) NOT NULL,
    GiaTriQuyDoi DECIMAL(18, 6) NOT NULL, -- Giá trị để quy đổi về ĐVT gốc (vd: 1 gram = 0.001 kg)
    LaDonViCoBan BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_DonViChuyenDoi_NguyenLieu FOREIGN KEY (idNguyenLieu) REFERENCES NguyenLieu(idNguyenLieu) ON DELETE CASCADE
);
GO

-- 4. Thêm Khóa Ngoại cho DinhLuong
ALTER TABLE [dbo].[DinhLuong] WITH CHECK ADD CONSTRAINT [FK_DinhLuong_DonViChuyenDoi] FOREIGN KEY([idDonViSuDung])
REFERENCES [dbo].[DonViChuyenDoi] ([idChuyenDoi]);
GO

PRINT N'Cập nhật CSDL cho Định Lượng (v2) thành công.';