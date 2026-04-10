USE [CAFEBOOKDB_v2]
GO

-- 1. Hạt Cà phê (ID 1)
INSERT INTO DonViChuyenDoi (idNguyenLieu, TenDonVi, GiaTriQuyDoi, LaDonViCoBan) VALUES
(1, N'kg', 1.0, 1),
(1, N'gram', 0.001, 0);

-- 2. Sữa tươi (ID 2)
INSERT INTO DonViChuyenDoi (idNguyenLieu, TenDonVi, GiaTriQuyDoi, LaDonViCoBan) VALUES
(2, N'lít', 1.0, 1),
(2, N'ml', 0.001, 0);

-- 3. Đường cát (ID 3)
INSERT INTO DonViChuyenDoi (idNguyenLieu, TenDonVi, GiaTriQuyDoi, LaDonViCoBan) VALUES
(3, N'kg', 1.0, 1),
(3, N'gram', 0.001, 0);

-- 4. Trà Oolong (ID 4)
INSERT INTO DonViChuyenDoi (idNguyenLieu, TenDonVi, GiaTriQuyDoi, LaDonViCoBan) VALUES
(4, N'kg', 1.0, 1),
(4, N'gram', 0.001, 0);

-- 5. Bột Cacao (ID 5)
INSERT INTO DonViChuyenDoi (idNguyenLieu, TenDonVi, GiaTriQuyDoi, LaDonViCoBan) VALUES
(5, N'kg', 1.0, 1),
(5, N'gram', 0.001, 0);

PRINT N'Chèn dữ liệu ĐVT thành công.';