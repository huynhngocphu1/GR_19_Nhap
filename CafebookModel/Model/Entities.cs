using System; // <-- Thêm
using System.Collections.Generic; // <-- Thêm
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CafebookModel.Model.Entities
{
    [Table("KhuVuc")]
    public class KhuVuc
    {
        [Key]
        public int IdKhuVuc { get; set; }
        [Required]
        [StringLength(100)]
        public string TenKhuVuc { get; set; } = string.Empty;
        [StringLength(500)]
        public string? MoTa { get; set; }

        public virtual ICollection<Ban> Bans { get; set; } = new List<Ban>();
    }

    [Table("Ban")]
    public class Ban
    {
        [Key]
        public int IdBan { get; set; }
        [Required]
        [StringLength(50)]
        public string SoBan { get; set; } = string.Empty;
        public int SoGhe { get; set; }
        [Required]
        [StringLength(50)]
        public string TrangThai { get; set; } = string.Empty;
        [StringLength(500)]
        public string? GhiChu { get; set; }

        public int? IdKhuVuc { get; set; }
        [ForeignKey("IdKhuVuc")]
        public virtual KhuVuc? KhuVuc { get; set; }

        public virtual ICollection<HoaDon> HoaDons { get; set; } = new List<HoaDon>();
        public virtual ICollection<PhieuDatBan> PhieuDatBans { get; set; } = new List<PhieuDatBan>();
    }

    [Table("NguoiGiaoHang")]
    public class NguoiGiaoHang
    {
        [Key]
        public int IdNguoiGiaoHang { get; set; }
        [Required]
        [StringLength(100)]
        public string TenNguoiGiaoHang { get; set; } = string.Empty;
        [Required]
        [StringLength(20)]
        public string SoDienThoai { get; set; } = string.Empty;
        [StringLength(50)]
        public string? TrangThai { get; set; }

        public virtual ICollection<HoaDon> HoaDons { get; set; } = new List<HoaDon>();
    }

    [Table("HoaDon")]
    public class HoaDon
    {
        [Key]
        public int IdHoaDon { get; set; }
        public int? IdBan { get; set; }
        public int? IdNhanVien { get; set; }
        public int? IdKhachHang { get; set; }
        public DateTime ThoiGianTao { get; set; }
        public DateTime? ThoiGianThanhToan { get; set; }
        [Required]
        [StringLength(50)]
        public string TrangThai { get; set; } = string.Empty;
        [Column(TypeName = "decimal(18, 2)")]
        public decimal TongTienGoc { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal GiamGia { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal TongPhuThu { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public decimal ThanhTien { get; set; }
        [StringLength(50)]
        public string? PhuongThucThanhToan { get; set; }
        public string? GhiChu { get; set; }
        [Required]
        [StringLength(50)]
        public string LoaiHoaDon { get; set; } = string.Empty;
        [StringLength(100)]
        public string? TrangThaiGiaoHang { get; set; }
        [StringLength(500)]
        public string? DiaChiGiaoHang { get; set; }
        [StringLength(20)]
        public string? SoDienThoaiGiaoHang { get; set; }
        public int? IdNguoiGiaoHang { get; set; }

        [ForeignKey("IdBan")]
        public virtual Ban? Ban { get; set; }
        [ForeignKey("IdNhanVien")]
        public virtual NhanVien? NhanVien { get; set; }
        [ForeignKey("IdKhachHang")]
        public virtual KhachHang? KhachHang { get; set; }
        [ForeignKey("IdNguoiGiaoHang")]
        public virtual NguoiGiaoHang? NguoiGiaoHang { get; set; }

        public virtual ICollection<ChiTietHoaDon> ChiTietHoaDons { get; set; } = new List<ChiTietHoaDon>();
        public virtual ICollection<NhatKyHuyMon> NhatKyHuyMons { get; set; } = new List<NhatKyHuyMon>();
        public virtual ICollection<GiaoDichThanhToan> GiaoDichThanhToans { get; set; } = new List<GiaoDichThanhToan>();
        public virtual ICollection<ChiTietPhuThuHoaDon> ChiTietPhuThuHoaDons { get; set; } = new List<ChiTietPhuThuHoaDon>();
        public virtual ICollection<HoaDon_KhuyenMai> HoaDonKhuyenMais { get; set; } = new List<HoaDon_KhuyenMai>();
    }

    [Table("DanhMuc")]
    public class DanhMuc
    {
        [Key]
        public int IdDanhMuc { get; set; }
        [Required]
        [StringLength(255)]
        public string TenDanhMuc { get; set; } = string.Empty;
        public int? IdDanhMucCha { get; set; }

        [ForeignKey("IdDanhMucCha")]
        public virtual DanhMuc? DanhMucCha { get; set; }
        public virtual ICollection<DanhMuc> DanhMucCons { get; set; } = new List<DanhMuc>();
        public virtual ICollection<SanPham> SanPhams { get; set; } = new List<SanPham>();
    }

    [Table("SanPham")]
    public class SanPham
    {
        [Key]
        public int IdSanPham { get; set; }
        [Required]
        [StringLength(255)]
        public string TenSanPham { get; set; } = string.Empty;
        public int IdDanhMuc { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal GiaBan { get; set; }
        public string? MoTa { get; set; }
        public bool TrangThaiKinhDoanh { get; set; }
        public string? HinhAnh { get; set; } // Base64
        [StringLength(50)]
        public string? NhomIn { get; set; }

        [ForeignKey("IdDanhMuc")]
        public virtual DanhMuc DanhMuc { get; set; } = null!;

        public virtual ICollection<ChiTietHoaDon> ChiTietHoaDons { get; set; } = new List<ChiTietHoaDon>();
        public virtual ICollection<NhatKyHuyMon> NhatKyHuyMons { get; set; } = new List<NhatKyHuyMon>();
        public virtual ICollection<DinhLuong> DinhLuongs { get; set; } = new List<DinhLuong>();
        public virtual ICollection<DeXuatSanPham> DeXuatSanPhamGocs { get; set; } = new List<DeXuatSanPham>();
        public virtual ICollection<DeXuatSanPham> DeXuatSanPhamDeXuats { get; set; } = new List<DeXuatSanPham>();
        public virtual ICollection<DanhGia> DanhGias { get; set; } = new List<DanhGia>();
    }

    [Table("ChiTietHoaDon")]
    public class ChiTietHoaDon
    {
        [Key]
        public int IdChiTietHoaDon { get; set; }
        public int IdHoaDon { get; set; }
        public int IdSanPham { get; set; }
        public int SoLuong { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal DonGia { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public decimal ThanhTien { get; set; }
        [StringLength(500)]
        public string? GhiChu { get; set; }

        [ForeignKey("IdHoaDon")]
        public virtual HoaDon HoaDon { get; set; } = null!;
        [ForeignKey("IdSanPham")]
        public virtual SanPham SanPham { get; set; } = null!;
    }

    [Table("PhuThu")]
    public class PhuThu
    {
        [Key]
        public int IdPhuThu { get; set; }
        [Required]
        [StringLength(100)]
        public string TenPhuThu { get; set; } = string.Empty;
        [Column(TypeName = "decimal(18, 2)")]
        public decimal GiaTri { get; set; }
        [Required]
        [StringLength(20)]
        public string LoaiGiaTri { get; set; } = string.Empty;

        public virtual ICollection<ChiTietPhuThuHoaDon> ChiTietPhuThuHoaDons { get; set; } = new List<ChiTietPhuThuHoaDon>();
    }

    [Table("ChiTietPhuThuHoaDon")]
    public class ChiTietPhuThuHoaDon
    {
        public int IdHoaDon { get; set; }
        public int IdPhuThu { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal SoTien { get; set; }

        [ForeignKey("IdHoaDon")]
        public virtual HoaDon HoaDon { get; set; } = null!;
        [ForeignKey("IdPhuThu")]
        public virtual PhuThu PhuThu { get; set; } = null!;
    }

    [Table("NhatKyHuyMon")]
    public class NhatKyHuyMon
    {
        [Key]
        public int IdNhatKy { get; set; }
        public int IdHoaDon { get; set; }
        public int IdSanPham { get; set; }
        public int SoLuongHuy { get; set; }
        [Required]
        [StringLength(255)]
        public string LyDo { get; set; } = string.Empty;
        public int IdNhanVienHuy { get; set; }
        public DateTime ThoiGianHuy { get; set; }

        [ForeignKey("IdHoaDon")]
        public virtual HoaDon HoaDon { get; set; } = null!;
        [ForeignKey("IdSanPham")]
        public virtual SanPham SanPham { get; set; } = null!;
        [ForeignKey("IdNhanVienHuy")]
        public virtual NhanVien NhanVienHuy { get; set; } = null!;
    }

    [Table("NguyenLieu")]
    public class NguyenLieu
    {
        [Key]
        public int IdNguyenLieu { get; set; }
        [Required]
        [StringLength(255)]
        public string TenNguyenLieu { get; set; } = string.Empty;
        [Required]
        [StringLength(50)]
        public string DonViTinh { get; set; } = string.Empty;
        [Column(TypeName = "decimal(18, 2)")]
        public decimal TonKho { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal TonKhoToiThieu { get; set; }

        public virtual ICollection<DinhLuong> DinhLuongs { get; set; } = new List<DinhLuong>();
        public virtual ICollection<ChiTietNhapKho> ChiTietNhapKhos { get; set; } = new List<ChiTietNhapKho>();
        public virtual ICollection<ChiTietKiemKho> ChiTietKiemKhos { get; set; } = new List<ChiTietKiemKho>();
        public virtual ICollection<ChiTietXuatHuy> ChiTietXuatHuys { get; set; } = new List<ChiTietXuatHuy>();
    }

    [Table("DinhLuong")]
    public class DinhLuong
    {
        public int IdSanPham { get; set; }
        public int IdNguyenLieu { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal SoLuongSuDung { get; set; } // <-- ĐÃ ĐỔI TÊN

        public int IdDonViSuDung { get; set; } // <-- THÊM MỚI

        [ForeignKey("IdSanPham")]
        public virtual SanPham SanPham { get; set; } = null!;
        [ForeignKey("IdNguyenLieu")]
        public virtual NguyenLieu NguyenLieu { get; set; } = null!;

        [ForeignKey("IdDonViSuDung")] // <-- THÊM MỚI
        public virtual DonViChuyenDoi DonViSuDung { get; set; } = null!;
    }

    [Table("DonViChuyenDoi")]
    public class DonViChuyenDoi
    {
        [Key]
        public int IdChuyenDoi { get; set; }

        public int IdNguyenLieu { get; set; }

        [Required]
        [StringLength(50)]
        public string TenDonVi { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18, 6)")]
        public decimal GiaTriQuyDoi { get; set; }

        public bool LaDonViCoBan { get; set; }

        [ForeignKey("IdNguyenLieu")]
        public virtual NguyenLieu NguyenLieu { get; set; } = null!;
        public virtual ICollection<DinhLuong> DinhLuongs { get; set; } = new List<DinhLuong>();
    }

    [Table("NhaCungCap")]
    public class NhaCungCap
    {
        [Key]
        public int IdNhaCungCap { get; set; }
        [Required]
        [StringLength(255)]
        public string TenNhaCungCap { get; set; } = string.Empty;
        [StringLength(20)]
        public string? SoDienThoai { get; set; }
        [StringLength(500)]
        public string? DiaChi { get; set; }
        [StringLength(100)]
        public string? Email { get; set; }

        public virtual ICollection<PhieuNhapKho> PhieuNhapKhos { get; set; } = new List<PhieuNhapKho>();
    }

    [Table("PhieuNhapKho")]
    public class PhieuNhapKho
    {
        [Key]
        public int IdPhieuNhapKho { get; set; }
        public int? IdNhaCungCap { get; set; }
        public int IdNhanVien { get; set; }
        public DateTime NgayNhap { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal TongTien { get; set; }
        [StringLength(500)]
        public string? GhiChu { get; set; }
        [Required]
        [StringLength(50)]
        public string TrangThai { get; set; } = string.Empty;

        [ForeignKey("IdNhaCungCap")]
        public virtual NhaCungCap? NhaCungCap { get; set; }
        [ForeignKey("IdNhanVien")]
        public virtual NhanVien NhanVien { get; set; } = null!;

        public virtual ICollection<ChiTietNhapKho> ChiTietNhapKhos { get; set; } = new List<ChiTietNhapKho>();
    }

    [Table("ChiTietNhapKho")]
    public class ChiTietNhapKho
    {
        public int IdPhieuNhapKho { get; set; }
        public int IdNguyenLieu { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal SoLuongNhap { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal DonGiaNhap { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public decimal ThanhTien { get; set; }

        [ForeignKey("IdPhieuNhapKho")]
        public virtual PhieuNhapKho PhieuNhapKho { get; set; } = null!;
        [ForeignKey("IdNguyenLieu")]
        public virtual NguyenLieu NguyenLieu { get; set; } = null!;
    }

    [Table("PhieuKiemKho")]
    public class PhieuKiemKho
    {
        [Key]
        public int IdPhieuKiemKho { get; set; }
        public int IdNhanVienKiem { get; set; }
        public DateTime NgayKiem { get; set; }
        [StringLength(500)]
        public string? GhiChu { get; set; }
        [Required]
        [StringLength(50)]
        public string TrangThai { get; set; } = string.Empty;

        [ForeignKey("IdNhanVienKiem")]
        public virtual NhanVien NhanVienKiem { get; set; } = null!;

        public virtual ICollection<ChiTietKiemKho> ChiTietKiemKhos { get; set; } = new List<ChiTietKiemKho>();
    }

    [Table("ChiTietKiemKho")]
    public class ChiTietKiemKho
    {
        public int IdPhieuKiemKho { get; set; }
        public int IdNguyenLieu { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal TonKhoHeThong { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal TonKhoThucTe { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public decimal ChenhLech { get; set; }
        [StringLength(255)]
        public string? LyDoChenhLech { get; set; }

        [ForeignKey("IdPhieuKiemKho")]
        public virtual PhieuKiemKho PhieuKiemKho { get; set; } = null!;
        [ForeignKey("IdNguyenLieu")]
        public virtual NguyenLieu NguyenLieu { get; set; } = null!;
    }

    [Table("PhieuXuatHuy")]
    public class PhieuXuatHuy
    {
        [Key]
        public int IdPhieuXuatHuy { get; set; }
        public int IdNhanVienXuat { get; set; }
        public DateTime NgayXuatHuy { get; set; }
        [Required]
        [StringLength(500)]
        public string LyDoXuatHuy { get; set; } = string.Empty;
        [Column(TypeName = "decimal(18, 2)")]
        public decimal TongGiaTriHuy { get; set; }

        [ForeignKey("IdNhanVienXuat")]
        public virtual NhanVien NhanVienXuat { get; set; } = null!;

        public virtual ICollection<ChiTietXuatHuy> ChiTietXuatHuys { get; set; } = new List<ChiTietXuatHuy>();
    }

    [Table("ChiTietXuatHuy")]
    public class ChiTietXuatHuy
    {
        public int IdPhieuXuatHuy { get; set; }
        public int IdNguyenLieu { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal SoLuong { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal DonGiaVon { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public decimal ThanhTien { get; set; }

        [ForeignKey("IdPhieuXuatHuy")]
        public virtual PhieuXuatHuy PhieuXuatHuy { get; set; } = null!;
        [ForeignKey("IdNguyenLieu")]
        public virtual NguyenLieu NguyenLieu { get; set; } = null!;
    }

    [Table("KhachHang")]
    public class KhachHang
    {
        [Key]
        public int IdKhachHang { get; set; }
        [Required]
        [StringLength(255)]
        public string HoTen { get; set; } = string.Empty;
        [StringLength(20)]
        public string? SoDienThoai { get; set; }
        [StringLength(100)]
        public string? Email { get; set; }
        [StringLength(500)]
        public string? DiaChi { get; set; }
        public int DiemTichLuy { get; set; }
        [StringLength(100)]
        public string? TenDangNhap { get; set; }
        [StringLength(255)]
        public string? MatKhau { get; set; }
        public DateTime NgayTao { get; set; }
        public bool BiKhoa { get; set; }
        public string? AnhDaiDien { get; set; } 
        public bool TaiKhoanTam { get; set; }
        public virtual ICollection<HoaDon> HoaDons { get; set; } = new List<HoaDon>();
        public virtual ICollection<PhieuDatBan> PhieuDatBans { get; set; } = new List<PhieuDatBan>();
        public virtual ICollection<PhieuThueSach> PhieuThueSachs { get; set; } = new List<PhieuThueSach>();
        public virtual ICollection<ChatLichSu> ChatLichSus { get; set; } = new List<ChatLichSu>();
    }

    [Table("PhieuDatBan")]
    public class PhieuDatBan
    {
        [Key]
        public int IdPhieuDatBan { get; set; }
        public int? IdKhachHang { get; set; }
        public int IdBan { get; set; }
        [StringLength(100)]
        public string? HoTenKhach { get; set; }
        [StringLength(20)]
        public string? SdtKhach { get; set; }
        public DateTime ThoiGianDat { get; set; }
        public int SoLuongKhach { get; set; }
        [Required]
        [StringLength(50)]
        public string TrangThai { get; set; } = string.Empty;
        [StringLength(500)]
        public string? GhiChu { get; set; }

        [ForeignKey("IdKhachHang")]
        public virtual KhachHang? KhachHang { get; set; }
        [ForeignKey("IdBan")]
        public virtual Ban Ban { get; set; } = null!;
    }

    [Table("KhuyenMai")]
    public class KhuyenMai
    {
        [Key]
        public int IdKhuyenMai { get; set; }
        [Required]
        [StringLength(50)]
        public string MaKhuyenMai { get; set; } = string.Empty;
        [Required]
        [StringLength(255)]
        public string TenChuongTrinh { get; set; } = string.Empty;
        public string? MoTa { get; set; }
        [Required]
        [StringLength(20)]
        public string LoaiGiamGia { get; set; } = string.Empty;
        [Column(TypeName = "decimal(18, 2)")]
        public decimal GiaTriGiam { get; set; }
        public DateTime NgayBatDau { get; set; }
        public DateTime NgayKetThuc { get; set; }
        [StringLength(500)]
        public string? DieuKienApDung { get; set; } // Mô tả cho DataGrid
        public int? SoLuongConLai { get; set; }

        // --- CÁC CỘT MỚI ---
        [Required]
        [StringLength(50)]
        public string TrangThai { get; set; } = string.Empty; // Hoạt động, Tạm dừng, Hết hạn
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? GiamToiDa { get; set; }
        public int? IdSanPhamApDung { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? HoaDonToiThieu { get; set; }
        public TimeSpan? GioBatDau { get; set; }
        public TimeSpan? GioKetThuc { get; set; }
        [StringLength(50)]
        public string? NgayTrongTuan { get; set; }

        [ForeignKey("IdSanPhamApDung")]
        public virtual SanPham? SanPhamApDung { get; set; }

        public virtual ICollection<HoaDon_KhuyenMai> HoaDonKhuyenMais { get; set; } = new List<HoaDon_KhuyenMai>();
    }

    [Table("HoaDon_KhuyenMai")]
    public class HoaDon_KhuyenMai
    {
        public int IdHoaDon { get; set; }
        public int IdKhuyenMai { get; set; }

        [ForeignKey("IdHoaDon")]
        public virtual HoaDon HoaDon { get; set; } = null!;
        [ForeignKey("IdKhuyenMai")]
        public virtual KhuyenMai KhuyenMai { get; set; } = null!;
    }

    [Table("TheLoai")]
    public class TheLoai
    {
        [Key]
        public int IdTheLoai { get; set; }
        public string TenTheLoai { get; set; } = null!;
        public string? MoTa { get; set; }
        public virtual ICollection<SachTheLoai> SachTheLoais { get; set; } = new List<SachTheLoai>();
    }

    [Table("TacGia")]
    public class TacGia
    {
        [Key]
        public int IdTacGia { get; set; }
        [Required]
        [StringLength(255)]
        public string TenTacGia { get; set; } = string.Empty;
        public string? GioiThieu { get; set; }
        public virtual ICollection<SachTacGia> SachTacGias { get; set; } = new List<SachTacGia>();
    }

    [Table("NhaXuatBan")]
    public class NhaXuatBan
    {
        [Key]
        public int IdNhaXuatBan { get; set; }
        public string TenNhaXuatBan { get; set; } = null!;
        public string? MoTa { get; set; }
        public virtual ICollection<SachNhaXuatBan> SachNhaXuatBans { get; set; } = new List<SachNhaXuatBan>();
    }

    [Table("Sach")]
    public class Sach
    {
        [Key]
        public int IdSach { get; set; }
        public string TenSach { get; set; } = null!;
        public int? NamXuatBan { get; set; }
        public string? MoTa { get; set; }
        public int SoLuongTong { get; set; }
        public int SoLuongHienCo { get; set; }
        public string? AnhBia { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? GiaBia { get; set; }
        public string? ViTri { get; set; }
        public virtual ICollection<SachTacGia> SachTacGias { get; set; } = new List<SachTacGia>();
        public virtual ICollection<SachTheLoai> SachTheLoais { get; set; } = new List<SachTheLoai>();
        public virtual ICollection<SachNhaXuatBan> SachNhaXuatBans { get; set; } = new List<SachNhaXuatBan>();
        public virtual ICollection<ChiTietPhieuThue> ChiTietPhieuThues { get; set; } = new List<ChiTietPhieuThue>();
        [InverseProperty("SachGoc")]
        public virtual ICollection<DeXuatSach> DeXuatSachGocs { get; set; } = new List<DeXuatSach>();
        [InverseProperty("SachDeXuat")]
        public virtual ICollection<DeXuatSach> DeXuatSachDeXuats { get; set; } = new List<DeXuatSach>();
        public virtual ICollection<ChiTietPhieuTra> ChiTietPhieuTras { get; set; } = new List<ChiTietPhieuTra>();
    }


    [Table("PhieuTraSach")]
    public class PhieuTraSach
    {
        [Key]
        public int IdPhieuTra { get; set; }

        public int IdPhieuThueSach { get; set; }
        public int IdNhanVien { get; set; }
        public DateTime NgayTra { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal TongPhiThue { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal TongTienPhat { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal TongTienCocHoan { get; set; }

        public int DiemTichLuy { get; set; }

        // Navigation properties
        [ForeignKey("IdPhieuThueSach")]
        public virtual PhieuThueSach PhieuThueSach { get; set; } = null!;

        [ForeignKey("IdNhanVien")]
        public virtual NhanVien NhanVien { get; set; } = null!;

        public virtual ICollection<ChiTietPhieuTra> ChiTietPhieuTras { get; set; } = new List<ChiTietPhieuTra>();
        
    }

    [Table("ChiTietPhieuTra")]
    public class ChiTietPhieuTra
    {
        public int IdPhieuTra { get; set; }
        public int IdSach { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal TienPhat { get; set; }

        // Navigation properties
        [ForeignKey("IdPhieuTra")]
        public virtual PhieuTraSach PhieuTraSach { get; set; } = null!;

        [ForeignKey("IdSach")]
        public virtual Sach Sach { get; set; } = null!;
    }

    [Table("Sach_TacGia")]
    public class SachTacGia
    {
        public int IdSach { get; set; }
        public virtual Sach Sach { get; set; } = null!;
        public int IdTacGia { get; set; }
        public virtual TacGia TacGia { get; set; } = null!;
    }

    [Table("Sach_TheLoai")]
    public class SachTheLoai
    {
        public int IdSach { get; set; }
        public virtual Sach Sach { get; set; } = null!;
        public int IdTheLoai { get; set; }
        public virtual TheLoai TheLoai { get; set; } = null!;
    }

    [Table("Sach_NhaXuatBan")]
    public class SachNhaXuatBan
    {
        public int IdSach { get; set; }
        public virtual Sach Sach { get; set; } = null!;
        public int IdNhaXuatBan { get; set; }
        public virtual NhaXuatBan NhaXuatBan { get; set; } = null!;
    }

    [Table("PhieuThueSach")]
    public class PhieuThueSach
    {
        [Key]
        public int IdPhieuThueSach { get; set; }
        public int IdKhachHang { get; set; }
        public int? IdNhanVien { get; set; }
        public DateTime NgayThue { get; set; }
        [Required]
        [StringLength(50)]
        public string TrangThai { get; set; } = string.Empty;
        [Column(TypeName = "decimal(18, 2)")]
        public decimal TongTienCoc { get; set; }
        [ForeignKey("IdKhachHang")]
        public virtual KhachHang KhachHang { get; set; } = null!;
        [ForeignKey("IdNhanVien")]
        public virtual NhanVien? NhanVien { get; set; }
        public virtual ICollection<ChiTietPhieuThue> ChiTietPhieuThues { get; set; } = new List<ChiTietPhieuThue>();
        public virtual ICollection<PhieuTraSach> PhieuTraSachs { get; set; } = new List<PhieuTraSach>();
    }

    [Table("ChiTietPhieuThue")]
    public class ChiTietPhieuThue
    {
        public int IdPhieuThueSach { get; set; }
        public int IdSach { get; set; }
        public DateTime NgayHenTra { get; set; }
        public DateTime? NgayTraThucTe { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal TienCoc { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? TienPhatTraTre { get; set; }

        [ForeignKey("IdPhieuThueSach")]
        public virtual PhieuThueSach PhieuThueSach { get; set; } = null!;
        [ForeignKey("IdSach")]
        public virtual Sach Sach { get; set; } = null!;

    }

    [Table("VaiTro")]
    public class VaiTro
    {
        [Key]
        public int IdVaiTro { get; set; }
        [Required]
        [StringLength(100)]
        public string TenVaiTro { get; set; } = string.Empty;
        [StringLength(500)]
        public string? MoTa { get; set; }

        public virtual ICollection<NhanVien> NhanViens { get; set; } = new List<NhanVien>();
        public virtual ICollection<VaiTro_Quyen> VaiTroQuyens { get; set; } = new List<VaiTro_Quyen>();
    }

    [Table("NhanVien")]
    public class NhanVien
    {
        [Key]
        public int IdNhanVien { get; set; }
        [Required]
        [StringLength(255)]
        public string HoTen { get; set; } = string.Empty;
        [Required]
        [StringLength(20)]
        public string SoDienThoai { get; set; } = string.Empty;
        [StringLength(100)]
        public string? Email { get; set; }
        [StringLength(500)]
        public string? DiaChi { get; set; }
        public DateTime NgayVaoLam { get; set; }
        public int IdVaiTro { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal LuongCoBan { get; set; }
        [Required]
        [StringLength(50)]
        public string TrangThaiLamViec { get; set; } = string.Empty;
        [Required]
        [StringLength(100)]
        public string TenDangNhap { get; set; } = string.Empty;
        [Required]
        [StringLength(255)]
        public string MatKhau { get; set; } = string.Empty;
        public string? AnhDaiDien { get; set; } // Base64

        [ForeignKey("IdVaiTro")]
        public virtual VaiTro VaiTro { get; set; } = null!;

        // --- BẮT ĐẦU SỬA LỖI ---

        public virtual ICollection<HoaDon> HoaDons { get; set; } = new List<HoaDon>();
        public virtual ICollection<NhatKyHuyMon> NhatKyHuyMons { get; set; } = new List<NhatKyHuyMon>();
        public virtual ICollection<PhieuNhapKho> PhieuNhapKhos { get; set; } = new List<PhieuNhapKho>();
        public virtual ICollection<PhieuKiemKho> PhieuKiemKhos { get; set; } = new List<PhieuKiemKho>();
        public virtual ICollection<PhieuXuatHuy> PhieuXuatHuys { get; set; } = new List<PhieuXuatHuy>();
        public virtual ICollection<PhieuThueSach> PhieuThueSachs { get; set; } = new List<PhieuThueSach>();
        public virtual ICollection<LichLamViec> LichLamViecs { get; set; } = new List<LichLamViec>();
        [InverseProperty("NhanVien")]
        public virtual ICollection<PhieuLuong> PhieuLuongs { get; set; } = new List<PhieuLuong>();
        [InverseProperty("NguoiPhat")]
        public virtual ICollection<PhieuLuong> PhieuLuongsDaPhat { get; set; } = new List<PhieuLuong>();
        [InverseProperty("NhanVien")]
        public virtual ICollection<DonXinNghi> DonXinNghis { get; set; } = new List<DonXinNghi>();
        [InverseProperty("NguoiDuyet")]
        public virtual ICollection<DonXinNghi> DonXinNghiNguoiDuyets { get; set; } = new List<DonXinNghi>();
        public virtual ICollection<ChatLichSu> ChatLichSus { get; set; } = new List<ChatLichSu>();
        public virtual ICollection<PhieuTraSach> PhieuTraSachs { get; set; } = new List<PhieuTraSach>();
        public virtual ICollection<PhieuThuongPhat> PhieuThuongPhatNguoiTaos { get; set; } = new List<PhieuThuongPhat>();

        // --- KẾT THÚC SỬA LỖI ---
    }

    [Table("CaLamViec")]
    public class CaLamViec
    {
        [Key]
        public int IdCa { get; set; }
        [Required]
        [StringLength(100)]
        public string TenCa { get; set; } = string.Empty;
        public TimeSpan GioBatDau { get; set; }
        public TimeSpan GioKetThuc { get; set; }

        public virtual ICollection<LichLamViec> LichLamViecs { get; set; } = new List<LichLamViec>();
    }

    [Table("LichLamViec")]
    public class LichLamViec
    {
        [Key]
        public int IdLichLamViec { get; set; }
        public int IdNhanVien { get; set; }
        public int IdCa { get; set; }
        public DateTime NgayLam { get; set; }

        [ForeignKey("IdNhanVien")]
        public virtual NhanVien NhanVien { get; set; } = null!;
        [ForeignKey("IdCa")]
        public virtual CaLamViec CaLamViec { get; set; } = null!;

        public virtual ICollection<BangChamCong> BangChamCongs { get; set; } = new List<BangChamCong>();
    }

    [Table("BangChamCong")]
    public class BangChamCong
    {
        [Key]
        public int IdChamCong { get; set; }
        public int IdLichLamViec { get; set; }
        public DateTime? GioVao { get; set; }
        public DateTime? GioRa { get; set; }

        // --- SỬA LỖI TỪ double? SANG decimal? VÀ THÊM TYPE ---
        [Column(TypeName = "decimal(18, 2)")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public decimal? SoGioLam { get; set; }

        [ForeignKey("IdLichLamViec")]
        public virtual LichLamViec LichLamViec { get; set; } = null!;
    }

    [Table("PhieuLuong")]
    public class PhieuLuong
    {
        [Key]
        public int IdPhieuLuong { get; set; }
        public int IdNhanVien { get; set; }
        public int Thang { get; set; }
        public int Nam { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal LuongCoBan { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal TongGioLam { get; set; }

        // --- SỬA LỖI KHỚP VỚI CSDL (cho phép NULL) ---
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? TienThuong { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? KhauTru { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal ThucLanh { get; set; }
        public DateTime NgayTao { get; set; }
        [Required]
        [StringLength(50)]
        public string TrangThai { get; set; } = string.Empty;

        [ForeignKey("IdNhanVien")]
        public virtual NhanVien NhanVien { get; set; } = null!;

        // --- SỬA LỖI KHỚP VỚI CSDL (thêm cột mới) ---
        public DateTime? NgayPhatLuong { get; set; }
        public int? IdNguoiPhat { get; set; }
        [ForeignKey("IdNguoiPhat")]
        public virtual NhanVien? NguoiPhat { get; set; }
    }

    [Table("DonXinNghi")]
    public class DonXinNghi
    {
        [Key]
        public int IdDonXinNghi { get; set; }
        public int IdNhanVien { get; set; }
        [Required]
        [StringLength(100)]
        public string LoaiDon { get; set; } = string.Empty;
        [Required]
        [StringLength(500)]
        public string LyDo { get; set; } = string.Empty;
        public DateTime NgayBatDau { get; set; }
        public DateTime NgayKetThuc { get; set; }
        [Required]
        [StringLength(50)]
        public string TrangThai { get; set; } = string.Empty;
        public int? IdNguoiDuyet { get; set; }
        public DateTime? NgayDuyet { get; set; }
        [StringLength(255)]
        public string? GhiChuPheDuyet { get; set; }

        [ForeignKey("IdNhanVien")]
        public virtual NhanVien NhanVien { get; set; } = null!;
        [ForeignKey("IdNguoiDuyet")]
        public virtual NhanVien? NguoiDuyet { get; set; }
    }

    [Table("CaiDat")]
    public class CaiDat
    {
        [Key]
        [StringLength(100)]
        public string TenCaiDat { get; set; } = string.Empty;
        [Required]
        public string GiaTri { get; set; } = string.Empty;
        [StringLength(500)]
        public string? MoTa { get; set; }
    }

    [Table("Quyen")]
    public class Quyen
    {
        [Key]
        [StringLength(100)]
        public string IdQuyen { get; set; } = string.Empty;
        [Required]
        [StringLength(255)]
        public string TenQuyen { get; set; } = string.Empty;
        [Required]
        [StringLength(100)]
        public string NhomQuyen { get; set; } = string.Empty;

        public virtual ICollection<VaiTro_Quyen> VaiTroQuyens { get; set; } = new List<VaiTro_Quyen>();
    }

    [Table("VaiTro_Quyen")]
    public class VaiTro_Quyen
    {
        public int IdVaiTro { get; set; }
        [StringLength(100)]
        public string IdQuyen { get; set; } = string.Empty;

        [ForeignKey("IdVaiTro")]
        public virtual VaiTro VaiTro { get; set; } = null!;
        [ForeignKey("IdQuyen")]
        public virtual Quyen Quyen { get; set; } = null!;
    }

    [Table("GiaoDichThanhToan")]
    public class GiaoDichThanhToan
    {
        [Key]
        public int IdGiaoDich { get; set; }
        public int IdHoaDon { get; set; }
        [Required]
        [StringLength(100)]
        public string MaGiaoDichNgoai { get; set; } = string.Empty;
        [Required]
        [StringLength(50)]
        public string CongThanhToan { get; set; } = string.Empty;
        [Column(TypeName = "decimal(18, 2)")]
        public decimal SoTien { get; set; }
        public DateTime ThoiGianGiaoDich { get; set; }
        [Required]
        [StringLength(100)]
        public string TrangThai { get; set; } = string.Empty;
        [StringLength(50)]
        public string? MaLoi { get; set; }
        [StringLength(500)]
        public string? MoTaLoi { get; set; }

        [ForeignKey("IdHoaDon")]
        public virtual HoaDon HoaDon { get; set; } = null!;
    }

    [Table("ChatLichSu")]
    public class ChatLichSu
    {
        [Key]
        public long IdChat { get; set; }
        public int? IdKhachHang { get; set; }
        public int? IdNhanVien { get; set; }
        [Required]
        public string NoiDungHoi { get; set; } = string.Empty;
        [Required]
        public string NoiDungTraLoi { get; set; } = string.Empty;
        public DateTime ThoiGian { get; set; }
        [StringLength(50)]
        public string? LoaiChat { get; set; }

        // === CẬP NHẬT (THEO FSD MỚI) ===
        [StringLength(100)]
        public string? GuestSessionId { get; set; } // Hỗ trợ khách vãng lai

        [StringLength(20)]
        public string? LoaiTinNhan { get; set; } // "KhachHang", "AI", "NhanVien"

        public int? IdThongBaoHoTro { get; set; }
        // === KẾT THÚC CẬP NHẬT ===

        [ForeignKey("IdKhachHang")]
        public virtual KhachHang? KhachHang { get; set; }
        [ForeignKey("IdNhanVien")]
        public virtual NhanVien? NhanVien { get; set; }

        [ForeignKey("IdThongBaoHoTro")]
        public virtual ThongBaoHoTro? ThongBaoHoTro { get; set; }
    }

    [Table("DeXuatSanPham")]
    public class DeXuatSanPham
    {
        public int IdSanPhamGoc { get; set; }
        public int IdSanPhamDeXuat { get; set; }
        public double DoLienQuan { get; set; }
        [Required]
        [StringLength(100)]
        public string LoaiDeXuat { get; set; } = string.Empty;

        [ForeignKey("IdSanPhamGoc")]
        public virtual SanPham SanPhamGoc { get; set; } = null!;
        [ForeignKey("IdSanPhamDeXuat")]
        public virtual SanPham SanPhamDeXuat { get; set; } = null!;
    }

    [Table("DeXuatSach")]
    public class DeXuatSach
    {
        public int IdSachGoc { get; set; }
        public int IdSachDeXuat { get; set; }
        public double DoLienQuan { get; set; }
        [Required]
        [StringLength(100)]
        public string LoaiDeXuat { get; set; } = string.Empty;

        [ForeignKey("IdSachGoc")]
        public virtual Sach SachGoc { get; set; } = null!;
        [ForeignKey("IdSachDeXuat")]
        public virtual Sach SachDeXuat { get; set; } = null!;
    }

    [Table("ThongBao")]
    public class ThongBao
    {
        [Key]
        public int IdThongBao { get; set; }

        public int? IdNhanVienTao { get; set; }

        [Required]
        [StringLength(500)]
        public string NoiDung { get; set; } = string.Empty;

        public DateTime ThoiGianTao { get; set; }

        [StringLength(50)]
        public string? LoaiThongBao { get; set; }

        public int? IdLienQuan { get; set; } // Sẽ là idBan

        public bool DaXem { get; set; }

        [ForeignKey("IdNhanVienTao")]
        public virtual NhanVien? NhanVienTao { get; set; }
    }

    [Table("PhieuThuongPhat")]
    public class PhieuThuongPhat
    {
        [Key]
        public int IdPhieuThuongPhat { get; set; }

        public int IdNhanVien { get; set; }

        public int IdNguoiTao { get; set; }

        public DateTime NgayTao { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal SoTien { get; set; }

        [Required]
        [StringLength(500)]
        public string LyDo { get; set; } = string.Empty;

        public int? IdPhieuLuong { get; set; }

        [ForeignKey("IdNhanVien")]
        public virtual NhanVien NhanVien { get; set; } = null!;

        [ForeignKey("IdNguoiTao")]
        public virtual NhanVien NguoiTao { get; set; } = null!;

        [ForeignKey("IdPhieuLuong")]
        public virtual PhieuLuong? PhieuLuong { get; set; }
    }

    [Table("TrangThaiCheBien")]
    public class TrangThaiCheBien
    {
        [Key]
        public int IdTrangThaiCheBien { get; set; }

        public int IdChiTietHoaDon { get; set; }
        public int IdHoaDon { get; set; }
        public int IdSanPham { get; set; }

        [Required]
        [StringLength(255)]
        public string TenMon { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string SoBan { get; set; } = string.Empty;

        public int SoLuong { get; set; }

        [StringLength(500)]
        public string? GhiChu { get; set; }

        [StringLength(50)]
        public string? NhomIn { get; set; }

        [Required]
        [StringLength(50)]
        public string TrangThai { get; set; } = string.Empty; // Chờ làm, Đang làm, Hoàn thành

        public DateTime ThoiGianGoi { get; set; }
        public DateTime? ThoiGianBatDau { get; set; }
        public DateTime? ThoiGianHoanThanh { get; set; }

        // Khóa ngoại
        [ForeignKey("IdChiTietHoaDon")]
        public virtual ChiTietHoaDon ChiTietHoaDon { get; set; } = null!;

        [ForeignKey("IdHoaDon")]
        public virtual HoaDon HoaDon { get; set; } = null!;

        [ForeignKey("IdSanPham")]
        public virtual SanPham SanPham { get; set; } = null!;
    }

    [Table("DanhGia")]
    public class DanhGia
    {
        [Key]
        public int idDanhGia { get; set; }

        [ForeignKey("KhachHang")]
        public int idKhachHang { get; set; }
        public virtual KhachHang? KhachHang { get; set; }

        [ForeignKey("SanPham")]
        public int? idSanPham { get; set; }
        public virtual SanPham? SanPham { get; set; }

        [ForeignKey("HoaDon")]
        public int idHoaDon { get; set; }
        public virtual HoaDon? HoaDon { get; set; }

        [Range(1, 5)]
        public int SoSao { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? BinhLuan { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? HinhAnhURL { get; set; }

        [Required]
        // CHỈ CÓ MỘT DÒNG NÀY
        public DateTime NgayTao { get; set; } 

        [Required]
        [Column(TypeName = "nvarchar(50)")]
        public string TrangThai { get; set; } = "Hiển thị";

        public virtual ICollection<PhanHoiDanhGia> PhanHoiDanhGias { get; set; } = new List<PhanHoiDanhGia>();

        public DanhGia()
        {
            // Constructor gán giá trị cho thuộc tính NgayTao ở trên
            this.NgayTao = DateTime.Now;
        }
    }

    [Table("PhanHoiDanhGia")]
    public class PhanHoiDanhGia
    {
        [Key]
        public int idPhanHoi { get; set; }

        [ForeignKey("DanhGia")]
        public int idDanhGia { get; set; }
        public virtual DanhGia? DanhGia { get; set; }

        [ForeignKey("NhanVien")]
        public int idNhanVien { get; set; }
        public virtual NhanVien? NhanVien { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(max)")]
        public string NoiDung { get; set; } = null!;

        [Required]
        // CHỈ CÓ MỘT DÒNG NÀY
        public DateTime NgayTao { get; set; } 

        public PhanHoiDanhGia()
        {
            // Constructor gán giá trị cho thuộc tính NgayTao ở trên
            this.NgayTao = DateTime.Now;
        }
    }

    [Table("ThongBaoHoTro")]
    public class ThongBaoHoTro
    {
        [Key]
        public int IdThongBao { get; set; }

        // ======================================
        // === SỬA LỖI CS0266 TẠI ĐÂY ===
        // ======================================
        // 1. Xóa [Required]
        // 2. Thêm dấu '?' để cho phép NULL
        public int? IdKhachHang { get; set; }
        // ======================================

        [StringLength(100)]
        public string? GuestSessionId { get; set; } // Hỗ trợ khách vãng lai

        public string? NoiDungYeuCau { get; set; }

        [Required]
        public DateTime ThoiGianTao { get; set; }

        [Required]
        [StringLength(50)]
        public string TrangThai { get; set; } = string.Empty;

        public int? IdNhanVien { get; set; }

        public DateTime? ThoiGianPhanHoi { get; set; }

        [StringLength(500)]
        public string? GhiChu { get; set; }

        // Navigation Properties
        [ForeignKey("IdKhachHang")]
        public virtual KhachHang? KhachHang { get; set; } // Thêm '?'

        [ForeignKey("IdNhanVien")]
        public virtual NhanVien? NhanVien { get; set; }

        public virtual ICollection<ChatLichSu> ChatLichSus { get; set; } = new List<ChatLichSu>();
    }
}