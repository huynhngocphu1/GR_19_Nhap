// Tập tin: CafebookApi/Data/CafebookDbContext.cs
using CafebookModel.Model.Entities;
using Microsoft.EntityFrameworkCore;

namespace CafebookApi.Data
{
    public class CafebookDbContext : DbContext
    {
        public CafebookDbContext(DbContextOptions<CafebookDbContext> options)
            : base(options)
        {
        }

        // --- Khai báo tất cả các Bảng (DbSet) ---
        public DbSet<KhuVuc> KhuVucs { get; set; }
        public DbSet<Ban> Bans { get; set; }
        public DbSet<NguoiGiaoHang> NguoiGiaoHangs { get; set; }
        public DbSet<HoaDon> HoaDons { get; set; }
        public DbSet<DanhMuc> DanhMucs { get; set; }
        public DbSet<SanPham> SanPhams { get; set; }
        public DbSet<ChiTietHoaDon> ChiTietHoaDons { get; set; }
        public DbSet<PhuThu> PhuThus { get; set; }
        public DbSet<ChiTietPhuThuHoaDon> ChiTietPhuThuHoaDons { get; set; }
        public DbSet<NhatKyHuyMon> NhatKyHuyMons { get; set; }
        public DbSet<NguyenLieu> NguyenLieus { get; set; }
        public DbSet<DinhLuong> DinhLuongs { get; set; }
        public DbSet<NhaCungCap> NhaCungCaps { get; set; }
        public DbSet<PhieuNhapKho> PhieuNhapKhos { get; set; }
        public DbSet<ChiTietNhapKho> ChiTietNhapKhos { get; set; }
        public DbSet<PhieuKiemKho> PhieuKiemKhos { get; set; }
        public DbSet<ChiTietKiemKho> ChiTietKiemKhos { get; set; }
        public DbSet<PhieuXuatHuy> PhieuXuatHuys { get; set; }
        public DbSet<ChiTietXuatHuy> ChiTietXuatHuys { get; set; }
        public DbSet<KhachHang> KhachHangs { get; set; }
        public DbSet<PhieuDatBan> PhieuDatBans { get; set; }
        public DbSet<KhuyenMai> KhuyenMais { get; set; }
        public DbSet<HoaDon_KhuyenMai> HoaDonKhuyenMais { get; set; }
        public DbSet<TheLoai> TheLoais { get; set; }
        public DbSet<TacGia> TacGias { get; set; }
        public DbSet<NhaXuatBan> NhaXuatBans { get; set; }
        public DbSet<SachTacGia> SachTacGias { get; set; }
        public DbSet<SachTheLoai> SachTheLoais { get; set; }
        public DbSet<SachNhaXuatBan> SachNhaXuatBans { get; set; }
        public DbSet<Sach> Sachs { get; set; }
        public DbSet<PhieuThueSach> PhieuThueSachs { get; set; }
        public DbSet<ChiTietPhieuThue> ChiTietPhieuThues { get; set; }
        public DbSet<VaiTro> VaiTros { get; set; }
        public DbSet<NhanVien> NhanViens { get; set; }
        public DbSet<CaLamViec> CaLamViecs { get; set; }
        public DbSet<LichLamViec> LichLamViecs { get; set; }
        public DbSet<BangChamCong> BangChamCongs { get; set; }
        public DbSet<PhieuLuong> PhieuLuongs { get; set; }
        public DbSet<DonXinNghi> DonXinNghis { get; set; }
        public DbSet<CaiDat> CaiDats { get; set; }
        public DbSet<Quyen> Quyens { get; set; }
        public DbSet<VaiTro_Quyen> VaiTroQuyens { get; set; }
        public DbSet<GiaoDichThanhToan> GiaoDichThanhToans { get; set; }
        public DbSet<ChatLichSu> ChatLichSus { get; set; }
        public DbSet<DeXuatSanPham> DeXuatSanPhams { get; set; }
        public DbSet<DeXuatSach> DeXuatSachs { get; set; }
        public DbSet<ThongBao> ThongBaos { get; set; }
        public DbSet<DonViChuyenDoi> DonViChuyenDois { get; set; }
        public DbSet<PhieuThuongPhat> PhieuThuongPhats { get; set; }
        public DbSet<PhieuTraSach> PhieuTraSachs { get; set; }
        public DbSet<ChiTietPhieuTra> ChiTietPhieuTras { get; set; }
        public DbSet<TrangThaiCheBien> TrangThaiCheBiens { get; set; }
        public virtual DbSet<DanhGia> DanhGias { get; set; }
        public virtual DbSet<PhanHoiDanhGia> PhanHoiDanhGias { get; set; }
        public virtual DbSet<ThongBaoHoTro> ThongBaoHoTros { get; set; }

        // --- Cấu hình Khóa (Fluent API) ---
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.UseCollation("Vietnamese_CI_AS");

            // Cấu hình Khóa chính tổng hợp (Composite Primary Keys)
            modelBuilder.Entity<ChiTietPhuThuHoaDon>().HasKey(c => new { c.IdHoaDon, c.IdPhuThu });
            modelBuilder.Entity<DinhLuong>().HasKey(c => new { c.IdSanPham, c.IdNguyenLieu });
            modelBuilder.Entity<ChiTietNhapKho>().HasKey(c => new { c.IdPhieuNhapKho, c.IdNguyenLieu });
            modelBuilder.Entity<ChiTietKiemKho>().HasKey(c => new { c.IdPhieuKiemKho, c.IdNguyenLieu });
            modelBuilder.Entity<ChiTietXuatHuy>().HasKey(c => new { c.IdPhieuXuatHuy, c.IdNguyenLieu });
            modelBuilder.Entity<HoaDon_KhuyenMai>().HasKey(c => new { c.IdHoaDon, c.IdKhuyenMai });
            modelBuilder.Entity<ChiTietPhieuThue>().HasKey(c => new { c.IdPhieuThueSach, c.IdSach });
            modelBuilder.Entity<VaiTro_Quyen>().HasKey(c => new { c.IdVaiTro, c.IdQuyen });
            modelBuilder.Entity<DeXuatSanPham>().HasKey(c => new { c.IdSanPhamGoc, c.IdSanPhamDeXuat, c.LoaiDeXuat });

            // SỬA LỖI & TỐI ƯU: Bỏ comment và thêm HasKey cho DeXuatSach
            modelBuilder.Entity<DeXuatSach>().HasKey(c => new { c.IdSachGoc, c.IdSachDeXuat, c.LoaiDeXuat });
            modelBuilder.Entity<ChiTietPhieuTra>().HasKey(c => new { c.IdPhieuTra, c.IdSach });


            // THÊM MỚI CẤU HÌNH CHO TrangThaiCheBien
            modelBuilder.Entity<TrangThaiCheBien>(e =>
            {
                e.HasOne(cb => cb.ChiTietHoaDon)
                 .WithMany() // Không cần collection ngược
                 .HasForeignKey(cb => cb.IdChiTietHoaDon)
                 .OnDelete(DeleteBehavior.Cascade); // Xóa CTHD thì xóa luôn phiếu bếp

                e.HasOne(cb => cb.HoaDon)
                 .WithMany() // Không cần collection ngược
                 .HasForeignKey(cb => cb.IdHoaDon)
                 .OnDelete(DeleteBehavior.NoAction); // Không cascade

                e.HasOne(cb => cb.SanPham)
                 .WithMany() // Không cần collection ngược
                 .HasForeignKey(cb => cb.IdSanPham)
                 .OnDelete(DeleteBehavior.NoAction); // Không cascade

                // Thêm Index
                e.HasIndex(cb => new { cb.TrangThai, cb.NhomIn })
                 .IncludeProperties(cb => cb.ThoiGianGoi);
            });
            // --- SỬA LỖI: THÊM CẤU HÌNH KHÓA NGOẠI CHO PHIẾU THUÊ ---
            modelBuilder.Entity<PhieuThueSach>(e =>
            {
                e.HasOne(p => p.KhachHang)
                 .WithMany(k => k.PhieuThueSachs)
                 .HasForeignKey(p => p.IdKhachHang) // Chỉ rõ FK là IdKhachHang
                 .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(p => p.NhanVien)
                 .WithMany(n => n.PhieuThueSachs)
                 .HasForeignKey(p => p.IdNhanVien) // Chỉ rõ FK là IdNhanVien
                 .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<ChiTietPhieuThue>(e =>
            {
                // (Khóa chính đã được định nghĩa ở trên)
                // e.HasKey(c => new { c.IdPhieuThueSach, c.IdSach });

                e.HasOne(ct => ct.PhieuThueSach)
                 .WithMany(p => p.ChiTietPhieuThues)
                 .HasForeignKey(ct => ct.IdPhieuThueSach) // Chỉ rõ FK
                 .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(ct => ct.Sach)
                 .WithMany(s => s.ChiTietPhieuThues)
                 .HasForeignKey(ct => ct.IdSach) // Chỉ rõ FK
                 .OnDelete(DeleteBehavior.NoAction);
            });

            // --- THÊM CẤU HÌNH MỚI CHO PHIẾU TRẢ SÁCH ---
            modelBuilder.Entity<PhieuTraSach>()
               .HasOne(pt => pt.PhieuThueSach)
               .WithMany(p => p.PhieuTraSachs)
               .HasForeignKey(pt => pt.IdPhieuThueSach)
               .OnDelete(DeleteBehavior.NoAction); // Tránh cascade

            modelBuilder.Entity<PhieuTraSach>()
               .HasOne(pt => pt.NhanVien)
               .WithMany(nv => nv.PhieuTraSachs)
               .HasForeignKey(pt => pt.IdNhanVien)
               .OnDelete(DeleteBehavior.NoAction); // Tránh cascade
            // --- KẾT THÚC THÊM MỚI ---

            // Cấu hình N-N cho Sách (Đã đúng)
            modelBuilder.Entity<SachTacGia>(e =>
            {
                e.HasKey(st => new { st.IdSach, st.IdTacGia });
                e.HasOne(st => st.Sach)
                    .WithMany(s => s.SachTacGias)
                    .HasForeignKey(st => st.IdSach);
                e.HasOne(st => st.TacGia)
                    .WithMany(t => t.SachTacGias)
                    .HasForeignKey(st => st.IdTacGia);
            });

            modelBuilder.Entity<SachTheLoai>(e =>
            {
                e.HasKey(st => new { st.IdSach, st.IdTheLoai });
                e.HasOne(st => st.Sach)
                    .WithMany(s => s.SachTheLoais)
                    .HasForeignKey(st => st.IdSach);
                e.HasOne(st => st.TheLoai)
                    .WithMany(t => t.SachTheLoais)
                    .HasForeignKey(st => st.IdTheLoai);
            });

            modelBuilder.Entity<SachNhaXuatBan>(e =>
            {
                e.HasKey(sn => new { sn.IdSach, sn.IdNhaXuatBan });
                e.HasOne(sn => sn.Sach)
                    .WithMany(s => s.SachNhaXuatBans)
                    .HasForeignKey(sn => sn.IdSach);
                e.HasOne(sn => sn.NhaXuatBan)
                    .WithMany(n => n.SachNhaXuatBans)
                    .HasForeignKey(sn => sn.IdNhaXuatBan);
            });

            // Cấu hình quan hệ N-1 phức tạp (Đã đúng)
            modelBuilder.Entity<PhieuThuongPhat>()
                .HasOne(p => p.NhanVien)
                .WithMany() // Không tạo collection
                .HasForeignKey(p => p.IdNhanVien)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<PhieuThuongPhat>()
                .HasOne(p => p.NguoiTao)
                .WithMany() // Không tạo collection
                .HasForeignKey(p => p.IdNguoiTao)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<PhieuThuongPhat>()
                .HasOne(p => p.PhieuLuong)
                .WithMany() // Không tạo collection
                .HasForeignKey(p => p.IdPhieuLuong)
                .OnDelete(DeleteBehavior.SetNull);

            // Cấu hình DinhLuong (Đã đúng)
            modelBuilder.Entity<DinhLuong>()
                .HasOne(d => d.DonViSuDung)
                .WithMany(dv => dv.DinhLuongs)
                .HasForeignKey(d => d.IdDonViSuDung)
                .OnDelete(DeleteBehavior.NoAction);

            // Cấu hình Tự tham chiếu (Đã đúng)
            modelBuilder.Entity<DanhMuc>()
                .HasOne(d => d.DanhMucCha)
                .WithMany(d => d.DanhMucCons)
                .HasForeignKey(d => d.IdDanhMucCha)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<DonXinNghi>()
                .HasOne(d => d.NguoiDuyet)
                .WithMany(nv => nv.DonXinNghiNguoiDuyets)
                .HasForeignKey(d => d.IdNguoiDuyet)
                .OnDelete(DeleteBehavior.NoAction);

            // Cấu hình DeXuat (Tránh lỗi cascade)
            modelBuilder.Entity<DeXuatSanPham>()
                .HasOne(d => d.SanPhamGoc)
                .WithMany(p => p.DeXuatSanPhamGocs)
                .HasForeignKey(d => d.IdSanPhamGoc)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<DeXuatSanPham>()
                .HasOne(d => d.SanPhamDeXuat)
                .WithMany(p => p.DeXuatSanPhamDeXuats)
                .HasForeignKey(d => d.IdSanPhamDeXuat)
                .OnDelete(DeleteBehavior.NoAction);

            // SỬA LỖI & TỐI ƯU: Bỏ comment cấu hình DeXuatSach
            modelBuilder.Entity<DeXuatSach>()
               .HasOne(d => d.SachGoc)
               .WithMany(p => p.DeXuatSachGocs)
               .HasForeignKey(d => d.IdSachGoc)
               .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<DeXuatSach>()
                .HasOne(d => d.SachDeXuat)
                .WithMany(p => p.DeXuatSachDeXuats)
                .HasForeignKey(d => d.IdSachDeXuat)
                .OnDelete(DeleteBehavior.NoAction);

            // Cấu hình Tắt Cascade khác (Đã đúng)
            modelBuilder.Entity<NhatKyHuyMon>()
                .HasOne(n => n.HoaDon)
                .WithMany(h => h.NhatKyHuyMons)
                .HasForeignKey(n => n.IdHoaDon)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<NhatKyHuyMon>()
                .HasOne(n => n.NhanVienHuy)
                .WithMany(nv => nv.NhatKyHuyMons)
                .HasForeignKey(n => n.IdNhanVienHuy)
                .OnDelete(DeleteBehavior.NoAction);

            // Cấu hình cho ChatLichSu (liên kết với ThongBaoHoTro)
            modelBuilder.Entity<ChatLichSu>()
                .HasOne(c => c.ThongBaoHoTro)
                .WithMany(t => t.ChatLichSus)
                .HasForeignKey(c => c.IdThongBaoHoTro)
                .OnDelete(DeleteBehavior.SetNull); // Nếu xóa phiếu hỗ trợ, vẫn giữ lịch sử chat

            // Cấu hình cho ThongBaoHoTro
            modelBuilder.Entity<ThongBaoHoTro>(e =>
            {
                e.HasOne(t => t.KhachHang)
                 .WithMany() // Không cần navigation ngược từ KhachHang
                 .HasForeignKey(t => t.IdKhachHang)
                 .OnDelete(DeleteBehavior.NoAction); // Không cho xóa KH nếu còn phiếu HT

                e.HasOne(t => t.NhanVien)
                 .WithMany() // Không cần navigation ngược từ NhanVien
                 .HasForeignKey(t => t.IdNhanVien)
                 .OnDelete(DeleteBehavior.NoAction); // Không cho xóa NV nếu còn phiếu HT
            });

            // Cấu hình bổ sung (Đã đúng)
            modelBuilder.Entity<Sach>()
                .Property(s => s.GiaBia)
                .HasColumnType("decimal(18, 2)");
        }
    }
}