USE [CAFEBOOKDB_v2]
GO

-- 1. Thêm các cột mới vào bảng KhuyenMai
ALTER TABLE KhuyenMai
ADD TrangThai NVARCHAR(50) NOT NULL DEFAULT N'Hoạt động', -- Hoạt động, Tạm dừng, Hết hạn
    GiamToiDa DECIMAL(18, 2) NULL,
    IdSanPhamApDung INT NULL,
    HoaDonToiThieu DECIMAL(18, 2) NULL,
    GioBatDau TIME NULL,
    GioKetThuc TIME NULL,
    NgayTrongTuan NVARCHAR(50) NULL;
GO

-- 2. Thêm khóa ngoại cho sản phẩm áp dụng
ALTER TABLE [dbo].[KhuyenMai] WITH CHECK ADD CONSTRAINT [FK_KhuyenMai_SanPham] FOREIGN KEY([IdSanPhamApDung])
REFERENCES [dbo].[SanPham] ([idSanPham]);
GO