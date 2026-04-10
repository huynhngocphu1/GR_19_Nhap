/*
================================================================================
 KỊCH BẢN TẠO CẤU TRÚC CSDL CAFEBOOKDB PHIÊN BẢN NÂNG CẤP (v2.2.1)
================================================================================
 File này chỉ chứa cấu trúc (Schema): CREATE DATABASE, CREATE TABLE, ALTER TABLE.
 
 CẬP NHẬT (so với v2.2):
 - Chuyển đổi các cột SanPham.HinhAnh, Sach.AnhBia, NhanVien.AnhDaiDien
   sang NVARCHAR(MAX) để lưu trữ ảnh mã hóa (Base64).
================================================================================
*/

USE [master]
GO

/* 1. TẠO CƠ SỞ DỮ LIỆU */
IF DB_ID('CAFEBOOKDB_v2') IS NOT NULL
BEGIN
    ALTER DATABASE [CAFEBOOKDB_v2] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [CAFEBOOKDB_v2];
END
GO

CREATE DATABASE [CAFEBOOKDB_v2]
GO

USE [CAFEBOOKDB_v2]
GO

/*
================================================================================
 PHẦN 1: TẠO TẤT CẢ CÁC BẢNG (CHƯA CÓ KHÓA NGOẠI)
================================================================================
*/

-- Bảng Khu Vực
CREATE TABLE KhuVuc (
    idKhuVuc INT PRIMARY KEY IDENTITY,
    TenKhuVuc NVARCHAR(100) NOT NULL,
    MoTa NVARCHAR(500) NULL
);
GO

-- Bảng Bàn
CREATE TABLE Ban (
    idBan INT PRIMARY KEY IDENTITY,
    soBan NVARCHAR(50) NOT NULL,
    soGhe INT NOT NULL DEFAULT 2,
    trangThai NVARCHAR(50) NOT NULL DEFAULT N'Trống',
    ghiChu NVARCHAR(500) NULL,
    idKhuVuc INT NULL /* FK Sẽ được thêm ở PHẦN 2 */
);
GO

-- Bảng Người Giao Hàng
CREATE TABLE NguoiGiaoHang (
    idNguoiGiaoHang INT PRIMARY KEY IDENTITY,
    TenNguoiGiaoHang NVARCHAR(100) NOT NULL,
    SoDienThoai NVARCHAR(20) UNIQUE NOT NULL,
    TrangThai NVARCHAR(50) DEFAULT N'Sẵn sàng'
);
GO

-- Bảng Hóa Đơn
CREATE TABLE HoaDon (
    idHoaDon INT PRIMARY KEY IDENTITY,
    idBan INT NULL,
    idNhanVien INT NOT NULL, /* FK Sẽ được thêm ở PHẦN 2 */
    idKhachHang INT NULL,
    thoiGianTao DATETIME DEFAULT GETDATE(),
    thoiGianThanhToan DATETIME NULL,
    trangThai NVARCHAR(50) NOT NULL DEFAULT N'Chưa thanh toán',
    tongTienGoc DECIMAL(18, 2) DEFAULT 0,
    giamGia DECIMAL(18, 2) DEFAULT 0,
    TongPhuThu DECIMAL(18, 2) DEFAULT 0,
    thanhTien AS (tongTienGoc - giamGia + TongPhuThu),
    phuongThucThanhToan NVARCHAR(50) NULL,
    ghiChu NVARCHAR(MAX) NULL,
    LoaiHoaDon NVARCHAR(50) NOT NULL DEFAULT N'Tại quán' CHECK (LoaiHoaDon IN (N'Tại quán', N'Mang về', N'Giao hàng')),
    TrangThaiGiaoHang NVARCHAR(100) NULL,
    DiaChiGiaoHang NVARCHAR(500) NULL,
    SoDienThoaiGiaoHang NVARCHAR(20) NULL,
    idNguoiGiaoHang INT NULL
);
GO

-- Bảng Danh Mục Sản Phẩm
CREATE TABLE DanhMuc (
    idDanhMuc INT PRIMARY KEY IDENTITY,
    tenDanhMuc NVARCHAR(255) NOT NULL,
    idDanhMucCha INT NULL /* FK Tự tham chiếu, thêm ở PHẦN 2 */
);
GO

-- Bảng Sản Phẩm
CREATE TABLE SanPham (
    idSanPham INT PRIMARY KEY IDENTITY,
    tenSanPham NVARCHAR(255) NOT NULL,
    idDanhMuc INT NOT NULL, /* FK Sẽ được thêm ở PHẦN 2 */
    giaBan DECIMAL(18, 2) NOT NULL,
    moTa NTEXT NULL,
    trangThaiKinhDoanh BIT NOT NULL DEFAULT 1,
    HinhAnh NVARCHAR(MAX) NULL, /* <<< ĐÃ SỬA THÀNH MAX */
    NhomIn NVARCHAR(50) NULL
);
GO

-- Bảng Chi Tiết Hóa Đơn
CREATE TABLE ChiTietHoaDon (
    idChiTietHoaDon INT PRIMARY KEY IDENTITY,
    idHoaDon INT NOT NULL, /* FK Sẽ được thêm ở PHẦN 2 */
    idSanPham INT NOT NULL, /* FK Sẽ được thêm ở PHẦN 2 */
    soLuong INT NOT NULL,
    donGia DECIMAL(18, 2) NOT NULL,
    thanhTien AS (soLuong * donGia),
    ghiChu NVARCHAR(500) NULL
);
GO

-- Bảng Phụ Thu
CREATE TABLE PhuThu (
    idPhuThu INT PRIMARY KEY IDENTITY,
    TenPhuThu NVARCHAR(100) NOT NULL,
    GiaTri DECIMAL(18, 2) NOT NULL,
    LoaiGiaTri NVARCHAR(20) NOT NULL DEFAULT 'VND'
);
GO

-- Bảng Chi Tiết Phụ Thu Của Hóa Đơn
CREATE TABLE ChiTietPhuThuHoaDon (
    idHoaDon INT NOT NULL, /* FK Sẽ được thêm ở PHẦN 2 */
    idPhuThu INT NOT NULL, /* FK Sẽ được thêm ở PHẦN 2 */
    SoTien DECIMAL(18, 2) NOT NULL,
    PRIMARY KEY (idHoaDon, idPhuThu)
);
GO

-- Bảng Nhật Ký Hủy Món
CREATE TABLE NhatKyHuyMon (
    idNhatKy INT PRIMARY KEY IDENTITY,
    idHoaDon INT NOT NULL, /* FK Sẽ được thêm ở PHẦN 2 */
    idSanPham INT NOT NULL, /* FK Sẽ được thêm ở PHẦN 2 */
    SoLuongHuy INT NOT NULL,
    LyDo NVARCHAR(255) NOT NULL,
    idNhanVienHuy INT NOT NULL, /* FK Sẽ được thêm ở PHẦN 2 */
    ThoiGianHuy DATETIME DEFAULT GETDATE()
);
GO

-- Bảng Nguyên Liệu
CREATE TABLE NguyenLieu (
    idNguyenLieu INT PRIMARY KEY IDENTITY,
    tenNguyenLieu NVARCHAR(255) NOT NULL,
    donViTinh NVARCHAR(50) NOT NULL,
    tonKho DECIMAL(18, 2) NOT NULL DEFAULT 0,
    TonKhoToiThieu DECIMAL(18, 2) NOT NULL DEFAULT 0
);
GO

-- Bảng Định Lượng (Công thức)
CREATE TABLE DinhLuong (
    idSanPham INT NOT NULL, /* FK Sẽ được thêm ở PHẦN 2 */
    idNguyenLieu INT NOT NULL, /* FK Sẽ được thêm ở PHẦN 2 */
    soLuong DECIMAL(18, 2) NOT NULL,
    PRIMARY KEY (idSanPham, idNguyenLieu)
);
GO

-- Bảng Nhà Cung Cấp
CREATE TABLE NhaCungCap (
    idNhaCungCap INT PRIMARY KEY IDENTITY,
    tenNhaCungCap NVARCHAR(255) NOT NULL,
    soDienThoai NVARCHAR(20) NULL,
    diaChi NVARCHAR(500) NULL,
    email NVARCHAR(100) NULL
);
GO

-- Bảng Phiếu Nhập Kho
CREATE TABLE PhieuNhapKho (
    idPhieuNhapKho INT PRIMARY KEY IDENTITY,
    idNhaCungCap INT NULL, /* FK Sẽ được thêm ở PHẦN 2 */
    idNhanVien INT NOT NULL, /* FK Sẽ được thêm ở PHẦN 2 */
    ngayNhap DATETIME DEFAULT GETDATE(),
    tongTien DECIMAL(18, 2) DEFAULT 0,
    ghiChu NVARCHAR(500) NULL,
    TrangThai NVARCHAR(50) NOT NULL DEFAULT N'Đã hoàn thành'
);
GO

-- Bảng Chi Tiết Phiếu Nhập Kho
CREATE TABLE ChiTietNhapKho (
    idPhieuNhapKho INT NOT NULL, /* FK Sẽ được thêm ở PHẦN 2 */
    idNguyenLieu INT NOT NULL, /* FK Sẽ được thêm ở PHẦN 2 */
    soLuongNhap DECIMAL(18, 2) NOT NULL,
    donGiaNhap DECIMAL(18, 2) NOT NULL,
    thanhTien AS (soLuongNhap * donGiaNhap),
    PRIMARY KEY (idPhieuNhapKho, idNguyenLieu)
);
GO

-- Bảng Phiếu Kiểm Kho
CREATE TABLE PhieuKiemKho (
    idPhieuKiemKho INT PRIMARY KEY IDENTITY,
    idNhanVienKiem INT NOT NULL, /* FK Sẽ được thêm ở PHẦN 2 */
    NgayKiem DATETIME DEFAULT GETDATE(),
    GhiChu NVARCHAR(500) NULL,
    TrangThai NVARCHAR(50) NOT NULL DEFAULT N'Đang kiểm'
);
GO

-- Bảng Chi Tiết Kiểm Kho
CREATE TABLE ChiTietKiemKho (
    idPhieuKiemKho INT NOT NULL, /* FK Sẽ được thêm ở PHẦN 2 */
    idNguyenLieu INT NOT NULL, /* FK Sẽ được thêm ở PHẦN 2 */
    TonKhoHeThong DECIMAL(18, 2) NOT NULL,
    TonKhoThucTe DECIMAL(18, 2) NOT NULL,
    ChenhLech AS (TonKhoThucTe - TonKhoHeThong),
    LyDoChenhLech NVARCHAR(255) NULL,
    PRIMARY KEY (idPhieuKiemKho, idNguyenLieu)
);
GO

-- Bảng Phiếu Xuất Hủy
CREATE TABLE PhieuXuatHuy (
    idPhieuXuatHuy INT PRIMARY KEY IDENTITY,
    idNhanVienXuat INT NOT NULL, /* FK Sẽ được thêm ở PHẦN 2 */
    NgayXuatHuy DATETIME DEFAULT GETDATE(),
    LyDoXuatHuy NVARCHAR(500) NOT NULL,
    TongGiaTriHuy DECIMAL(18, 2) DEFAULT 0
);
GO

-- Bảng Chi Tiết Xuất Hủy
CREATE TABLE ChiTietXuatHuy (
    idPhieuXuatHuy INT NOT NULL, /* FK Sẽ được thêm ở PHẦN 2 */
    idNguyenLieu INT NOT NULL, /* FK Sẽ được thêm ở PHẦN 2 */
    SoLuong DECIMAL(18, 2) NOT NULL,
    DonGiaVon DECIMAL(18, 2) NOT NULL,
    ThanhTien AS (SoLuong * DonGiaVon),
    PRIMARY KEY (idPhieuXuatHuy, idNguyenLieu)
);
GO

-- Bảng Khách Hàng
CREATE TABLE KhachHang (
    idKhachHang INT PRIMARY KEY IDENTITY,
    hoTen NVARCHAR(255) NOT NULL,
    soDienThoai NVARCHAR(20) UNIQUE NULL,
    email NVARCHAR(100) UNIQUE NULL,
    diaChi NVARCHAR(500) NULL,
    diemTichLuy INT DEFAULT 0,
    tenDangNhap NVARCHAR(100) UNIQUE NULL,
    matKhau NVARCHAR(255) NULL,
    ngayTao DATETIME DEFAULT GETDATE()
);
GO

-- Bảng Phiếu Đặt Bàn
CREATE TABLE PhieuDatBan (
    idPhieuDatBan INT PRIMARY KEY IDENTITY,
    idKhachHang INT NULL, /* FK Sẽ được thêm ở PHẦN 2 */
    idBan INT NOT NULL, /* FK Sẽ được thêm ở PHẦN 2 */
    hoTenKhach NVARCHAR(100) NULL,
    sdtKhach NVARCHAR(20) NULL,
    thoiGianDat DATETIME NOT NULL,
    soLuongKhach INT NOT NULL,
    trangThai NVARCHAR(50) NOT NULL DEFAULT N'Đã xác nhận',
    ghiChu NVARCHAR(500) NULL
);
GO

-- Bảng Khuyến Mãi
CREATE TABLE KhuyenMai (
    idKhuyenMai INT PRIMARY KEY IDENTITY,
    maKhuyenMai NVARCHAR(50) UNIQUE NOT NULL,
    tenChuongTrinh NVARCHAR(255) NOT NULL,
    moTa NTEXT NULL,
    loaiGiamGia NVARCHAR(20) NOT NULL,
    giaTriGiam DECIMAL(18, 2) NOT NULL,
    ngayBatDau DATETIME NOT NULL,
    ngayKetThuc DATETIME NOT NULL,
    dieuKienApDung NVARCHAR(500) NULL,
    soLuongConLai INT NULL
);
GO

-- Bảng Liên Kết Hóa Đơn - Khuyến Mãi
CREATE TABLE HoaDon_KhuyenMai (
    idHoaDon INT NOT NULL, /* FK Sẽ được thêm ở PHẦN 2 */
    idKhuyenMai INT NOT NULL, /* FK Sẽ được thêm ở PHẦN 2 */
    PRIMARY KEY (idHoaDon, idKhuyenMai)
);
GO

-- Bảng Thể Loại Sách
CREATE TABLE TheLoai (
    idTheLoai INT PRIMARY KEY IDENTITY,
    tenTheLoai NVARCHAR(255) NOT NULL
);
GO

-- Bảng Tác Giả
CREATE TABLE TacGia (
    idTacGia INT PRIMARY KEY IDENTITY,
    tenTacGia NVARCHAR(255) NOT NULL,
    gioiThieu NTEXT NULL
);
GO

-- Bảng Nhà Xuất Bản
CREATE TABLE NhaXuatBan (
    idNhaXuatBan INT PRIMARY KEY IDENTITY,
    tenNhaXuatBan NVARCHAR(255) NOT NULL
);
GO

-- Bảng Sách
CREATE TABLE Sach (
    idSach INT PRIMARY KEY IDENTITY,
    tenSach NVARCHAR(500) NOT NULL,
    idTheLoai INT NULL, /* FK Sẽ được thêm ở PHẦN 2 */
    idTacGia INT NULL, /* FK Sẽ được thêm ở PHẦN 2 */
    idNhaXuatBan INT NULL, /* FK Sẽ được thêm ở PHẦN 2 */
    namXuatBan INT NULL,
    moTa NTEXT NULL,
    soLuongTong INT NOT NULL DEFAULT 1,
    soLuongHienCo INT NOT NULL DEFAULT 1,
    AnhBia NVARCHAR(MAX) NULL /* <<< ĐÃ SỬA THÀNH MAX */
);
GO

-- Bảng Phiếu Thuê Sách
CREATE TABLE PhieuThueSach (
    idPhieuThueSach INT PRIMARY KEY IDENTITY,
    idKhachHang INT NOT NULL, /* FK Sẽ được thêm ở PHẦN 2 */
    idNhanVien INT NOT NULL, /* FK Sẽ được thêm ở PHẦN 2 */
    ngayThue DATETIME DEFAULT GETDATE(),
    trangThai NVARCHAR(50) NOT NULL DEFAULT N'Đang thuê',
    tongTienCoc DECIMAL(18, 2) DEFAULT 0
);
GO

-- Bảng Chi Tiết Phiếu Thuê Sách
CREATE TABLE ChiTietPhieuThue (
    idPhieuThueSach INT NOT NULL, /* FK Sẽ được thêm ở PHẦN 2 */
    idSach INT NOT NULL, /* FK Sẽ được thêm ở PHẦN 2 */
    ngayHenTra DATETIME NOT NULL,
    ngayTraThucTe DATETIME NULL,
    tienCoc DECIMAL(18, 2) NOT NULL,
    TienPhatTraTre DECIMAL(18, 2) NULL DEFAULT 0,
    PRIMARY KEY (idPhieuThueSach, idSach)
);
GO

-- Bảng Vai Trò (Chức vụ)
CREATE TABLE VaiTro (
    idVaiTro INT PRIMARY KEY IDENTITY,
    tenVaiTro NVARCHAR(100) NOT NULL UNIQUE,
    moTa NVARCHAR(500) NULL
);
GO

-- Bảng Nhân Viên
CREATE TABLE NhanVien (
    idNhanVien INT PRIMARY KEY IDENTITY,
    hoTen NVARCHAR(255) NOT NULL,
    soDienThoai NVARCHAR(20) UNIQUE NOT NULL,
    email NVARCHAR(100) UNIQUE NULL,
    diaChi NVARCHAR(500) NULL,
    ngayVaoLam DATE NOT NULL,
    idVaiTro INT NOT NULL, /* FK Sẽ được thêm ở PHẦN 2 */
    luongCoBan DECIMAL(18, 2) DEFAULT 0,
    trangThaiLamViec NVARCHAR(50) NOT NULL DEFAULT N'Đang làm việc',
    tenDangNhap NVARCHAR(100) UNIQUE NOT NULL,
    matKhau NVARCHAR(255) NOT NULL,
    AnhDaiDien NVARCHAR(MAX) NULL /* <<< ĐÃ SỬA THÀNH MAX */
);
GO

-- Bảng Ca Làm Việc
CREATE TABLE CaLamViec (
    idCa INT PRIMARY KEY IDENTITY,
    tenCa NVARCHAR(100) NOT NULL,
    gioBatDau TIME NOT NULL,
    gioKetThuc TIME NOT NULL
);
GO

-- Bảng Lịch Làm Việc (Phân ca)
CREATE TABLE LichLamViec (
    idLichLamViec INT PRIMARY KEY IDENTITY,
    idNhanVien INT NOT NULL, /* FK Sẽ được thêm ở PHẦN 2 */
    idCa INT NOT NULL, /* FK Sẽ được thêm ở PHẦN 2 */
    ngayLam DATE NOT NULL,
    UNIQUE (idNhanVien, ngayLam)
);
GO

-- Bảng Chấm Công
CREATE TABLE BangChamCong (
    idChamCong INT PRIMARY KEY IDENTITY,
    idLichLamViec INT NOT NULL, /* FK Sẽ được thêm ở PHẦN 2 */
    gioVao DATETIME NULL,
    gioRa DATETIME NULL,
    soGioLam AS (DATEDIFF(MINUTE, gioVao, gioRa) / 60.0)
);
GO

-- Bảng Phiếu Lương
CREATE TABLE PhieuLuong (
    idPhieuLuong INT PRIMARY KEY IDENTITY,
    idNhanVien INT NOT NULL, /* FK Sẽ được thêm ở PHẦN 2 */
    thang INT NOT NULL,
    nam INT NOT NULL,
    luongCoBan DECIMAL(18, 2) NOT NULL,
    tongGioLam DECIMAL(18, 2) NOT NULL,
    tienThuong DECIMAL(18, 2) DEFAULT 0,
    khauTru DECIMAL(18, 2) DEFAULT 0,
    thucLanh DECIMAL(18, 2) NOT NULL,
    ngayTao DATETIME DEFAULT GETDATE(),
    trangThai NVARCHAR(50) NOT NULL DEFAULT N'Chưa thanh toán',
    UNIQUE (idNhanVien, thang, nam)
);
GO

-- Bảng Đơn Xin Nghỉ
CREATE TABLE DonXinNghi (
    idDonXinNghi INT PRIMARY KEY IDENTITY,
    idNhanVien INT NOT NULL, /* FK Sẽ được thêm ở PHẦN 2 */
    LoaiDon NVARCHAR(100) NOT NULL,
    LyDo NVARCHAR(500) NOT NULL,
    NgayBatDau DATETIME NOT NULL,
    NgayKetThuc DATETIME NOT NULL,
    TrangThai NVARCHAR(50) NOT NULL DEFAULT N'Chờ duyệt',
    idNguoiDuyet INT NULL, /* FK Sẽ được thêm ở PHẦN 2 */
    NgayDuyet DATETIME NULL,
    GhiChuPheDuyet NVARCHAR(255) NULL
);
GO

-- Bảng Cài Đặt (Hệ thống)
CREATE TABLE CaiDat (
    tenCaiDat NVARCHAR(100) PRIMARY KEY,
    giaTri NVARCHAR(MAX) NOT NULL,
    moTa NVARCHAR(500) NULL
);
GO

-- Bảng Quyền
CREATE TABLE Quyen (
    idQuyen NVARCHAR(100) PRIMARY KEY,
    TenQuyen NVARCHAR(255) NOT NULL,
    NhomQuyen NVARCHAR(100) NOT NULL
);
GO

-- Bảng Phân Quyền (VaiTro_Quyen)
CREATE TABLE VaiTro_Quyen (
    idVaiTro INT NOT NULL, /* FK Sẽ được thêm ở PHẦN 2 */
    idQuyen NVARCHAR(100) NOT NULL, /* FK Sẽ được thêm ở PHẦN 2 */
    PRIMARY KEY (idVaiTro, idQuyen)
);
GO

-- Bảng Giao Dịch Thanh Toán
CREATE TABLE GiaoDichThanhToan (
    idGiaoDich INT PRIMARY KEY IDENTITY,
    idHoaDon INT NOT NULL, /* FK Sẽ được thêm ở PHẦN 2 */
    MaGiaoDichNgoai NVARCHAR(100) NOT NULL,
    CongThanhToan NVARCHAR(50) NOT NULL,
    SoTien DECIMAL(18, 2) NOT NULL,
    ThoiGianGiaoDich DATETIME DEFAULT GETDATE(),
    TrangThai NVARCHAR(100) NOT NULL,
    MaLoi NVARCHAR(50) NULL,
    MoTaLoi NVARCHAR(500) NULL,
    INDEX IX_GiaoDich_MaGiaoDichNgoai (MaGiaoDichNgoai)
);
GO

-- Bảng Lịch Sử Chat AI
CREATE TABLE ChatLichSu (
    idChat BIGINT PRIMARY KEY IDENTITY,
    idKhachHang INT NULL, /* FK Sẽ được thêm ở PHẦN 2 */
    idNhanVien INT NULL, /* FK Sẽ được thêm ở PHẦN 2 */
    NoiDungHoi NTEXT NOT NULL,
    NoiDungTraLoi NTEXT NOT NULL,
    ThoiGian DATETIME DEFAULT GETDATE(),
    LoaiChat NVARCHAR(50) NULL
);
GO

-- Bảng Gợi Ý Sản Phẩm
CREATE TABLE DeXuatSanPham (
    idSanPhamGoc INT NOT NULL, /* FK Sẽ được thêm ở PHẦN 2 */
    idSanPhamDeXuat INT NOT NULL, /* FK Sẽ được thêm ở PHẦN 2 */
    DoLienQuan FLOAT NOT NULL DEFAULT 0,
    LoaiDeXuat NVARCHAR(100) NOT NULL,
    PRIMARY KEY (idSanPhamGoc, idSanPhamDeXuat, LoaiDeXuat)
);
GO

-- Bảng Gợi Ý Sách
CREATE TABLE DeXuatSach (
    idSachGoc INT NOT NULL, /* FK Sẽ được thêm ở PHẦN 2 */
    idSachDeXuat INT NOT NULL, /* FK Sẽ được thêm ở PHẦN 2 */
    DoLienQuan FLOAT NOT NULL DEFAULT 0,
    LoaiDeXuat NVARCHAR(100) NOT NULL,
    PRIMARY KEY (idSachGoc, idSachDeXuat, LoaiDeXuat)
);
GO

PRINT N'PHẦN 1: TẠO TẤT CẢ CÁC BẢNG THÀNH CÔNG.';
GO

/*
================================================================================
 PHẦN 2: THÊM TẤT CẢ CÁC KHÓA NGOẠI (FOREIGN KEYS)
================================================================================
*/

ALTER TABLE [dbo].[Ban]  WITH CHECK ADD CONSTRAINT [FK_Ban_KhuVuc] FOREIGN KEY([idKhuVuc])
REFERENCES [dbo].[KhuVuc] ([idKhuVuc])
GO

ALTER TABLE [dbo].[HoaDon]  WITH CHECK ADD CONSTRAINT [FK_HoaDon_Ban] FOREIGN KEY([idBan])
REFERENCES [dbo].[Ban] ([idBan])
GO
ALTER TABLE [dbo].[HoaDon]  WITH CHECK ADD CONSTRAINT [FK_HoaDon_NhanVien] FOREIGN KEY([idNhanVien])
REFERENCES [dbo].[NhanVien] ([idNhanVien])
GO
ALTER TABLE [dbo].[HoaDon]  WITH CHECK ADD CONSTRAINT [FK_HoaDon_KhachHang] FOREIGN KEY([idKhachHang])
REFERENCES [dbo].[KhachHang] ([idKhachHang])
GO
ALTER TABLE [dbo].[HoaDon]  WITH CHECK ADD CONSTRAINT [FK_HoaDon_NguoiGiaoHang] FOREIGN KEY([idNguoiGiaoHang])
REFERENCES [dbo].[NguoiGiaoHang] ([idNguoiGiaoHang])
GO

ALTER TABLE [dbo].[DanhMuc]  WITH CHECK ADD CONSTRAINT [FK_DanhMuc_DanhMucCha] FOREIGN KEY([idDanhMucCha])
REFERENCES [dbo].[DanhMuc] ([idDanhMuc])
GO

ALTER TABLE [dbo].[SanPham]  WITH CHECK ADD CONSTRAINT [FK_SanPham_DanhMuc] FOREIGN KEY([idDanhMuc])
REFERENCES [dbo].[DanhMuc] ([idDanhMuc])
GO

ALTER TABLE [dbo].[ChiTietHoaDon]  WITH CHECK ADD CONSTRAINT [FK_ChiTietHoaDon_HoaDon] FOREIGN KEY([idHoaDon])
REFERENCES [dbo].[HoaDon] ([idHoaDon]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[ChiTietHoaDon]  WITH CHECK ADD CONSTRAINT [FK_ChiTietHoaDon_SanPham] FOREIGN KEY([idSanPham])
REFERENCES [dbo].[SanPham] ([idSanPham])
GO

ALTER TABLE [dbo].[ChiTietPhuThuHoaDon]  WITH CHECK ADD CONSTRAINT [FK_CTPT_HoaDon] FOREIGN KEY([idHoaDon])
REFERENCES [dbo].[HoaDon] ([idHoaDon]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[ChiTietPhuThuHoaDon]  WITH CHECK ADD CONSTRAINT [FK_CTPT_PhuThu] FOREIGN KEY([idPhuThu])
REFERENCES [dbo].[PhuThu] ([idPhuThu])
GO

ALTER TABLE [dbo].[NhatKyHuyMon]  WITH CHECK ADD CONSTRAINT [FK_NhatKyHuyMon_HoaDon] FOREIGN KEY([idHoaDon])
REFERENCES [dbo].[HoaDon] ([idHoaDon])
GO
ALTER TABLE [dbo].[NhatKyHuyMon]  WITH CHECK ADD CONSTRAINT [FK_NhatKyHuyMon_SanPham] FOREIGN KEY([idSanPham])
REFERENCES [dbo].[SanPham] ([idSanPham])
GO
ALTER TABLE [dbo].[NhatKyHuyMon]  WITH CHECK ADD CONSTRAINT [FK_NhatKyHuyMon_NhanVien] FOREIGN KEY([idNhanVienHuy])
REFERENCES [dbo].[NhanVien] ([idNhanVien])
GO

ALTER TABLE [dbo].[DinhLuong]  WITH CHECK ADD CONSTRAINT [FK_DinhLuong_SanPham] FOREIGN KEY([idSanPham])
REFERENCES [dbo].[SanPham] ([idSanPham]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[DinhLuong]  WITH CHECK ADD CONSTRAINT [FK_DinhLuong_NguyenLieu] FOREIGN KEY([idNguyenLieu])
REFERENCES [dbo].[NguyenLieu] ([idNguyenLieu])
GO

ALTER TABLE [dbo].[PhieuNhapKho]  WITH CHECK ADD CONSTRAINT [FK_PhieuNhapKho_NhaCungCap] FOREIGN KEY([idNhaCungCap])
REFERENCES [dbo].[NhaCungCap] ([idNhaCungCap])
GO
ALTER TABLE [dbo].[PhieuNhapKho]  WITH CHECK ADD CONSTRAINT [FK_PhieuNhapKho_NhanVien] FOREIGN KEY([idNhanVien])
REFERENCES [dbo].[NhanVien] ([idNhanVien])
GO

ALTER TABLE [dbo].[ChiTietNhapKho]  WITH CHECK ADD CONSTRAINT [FK_ChiTietNhapKho_PhieuNhapKho] FOREIGN KEY([idPhieuNhapKho])
REFERENCES [dbo].[PhieuNhapKho] ([idPhieuNhapKho]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[ChiTietNhapKho]  WITH CHECK ADD CONSTRAINT [FK_ChiTietNhapKho_NguyenLieu] FOREIGN KEY([idNguyenLieu])
REFERENCES [dbo].[NguyenLieu] ([idNguyenLieu])
GO

ALTER TABLE [dbo].[PhieuKiemKho]  WITH CHECK ADD CONSTRAINT [FK_PKK_NhanVien] FOREIGN KEY([idNhanVienKiem])
REFERENCES [dbo].[NhanVien] ([idNhanVien])
GO

ALTER TABLE [dbo].[ChiTietKiemKho]  WITH CHECK ADD CONSTRAINT [FK_CTKK_PhieuKiemKho] FOREIGN KEY([idPhieuKiemKho])
REFERENCES [dbo].[PhieuKiemKho] ([idPhieuKiemKho]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[ChiTietKiemKho]  WITH CHECK ADD CONSTRAINT [FK_CTKK_NguyenLieu] FOREIGN KEY([idNguyenLieu])
REFERENCES [dbo].[NguyenLieu] ([idNguyenLieu])
GO

ALTER TABLE [dbo].[PhieuXuatHuy]  WITH CHECK ADD CONSTRAINT [FK_PXH_NhanVien] FOREIGN KEY([idNhanVienXuat])
REFERENCES [dbo].[NhanVien] ([idNhanVien])
GO

ALTER TABLE [dbo].[ChiTietXuatHuy]  WITH CHECK ADD CONSTRAINT [FK_CTXH_PhieuXuatHuy] FOREIGN KEY([idPhieuXuatHuy])
REFERENCES [dbo].[PhieuXuatHuy] ([idPhieuXuatHuy]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[ChiTietXuatHuy]  WITH CHECK ADD CONSTRAINT [FK_CTXH_NguyenLieu] FOREIGN KEY([idNguyenLieu])
REFERENCES [dbo].[NguyenLieu] ([idNguyenLieu])
GO

ALTER TABLE [dbo].[PhieuDatBan]  WITH CHECK ADD CONSTRAINT [FK_PhieuDatBan_KhachHang] FOREIGN KEY([idKhachHang])
REFERENCES [dbo].[KhachHang] ([idKhachHang])
GO
ALTER TABLE [dbo].[PhieuDatBan]  WITH CHECK ADD CONSTRAINT [FK_PhieuDatBan_Ban] FOREIGN KEY([idBan])
REFERENCES [dbo].[Ban] ([idBan])
GO

ALTER TABLE [dbo].[HoaDon_KhuyenMai]  WITH CHECK ADD CONSTRAINT [FK_HDKM_HoaDon] FOREIGN KEY([idHoaDon])
REFERENCES [dbo].[HoaDon] ([idHoaDon])
GO
ALTER TABLE [dbo].[HoaDon_KhuyenMai]  WITH CHECK ADD CONSTRAINT [FK_HDKM_KhuyenMai] FOREIGN KEY([idKhuyenMai])
REFERENCES [dbo].[KhuyenMai] ([idKhuyenMai])
GO

ALTER TABLE [dbo].[Sach]  WITH CHECK ADD CONSTRAINT [FK_Sach_TheLoai] FOREIGN KEY([idTheLoai])
REFERENCES [dbo].[TheLoai] ([idTheLoai])
GO
ALTER TABLE [dbo].[Sach]  WITH CHECK ADD CONSTRAINT [FK_Sach_TacGia] FOREIGN KEY([idTacGia])
REFERENCES [dbo].[TacGia] ([idTacGia])
GO
ALTER TABLE [dbo].[Sach]  WITH CHECK ADD CONSTRAINT [FK_Sach_NhaXuatBan] FOREIGN KEY([idNhaXuatBan])
REFERENCES [dbo].[NhaXuatBan] ([idNhaXuatBan])
GO

ALTER TABLE [dbo].[PhieuThueSach]  WITH CHECK ADD CONSTRAINT [FK_PhieuThueSach_KhachHang] FOREIGN KEY([idKhachHang])
REFERENCES [dbo].[KhachHang] ([idKhachHang])
GO
ALTER TABLE [dbo].[PhieuThueSach]  WITH CHECK ADD CONSTRAINT [FK_PhieuThueSach_NhanVien] FOREIGN KEY([idNhanVien])
REFERENCES [dbo].[NhanVien] ([idNhanVien])
GO

ALTER TABLE [dbo].[ChiTietPhieuThue]  WITH CHECK ADD CONSTRAINT [FK_ChiTietThue_Phieu] FOREIGN KEY([idPhieuThueSach])
REFERENCES [dbo].[PhieuThueSach] ([idPhieuThueSach]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[ChiTietPhieuThue]  WITH CHECK ADD CONSTRAINT [FK_ChiTietThue_Sach] FOREIGN KEY([idSach])
REFERENCES [dbo].[Sach] ([idSach])
GO

ALTER TABLE [dbo].[NhanVien]  WITH CHECK ADD CONSTRAINT [FK_NhanVien_VaiTro] FOREIGN KEY([idVaiTro])
REFERENCES [dbo].[VaiTro] ([idVaiTro])
GO

ALTER TABLE [dbo].[LichLamViec]  WITH CHECK ADD CONSTRAINT [FK_LichLamViec_NhanVien] FOREIGN KEY([idNhanVien])
REFERENCES [dbo].[NhanVien] ([idNhanVien])
GO
ALTER TABLE [dbo].[LichLamViec]  WITH CHECK ADD CONSTRAINT [FK_LichLamViec_CaLamViec] FOREIGN KEY([idCa])
REFERENCES [dbo].[CaLamViec] ([idCa])
GO

ALTER TABLE [dbo].[BangChamCong]  WITH CHECK ADD CONSTRAINT [FK_ChamCong_LichLamViec] FOREIGN KEY([idLichLamViec])
REFERENCES [dbo].[LichLamViec] ([idLichLamViec]) ON DELETE CASCADE
GO

ALTER TABLE [dbo].[PhieuLuong]  WITH CHECK ADD CONSTRAINT [FK_PhieuLuong_NhanVien] FOREIGN KEY([idNhanVien])
REFERENCES [dbo].[NhanVien] ([idNhanVien])
GO

ALTER TABLE [dbo].[DonXinNghi]  WITH CHECK ADD CONSTRAINT [FK_DonXinNghi_NhanVien] FOREIGN KEY([idNhanVien])
REFERENCES [dbo].[NhanVien] ([idNhanVien])
GO
ALTER TABLE [dbo].[DonXinNghi]  WITH CHECK ADD CONSTRAINT [FK_DonXinNghi_NguoiDuyet] FOREIGN KEY([idNguoiDuyet])
REFERENCES [dbo].[NhanVien] ([idNhanVien])
GO

ALTER TABLE [dbo].[VaiTro_Quyen]  WITH CHECK ADD CONSTRAINT [FK_VTQ_VaiTro] FOREIGN KEY([idVaiTro])
REFERENCES [dbo].[VaiTro] ([idVaiTro]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[VaiTro_Quyen]  WITH CHECK ADD CONSTRAINT [FK_VTQ_Quyen] FOREIGN KEY([idQuyen])
REFERENCES [dbo].[Quyen] ([idQuyen]) ON DELETE CASCADE
GO

ALTER TABLE [dbo].[GiaoDichThanhToan]  WITH CHECK ADD CONSTRAINT [FK_GiaoDich_HoaDon] FOREIGN KEY([idHoaDon])
REFERENCES [dbo].[HoaDon] ([idHoaDon])
GO

ALTER TABLE [dbo].[ChatLichSu]  WITH CHECK ADD CONSTRAINT [FK_ChatLichSu_KhachHang] FOREIGN KEY([idKhachHang])
REFERENCES [dbo].[KhachHang] ([idKhachHang])
GO
ALTER TABLE [dbo].[ChatLichSu]  WITH CHECK ADD CONSTRAINT [FK_ChatLichSu_NhanVien] FOREIGN KEY([idNhanVien])
REFERENCES [dbo].[NhanVien] ([idNhanVien])
GO

ALTER TABLE [dbo].[DeXuatSanPham]  WITH CHECK ADD CONSTRAINT [FK_DeXuatSP_Goc] FOREIGN KEY([idSanPhamGoc])
REFERENCES [dbo].[SanPham] ([idSanPham])
GO
ALTER TABLE [dbo].[DeXuatSanPham]  WITH CHECK ADD CONSTRAINT [FK_DeXuatSP_DeXuat] FOREIGN KEY([idSanPhamDeXuat])
REFERENCES [dbo].[SanPham] ([idSanPham])
GO

ALTER TABLE [dbo].[DeXuatSach]  WITH CHECK ADD CONSTRAINT [FK_DeXuatSach_Goc] FOREIGN KEY([idSachGoc])
REFERENCES [dbo].[Sach] ([idSach])
GO
ALTER TABLE [dbo].[DeXuatSach]  WITH CHECK ADD CONSTRAINT [FK_DeXuatSach_DeXuat] FOREIGN KEY([idSachDeXuat])
REFERENCES [dbo].[Sach] ([idSach])
GO

PRINT N'PHẦN 2: THÊM TẤT CẢ KHÓA NGOẠI THÀNH CÔNG.';
GO

PRINT N'Tạo CẤU TRÚC CSDL CAFEBOOKDB_v2 (v2.2.1) thành công. Tất cả các bảng và khóa ngoại đã được thiết lập.';
GO