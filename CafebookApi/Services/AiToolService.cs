using CafebookApi.Data;
using CafebookModel.Model.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CafebookApi.Services
{
    public class AiToolService
    {
        private readonly CafebookDbContext _context;
        private const int SlotDurationHours = 2;

        // --- ĐỊNH NGHĨA LINK (Cập nhật theo yêu cầu) ---
        private const string LinkLienHe = "/LienHeView";
        private const string LinkTaiKhoan = "/Account/TaiKhoanTongQuanView";
        private const string LinkThongTinCaNhan = "/Account/ThongTinCaNhanView";
        private const string LinkLichSuThue = "/Account/LichSuThueSachView";
        private const string LinkLichSuDatBan = "/Account/LichSuDatBanView";
        private const string LinkLichSuDonHang = "/Account/LichSuDonHangView";
        private const string LinkDoiMatKhau = "/Account/DoiMatKhauView";

        // Các link chi tiết (Dạng /View/Id)
        private const string LinkDetailDonHang = "/Account/LichSuDonHangDetailView/";
        private const string LinkDetailSach = "/ChiTietSachView/";
        private const string LinkDetailSP = "/san-pham/";
        private const string LinkChinhSach = "/Privacy";

        public AiToolService(CafebookDbContext context)
        {
            _context = context;
        }

        // ==================================================================================
        // NHÓM 1: THÔNG TIN CHUNG & CẤU HÌNH
        // ==================================================================================

        public async Task<object> GetThongTinChungAsync()
        {
            var keys = new List<string> {
                "TenQuan", "DiaChi", "SoDienThoai", "GioiThieu",
                "Wifi_Ten", "Wifi_MatKhau",
                "LienHe_GioMoCua", "LienHe_Facebook", "LienHe_Zalo", "LienHe_Email", "LienHe_Website",
                "DiemTichLuy_DoiVND", "DiemTichLuy_NhanVND",
                "Sach_SoNgayMuonToiDa", "Sach_PhiThue", "Sach_PhiTraTreMoiNgay", "Sach_DiemPhieuThue"
            };

            var settings = await _context.CaiDats.AsNoTracking()
                .Where(c => keys.Contains(c.TenCaiDat))
                .ToDictionaryAsync(c => c.TenCaiDat, c => c.GiaTri);

            string GetVal(string key) => settings.ContainsKey(key) ? settings[key] : "Chưa cập nhật";
            string FormatMoney(string val) => double.TryParse(val, out double d) ? $"{d:N0}đ" : val;

            return new
            {
                ThongTinCoBan = new
                {
                    TenQuan = GetVal("TenQuan"),
                    DiaChi = GetVal("DiaChi"),
                    Hotline = GetVal("SoDienThoai"),
                    GioMoCua = GetVal("LienHe_GioMoCua"),
                    GioiThieu = GetVal("GioiThieu")
                },
                Wifi = new
                {
                    TenMang = GetVal("Wifi_Ten"),
                    MatKhau = GetVal("Wifi_MatKhau")
                },
                QuyDinhSach = new
                {
                    HanMuon = $"{GetVal("Sach_SoNgayMuonToiDa")} ngày",
                    PhiThue = $"{FormatMoney(GetVal("Sach_PhiThue"))} (trừ khi trả sách)",
                    PhatQuaHan = $"{FormatMoney(GetVal("Sach_PhiTraTreMoiNgay"))}/ngày",
                    DiemThuong = $"{GetVal("Sach_DiemPhieuThue")} điểm/phiếu"
                },
                // Cập nhật chính sách điểm: Đổi tối đa 50% hóa đơn
                ChinhSachDiem = new
                {
                    TichLuy = $"Mỗi {FormatMoney(GetVal("DiemTichLuy_NhanVND"))} trên hóa đơn = 1 điểm",
                    QuyDoi = $"1 điểm = {FormatMoney(GetVal("DiemTichLuy_DoiVND"))} (Được dùng điểm thanh toán tối đa 50% giá trị đơn hàng)"
                },
                LienKetXH = new
                {
                    Facebook = GetVal("LienHe_Facebook"),
                    Zalo = GetVal("LienHe_Zalo"),
                    Website = GetVal("LienHe_Website")
                },
                Actions = new List<object>
                {
                    new { Label = "Thông tin liên hệ", Link = LinkLienHe },
                    new { Label = "Chính sách & Quy định", Link = LinkChinhSach }
                }
            };
        }

        public async Task<object> GetKhuyenMaiAsync()
        {
            var now = DateTime.Now;
            var listKM = await _context.KhuyenMais.AsNoTracking()
                .Where(k => k.TrangThai == "Hoạt động" && k.NgayBatDau <= now && k.NgayKetThuc >= now)
                .OrderByDescending(k => k.GiaTriGiam)
                .Take(3)
                .Select(k => new
                {
                    ChuongTrinh = k.TenChuongTrinh,
                    Giam = k.LoaiGiamGia == "Phần trăm" ? $"{k.GiaTriGiam}%" : $"{k.GiaTriGiam:N0}đ",
                    MoTa = k.MoTa,
                    Han = k.NgayKetThuc.ToString("dd/MM/yyyy")
                })
                .ToListAsync();

            if (!listKM.Any()) return "Hiện tại quán chưa có chương trình khuyến mãi nào đang diễn ra.";
            return new { DanhSachKhuyenMai = listKM };
        }

        // ==================================================================================
        // NHÓM 2: F&B (SẢN PHẨM)
        // ==================================================================================

        public async Task<object> KiemTraSanPhamAsync(string keyword)
        {
            var sp = await _context.SanPhams.AsNoTracking()
                .Include(s => s.DanhMuc)
                .Where(s => s.TenSanPham.Contains(keyword))
                .FirstOrDefaultAsync();

            if (sp == null) return new { Status = "NotFound", Message = "Không tìm thấy món này trong menu." };

            return new
            {
                Name = sp.TenSanPham,
                Category = sp.DanhMuc?.TenDanhMuc ?? "Khác",
                Price = sp.GiaBan,
                Desc = sp.MoTa,
                Status = sp.TrangThaiKinhDoanh ? "Đang kinh doanh" : "Ngừng kinh doanh",
                Actions = new[] { new { Label = "Xem chi tiết món", Link = $"{LinkDetailSP}{sp.IdSanPham}" } }
            };
        }

        public async Task<object> TimMonTheoLoaiAsync(string loaiMon)
        {
            var list = await _context.SanPhams.AsNoTracking()
                .Include(s => s.DanhMuc)
                .Where(s => s.DanhMuc.TenDanhMuc.Contains(loaiMon) && s.TrangThaiKinhDoanh == true)
                .OrderBy(s => s.TenSanPham)
                .Take(5)
                .Select(s => new
                {
                    Ten = s.TenSanPham,
                    Gia = s.GiaBan,
                    MoTa = s.MoTa,
                    Link = $"{LinkDetailSP}{s.IdSanPham}" // Link chi tiết
                })
                .ToListAsync();

            if (!list.Any()) return new { Message = $"Không tìm thấy loại món nào tên là '{loaiMon}'." };

            // TẠO ACTIONS TỪ DANH SÁCH
            var actions = list.Select(s => new { Label = s.Ten, Link = s.Link }).ToList();

            return new
            {
                Message = $"Tìm thấy {list.Count} món thuộc loại '{loaiMon}':",
                DanhSach = list,
                Actions = actions // <--- QUAN TRỌNG: AI sẽ dùng cái này tạo nút
            };
        }

        public async Task<object> GetGoiYSanPhamAsync()
        {
            var list = await _context.SanPhams.AsNoTracking()
                .Where(s => s.TrangThaiKinhDoanh == true)
                .OrderBy(r => Guid.NewGuid())
                .Take(3)
                .Select(s => new
                {
                    Ten = s.TenSanPham,
                    Gia = s.GiaBan,
                    MoTa = s.MoTa,
                    Link = $"{LinkDetailSP}{s.IdSanPham}"
                })
                .ToListAsync();

            var actions = list.Select(s => new { Label = s.Ten, Link = s.Link }).ToList();

            return new { Message = "Một vài món ngon hôm nay:", DanhSach = list, Actions = actions };
        }

        // ==================================================================================
        // NHÓM 3: THƯ VIỆN SÁCH
        // ==================================================================================

        public async Task<object> KiemTraSachAsync(string keyword)
        {
            var sach = await _context.Sachs.AsNoTracking()
                .Include(s => s.SachTacGias).ThenInclude(st => st.TacGia)
                .Where(s => s.TenSach.Contains(keyword))
                .FirstOrDefaultAsync();

            if (sach == null) return new { Status = "NotFound", Message = "Không tìm thấy sách này." };

            return new
            {
                Name = sach.TenSach,
                Author = string.Join(", ", sach.SachTacGias.Select(x => x.TacGia.TenTacGia)),
                Location = sach.ViTri,
                Stock = sach.SoLuongHienCo,
                Status = sach.SoLuongHienCo > 0 ? "Có sẵn" : "Đã hết",
                Actions = new[] { new { Label = "Xem thông tin sách", Link = $"{LinkDetailSach}{sach.IdSach}" } }
            };
        }

        public async Task<object> TimSachTheoTacGiaAsync(string tenTacGia)
        {
            var list = await _context.Sachs.AsNoTracking()
                .Include(s => s.SachTacGias).ThenInclude(st => st.TacGia)
                .Where(s => s.SachTacGias.Any(t => t.TacGia.TenTacGia.Contains(tenTacGia)))
                .Take(5)
                .Select(s => new
                {
                    Ten = s.TenSach,
                    ViTri = s.ViTri,
                    Link = $"{LinkDetailSach}{s.IdSach}"
                })
                .ToListAsync();

            if (!list.Any()) return new { Message = $"Không tìm thấy sách nào của tác giả '{tenTacGia}'." };

            var actions = list.Select(s => new { Label = s.Ten, Link = s.Link }).ToList();

            return new { Message = $"Sách của tác giả {tenTacGia}:", DanhSach = list, Actions = actions };
        }

        public async Task<object> GetGoiYSachAsync()
        {
            var list = await _context.Sachs.AsNoTracking()
                .Where(s => s.SoLuongHienCo > 0)
                .OrderBy(r => Guid.NewGuid())
                .Take(3)
                .Select(s => new
                {
                    Ten = s.TenSach,
                    TacGia = string.Join(", ", s.SachTacGias.Select(st => st.TacGia.TenTacGia)),
                    Link = $"{LinkDetailSach}{s.IdSach}"
                })
                .ToListAsync();

            var actions = list.Select(s => new { Label = s.Ten, Link = s.Link }).ToList();

            return new { Message = "Những cuốn sách thú vị:", DanhSach = list, Actions = actions };
        }

        // ==================================================================================
        // NHÓM 4: ĐẶT BÀN
        // ==================================================================================

        public async Task<object> KiemTraBanTrongAsync(int soNguoi)
        {
            var bans = await _context.Bans.AsNoTracking()
                .Include(b => b.KhuVuc)
                .Where(b => b.SoGhe >= soNguoi && b.TrangThai != "Hỏng" && b.TrangThai != "Bảo trì" && b.TrangThai != "Đã Đặt")
                .OrderBy(b => b.SoGhe)
                .Take(6)
                .Select(b => new
                {
                    TenBan = b.SoBan,
                    SoGhe = b.SoGhe,
                    KhuVuc = b.KhuVuc != null ? b.KhuVuc.TenKhuVuc : "Chung",
                    Mota = $"Bàn {b.SoBan} ({b.SoGhe} ghế) - {(b.KhuVuc != null ? b.KhuVuc.TenKhuVuc : "Chung")}"
                })
                .ToListAsync();

            if (!bans.Any()) return new { Message = "Rất tiếc, không tìm thấy bàn nào phù hợp với số lượng người này." };
            return new { DanhSachBanTrong = bans, Note = "Vui lòng chọn một bàn từ danh sách trên." };
        }

        public async Task<object> DatBanThucSuAsync(string tenBan, int soNguoi, DateTime thoiGianDat, string hoTen, string sdt, string email, string ghiChu, int? idKhachHang)
        {
            if (thoiGianDat < DateTime.Now.AddMinutes(10))
                return new { Error = "Vui lòng đặt trước ít nhất 15 phút so với hiện tại." };

            var openingHours = await GetAndParseOpeningHours();
            if (!IsTimeValid(thoiGianDat, openingHours))
            {
                return new { Error = $"Quán đóng cửa vào giờ đó. Giờ mở cửa: {openingHours.Open:hh\\:mm} - {openingHours.Close:hh\\:mm}" };
            }

            var ban = await _context.Bans.FirstOrDefaultAsync(b => b.SoBan == tenBan || b.SoBan.Contains(tenBan));
            if (ban == null) return new { Error = $"Không tìm thấy bàn tên '{tenBan}'." };

            DateTime thoiGianKetThuc = thoiGianDat.AddHours(SlotDurationHours);
            bool isConflict = await _context.PhieuDatBans.AnyAsync(p =>
                p.IdBan == ban.IdBan &&
                p.TrangThai != "Đã Hủy" && p.TrangThai != "Hoàn thành" &&
                (
                    (thoiGianDat >= p.ThoiGianDat && thoiGianDat < p.ThoiGianDat.AddHours(SlotDurationHours)) ||
                    (thoiGianKetThuc > p.ThoiGianDat && thoiGianKetThuc <= p.ThoiGianDat.AddHours(SlotDurationHours)) ||
                    (thoiGianDat <= p.ThoiGianDat && thoiGianKetThuc >= p.ThoiGianDat.AddHours(SlotDurationHours))
                )
            );

            if (isConflict)
                return new { Error = $"Rất tiếc, bàn {ban.SoBan} đã có người đặt trong khung giờ {thoiGianDat:HH:mm}." };

            int finalIdKhach;
            if (idKhachHang.HasValue && idKhachHang > 0)
            {
                finalIdKhach = idKhachHang.Value;
            }
            else
            {
                var guest = await _context.KhachHangs.FirstOrDefaultAsync(k => k.SoDienThoai == sdt);
                if (guest == null)
                {
                    guest = new KhachHang
                    {
                        HoTen = hoTen,
                        SoDienThoai = sdt,
                        Email = string.IsNullOrEmpty(email) ? null : email,
                        TaiKhoanTam = true,
                        TenDangNhap = sdt,
                        MatKhau = Guid.NewGuid().ToString("N").Substring(0, 8),
                        NgayTao = DateTime.Now,
                        BiKhoa = false
                    };
                    _context.KhachHangs.Add(guest);
                    await _context.SaveChangesAsync();
                }
                finalIdKhach = guest.IdKhachHang;
            }

            var phieu = new PhieuDatBan
            {
                IdBan = ban.IdBan,
                IdKhachHang = finalIdKhach,
                SoLuongKhach = soNguoi,
                ThoiGianDat = thoiGianDat,
                GhiChu = ghiChu,
                TrangThai = "Chờ xác nhận",
                HoTenKhach = hoTen,
                SdtKhach = sdt
            };
            _context.PhieuDatBans.Add(phieu);

            var tb = new ThongBao
            {
                NoiDung = $"Khách {hoTen} ({sdt}) đặt {ban.SoBan} lúc {thoiGianDat:HH:mm dd/MM}",
                LoaiThongBao = "DatBan",
                ThoiGianTao = DateTime.Now,
                DaXem = false,
                IdLienQuan = phieu.IdPhieuDatBan
            };
            _context.ThongBaos.Add(tb);
            await _context.SaveChangesAsync();

            return new
            {
                Status = "Success",
                Message = $"Đặt bàn {ban.SoBan} thành công! Mã phiếu: {phieu.IdPhieuDatBan}.",
                CanhBao = "Lưu ý: Bàn sẽ tự động hủy nếu quý khách đến trễ quá 15 phút.",
                Actions = new[] { new { Label = "Quản lý đặt bàn", Link = LinkLichSuDatBan } }
            };
        }

        // --- HELPER CLASSES ---
        private class OpeningHours
        {
            public TimeSpan Open { get; set; } = new TimeSpan(6, 0, 0);
            public TimeSpan Close { get; set; } = new TimeSpan(23, 0, 0);
        }

        private async Task<OpeningHours> GetAndParseOpeningHours()
        {
            var setting = await _context.CaiDats.AsNoTracking().FirstOrDefaultAsync(cd => cd.TenCaiDat == "LienHe_GioMoCua");
            string settingValue = (setting != null && !string.IsNullOrEmpty(setting.GiaTri)) ? setting.GiaTri : "06:00 - 23:00";
            var hours = new OpeningHours();
            try
            {
                var match = Regex.Match(settingValue, @"(\d{1,2}:\d{2})\s*-\s*(\d{1,2}:\d{2})");
                if (match.Success)
                {
                    if (TimeSpan.TryParse(match.Groups[1].Value, out TimeSpan open)) hours.Open = open;
                    if (TimeSpan.TryParse(match.Groups[2].Value, out TimeSpan close)) hours.Close = close;
                }
            }
            catch { }
            return hours;
        }

        private bool IsTimeValid(DateTime thoiGianDat, OpeningHours hours)
        {
            var timeOfDay = thoiGianDat.TimeOfDay;
            return timeOfDay >= hours.Open && timeOfDay <= hours.Close;
        }

        // ==================================================================================
        // NHÓM 5: CÁ NHÂN & LỊCH SỬ (LOGIC MỚI: THEO THÁNG, TRỄ HẠN, SẮP HẾT HẠN)
        // ==================================================================================

        private string MaskInfo(string input)
        {
            if (string.IsNullOrEmpty(input) || input.Length < 4) return "***";
            return input.Substring(0, 3) + "***" + input.Substring(input.Length - 2);
        }
        /*
        public async Task<object> GetTongQuanTaiKhoanAsync(int idKhachHang)
        {
            var kh = await _context.KhachHangs.FindAsync(idKhachHang);
            if (kh == null) return "Lỗi: Không tìm thấy tài khoản.";

            // Đã bỏ trường "LoaiTK" (Hạng thành viên) theo yêu cầu
            return new
            {
                HoTen = kh.HoTen,
                DiemTichLuy = kh.DiemTichLuy, // Chỉ trả về điểm
                SDT = MaskInfo(kh.SoDienThoai ?? ""),
                Email = MaskInfo(kh.Email ?? ""),
                Actions = new List<object>
                {
                    new { Label = "Tổng quan tài khoản", Link =  LinkTaiKhoan },
                    new { Label = "Thông tin cá nhân", Link = LinkThongTinCaNhan }
                }
            };
        }
        */
        // TOOL 1: CHỈ LẤY ĐIỂM TÍCH LŨY
        public async Task<object> GetDiemTichLuyAsync(int idKhachHang)
        {
            var kh = await _context.KhachHangs.AsNoTracking()
                .Where(k => k.IdKhachHang == idKhachHang)
                .Select(k => new { k.HoTen, k.DiemTichLuy })
                .FirstOrDefaultAsync();

            if (kh == null) return "Lỗi: Không tìm thấy tài khoản.";

            return new
            {
                Message = $"Tài khoản {kh.HoTen} hiện có {kh.DiemTichLuy} điểm.",
                DiemTichLuy = kh.DiemTichLuy,
                Actions = new[] { new { Label = "Xem tổng quan tài khoản", Link = LinkTaiKhoan } }
            };
        }

        // TOOL 2: LẤY THÔNG TIN CÁ NHÂN (FULL)
        public async Task<object> GetThongTinCaNhanAsync(int idKhachHang)
        {
            var kh = await _context.KhachHangs.FindAsync(idKhachHang);
            if (kh == null) return "Lỗi: Không tìm thấy tài khoản.";

            return new
            {
                HoTen = kh.HoTen,
                SDT = MaskInfo(kh.SoDienThoai ?? ""),
                Email = MaskInfo(kh.Email ?? ""),
                NgayThamGia = kh.NgayTao.ToString("dd/MM/yyyy"),
                Actions = new List<object>
                {
                    new { Label = "Chỉnh sửa thông tin", Link = LinkThongTinCaNhan },
                    new { Label = "Đổi mật khẩu", Link = LinkDoiMatKhau }
                }
            };
        }

        // Lịch sử đặt bàn: Lấy danh sách trong tháng + Danh sách hôm nay
        public async Task<object> GetLichSuDatBanAsync(int idKhachHang)
        {
            var today = DateTime.Today;
            var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);

            // 1. Bàn đặt hôm nay
            var homNay = await _context.PhieuDatBans.AsNoTracking()
                .Where(p => p.IdKhachHang == idKhachHang && p.ThoiGianDat.Date == today)
                .OrderBy(p => p.ThoiGianDat)
                .Select(p => new
                {
                    MaPhieu = p.IdPhieuDatBan,
                    Gio = p.ThoiGianDat.ToString("HH:mm"),
                    Ban = p.Ban.SoBan,
                    TrangThai = p.TrangThai,
                    GhiChu = p.GhiChu
                })
                .ToListAsync();

            // 2. Lịch sử trong tháng này (Bao gồm cả hôm nay, để khách xem tổng quát)
            var lichSuThangNay = await _context.PhieuDatBans.AsNoTracking()
                .Where(p => p.IdKhachHang == idKhachHang && p.ThoiGianDat >= firstDayOfMonth)
                .OrderByDescending(p => p.ThoiGianDat)
                .Select(p => new
                {
                    Ngay = p.ThoiGianDat.ToString("dd/MM/yyyy HH:mm"),
                    Ban = p.Ban.SoBan,
                    TrangThai = p.TrangThai
                })
                .ToListAsync();

            if (!homNay.Any() && !lichSuThangNay.Any())
                return "Bạn chưa có lịch sử đặt bàn trong tháng này.";

            return new
            {
                DatBanHomNay = homNay.Any() ? homNay : null,
                LichSuThangNay = lichSuThangNay,
                Actions = new[] { new { Label = "Quản lý đặt bàn", Link = LinkLichSuDatBan } }
            };
        }

        public async Task<object> HuyDatBanAsync(int idPhieuDat, string lyDo, int idKhachHang)
        {
            var phieu = await _context.PhieuDatBans
                .FirstOrDefaultAsync(p => p.IdPhieuDatBan == idPhieuDat && p.IdKhachHang == idKhachHang);

            if (phieu == null) return "Không tìm thấy phiếu đặt bàn này.";
            if (phieu.TrangThai == "Đã Hủy" || phieu.TrangThai == "Hoàn thành")
                return $"Phiếu này đang ở trạng thái '{phieu.TrangThai}', không thể hủy.";

            phieu.TrangThai = "Đã Hủy";
            phieu.GhiChu += $" | Khách tự hủy: {lyDo}";

            var tb = new ThongBao
            {
                NoiDung = $"Khách hàng đã tự hủy phiếu đặt bàn #{idPhieuDat}. Lý do: {lyDo}",
                LoaiThongBao = "HuyDatBan",
                ThoiGianTao = DateTime.Now,
                DaXem = false,
                IdLienQuan = idPhieuDat
            };
            _context.ThongBaos.Add(tb);
            await _context.SaveChangesAsync();

            return new
            {
                Status = "Success",
                Message = $"Đã hủy thành công phiếu đặt bàn #{idPhieuDat}.",
                Actions = new[] { new { Label = "Xem lại lịch sử", Link = LinkLichSuDatBan } }
            };
        }

        // Lịch sử thuê sách: Trong tháng, Trễ hạn, Sắp hết hạn
        public async Task<object> GetLichSuThueSachAsync(int idKhachHang)
        {
            var settingPhiPhat = await _context.CaiDats.FirstOrDefaultAsync(c => c.TenCaiDat == "Sach_PhiTraTreMoiNgay");
            decimal phiPhatNgay = 5000;
            if (settingPhiPhat != null && decimal.TryParse(settingPhiPhat.GiaTri, out decimal parsed)) phiPhatNgay = parsed;

            var today = DateTime.Now;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);

            // 1. Danh sách thuê trong tháng này
            var thueThangNay = await _context.PhieuThueSachs.AsNoTracking()
                .Where(p => p.IdKhachHang == idKhachHang && p.NgayThue >= startOfMonth)
                .OrderByDescending(p => p.NgayThue)
                .Select(p => new
                {
                    NgayThue = p.NgayThue.ToString("dd/MM/yyyy"),
                    TongCoc = p.TongTienCoc,
                    TrangThai = p.TrangThai,
                    Sach = p.ChiTietPhieuThues.Select(ct => ct.Sach.TenSach).ToList()
                })
                .ToListAsync();

            // Lấy tất cả sách ĐANG THUÊ (chưa trả) của khách để check hạn
            var listDangThue = await _context.ChiTietPhieuThues.AsNoTracking()
                .Include(ct => ct.Sach)
                .Include(ct => ct.PhieuThueSach)
                .Where(ct => ct.PhieuThueSach.IdKhachHang == idKhachHang && ct.NgayTraThucTe == null && ct.PhieuThueSach.TrangThai == "DangThue")
                .ToListAsync();

            var sachTreHan = new List<object>();
            var sachSapHetHan = new List<object>();
            decimal tongPhatDuKien = 0;

            foreach (var item in listDangThue)
            {
                double daysDiff = (item.NgayHenTra - today).TotalDays;

                // 2. Check Trễ hạn (Quá hạn)
                if (item.NgayHenTra < today)
                {
                    int daysLate = (today - item.NgayHenTra).Days;
                    decimal tienPhat = daysLate * phiPhatNgay;
                    tongPhatDuKien += tienPhat;
                    sachTreHan.Add(new { Ten = item.Sach.TenSach, Tre = $"{daysLate} ngày", Phat = tienPhat });
                }
                // 3. Check Sắp hết hạn (Còn <= 1 ngày)
                else if (daysDiff <= 1 && daysDiff >= 0)
                {
                    string timeRemain = daysDiff < 1 ? "trong hôm nay" : "1 ngày nữa";
                    sachSapHetHan.Add(new { Ten = item.Sach.TenSach, HanTra = item.NgayHenTra.ToString("dd/MM/yyyy"), Note = $"Hết hạn {timeRemain}" });
                }
            }

            // 4. Tổng kết tài khoản (Giữ nguyên)
            var tongHop = await _context.PhieuTraSachs.AsNoTracking()
               .Include(pt => pt.PhieuThueSach)
               .Where(pt => pt.PhieuThueSach.IdKhachHang == idKhachHang)
               .GroupBy(x => 1)
               .Select(g => new
               {
                   TongSachDaTra = g.Sum(x => x.PhieuThueSach.ChiTietPhieuThues.Count),
                   TongPhiThue = g.Sum(x => x.TongPhiThue),
                   TongTienPhat = g.Sum(x => x.TongTienPhat)
               })
               .FirstOrDefaultAsync();

            if (!thueThangNay.Any() && tongHop == null) return "Bạn chưa có lịch sử thuê sách nào.";

            return new
            {
                LichSuThangNay = thueThangNay,
                CanhBaoTreHan = sachTreHan.Any() ? sachTreHan : null,
                CanhBaoSapHetHan = sachSapHetHan.Any() ? sachSapHetHan : null, // Thêm mục này
                TongTienPhatDuKien = tongPhatDuKien > 0 ? (decimal?)tongPhatDuKien : null,
                TongKetTaiKhoan = tongHop ?? new { TongSachDaTra = 0, TongPhiThue = 0.0m, TongTienPhat = 0.0m },
                Actions = new[] { new { Label = "Chi tiết lịch sử sách", Link = LinkLichSuThue } }
            };
        }

        public async Task<object> GetLichSuDonHangAsync(int idKhachHang)
        {
            // 1. Đơn hàng mới nhất (Để check trạng thái realtime)
            var donMoiNhat = await _context.HoaDons.AsNoTracking()
                .Where(h => h.IdKhachHang == idKhachHang)
                .OrderByDescending(h => h.ThoiGianTao)
                .Select(h => new
                {
                    MaDon = h.IdHoaDon,
                    NgayDat = h.ThoiGianTao.ToString("dd/MM HH:mm"),
                    TongTien = h.ThanhTien,
                    TrangThaiHienTai = h.TrangThaiGiaoHang ?? h.TrangThai,
                    MonAn = h.ChiTietHoaDons.Take(3).Select(ct => ct.SanPham.TenSanPham).ToList()
                })
                .FirstOrDefaultAsync();

            // 2. Danh sách lịch sử rút gọn
            var lichSu = await _context.HoaDons.AsNoTracking()
                .Where(h => h.IdKhachHang == idKhachHang)
                .OrderByDescending(h => h.ThoiGianTao)
                .Skip(1).Take(3)
                .Select(h => new
                {
                    Ngay = h.ThoiGianTao.ToString("dd/MM"),
                    Tien = h.ThanhTien,
                    TrangThai = h.TrangThai
                })
                .ToListAsync();

            if (donMoiNhat == null) return "Bạn chưa có đơn hàng nào.";

            return new
            {
                DonHangGanNhat = donMoiNhat,
                LichSuKhac = lichSu,
                Actions = new[]
                {
                    // Cập nhật link theo yêu cầu: /LinkDetailDonHang/Id
                    new { Label = "Theo dõi đơn này", Link = $"{LinkDetailDonHang}{donMoiNhat.MaDon}" },
                    new { Label = "Xem tất cả đơn", Link = LinkLichSuDonHang }
                }
            };
        }

        public async Task<object> TheoDoiDonHangAsync(int idHoaDon, int idKhachHang)
        {
            var hd = await _context.HoaDons.AsNoTracking()
                .FirstOrDefaultAsync(h => h.IdHoaDon == idHoaDon && h.IdKhachHang == idKhachHang);

            if (hd == null) return "Không tìm thấy đơn hàng này.";

            return new
            {
                MaDon = hd.IdHoaDon,
                TrangThai = hd.TrangThaiGiaoHang ?? hd.TrangThai,
                TongTien = hd.ThanhTien,
                // Cập nhật link chi tiết
                Actions = new[] { new { Label = "Xem chi tiết", Link = $"{LinkDetailDonHang}{hd.IdHoaDon}" } }
            };
        }
    }
}