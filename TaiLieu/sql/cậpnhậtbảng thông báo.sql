USE [CAFEBOOKDB_v2]
GO

-- 1. TẠO BẢNG THONGBAO --
CREATE TABLE [dbo].[ThongBao](
    [idThongBao] INT IDENTITY(1,1) NOT NULL,
    [idNhanVienTao] INT NULL,
    [NoiDung] NVARCHAR(500) NOT NULL,
    [ThoiGianTao] DATETIME NOT NULL DEFAULT GETDATE(),
    [LoaiThongBao] NVARCHAR(50) NULL,
    [IdLienQuan] INT NULL,
    [DaXem] BIT NOT NULL DEFAULT 0,
    
    PRIMARY KEY CLUSTERED ([idThongBao] ASC),
    
    CONSTRAINT [FK_ThongBao_NhanVien] FOREIGN KEY([idNhanVienTao])
    REFERENCES [dbo].[NhanVien] ([idNhanVien])
);
GO

PRINT N'Tạo bảng [ThongBao] thành công.';