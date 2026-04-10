// =========================================
// LOGIC SƠ ĐỒ BÀN (NHÂN VIÊN)
// =========================================

// Thay thế $(document).ready() bằng cú pháp $(function() { ... })
$(function () {

    // Chỉ chạy code này nếu chúng ta ở đúng trang
    const $container = $("#table-layout-container");
    if ($container.length > 0) {

        const apiBaseUrl = "http://localhost:5166";
        let currentSelectedBan = null;
        let currentNhanVienId = null;

        // Lấy ID nhân viên từ attribute (do C# render ra)
        currentNhanVienId = $container.data("nhanvien-id");

        // Hàm để tải và hiển thị bàn
        function loadSoDoBan() {
            // Chỉ hiển thị loading ở lần tải đầu
            if ($container.children(".spinner-border").length > 0) {
                $container.html('<div class="text-center w-100 p-5"><div class="spinner-border text-primary" role="status"></div><p>Đang tải...</p></div>');
            }

            $.ajax({
                url: `${apiBaseUrl}/api/app/sodoban/tables`,
                type: 'GET',
                dataType: 'json',
                success: function (tables) {
                    $container.empty(); // Xóa loading/bàn cũ
                    if (!tables || tables.length === 0) {
                        $container.html("<p>Không tìm thấy bàn nào.</p>");
                        return;
                    }

                    tables.forEach(ban => {
                        let statusClass = '';
                        let statusIcon = '';
                        switch (ban.trangThai) {
                            case 'Trống':
                                statusClass = 'status-trong';
                                statusIcon = 'fa-solid fa-check';
                                break;
                            case 'Có khách':
                                statusClass = 'status-co-khach';
                                statusIcon = 'fa-solid fa-users';
                                break;
                            case 'Đã đặt':
                                statusClass = 'status-da-dat';
                                statusIcon = 'fa-solid fa-clock';
                                break;
                            case 'Bảo trì':
                            case 'Tạm ngưng':
                            default:
                                statusClass = 'status-bao-tri';
                                statusIcon = 'fa-solid fa-triangle-exclamation';
                                break;
                        }

                        let tongTienHtml = ban.trangThai === 'Có khách'
                            ? `<p class="table-price">${ban.tongTienHienTai.toLocaleString('vi-VN')} đ</p>` : '';

                        // Truyền tất cả dữ liệu vào data-*
                        let tableCardHtml = `
                            <div class="table-card ${statusClass}" 
                                 data-id-ban="${ban.idBan}"
                                 data-id-hoadon="${ban.idHoaDonHienTai || ''}"
                                 data-so-ban="${ban.soBan}"
                                 data-trang-thai="${ban.trangThai}"
                                 data-ghi-chu="${ban.ghiChu || ''}"
                                 data-tong-tien="${ban.tongTienHienTai}">
                                <div class="table-icon"><i class="${statusIcon}"></i></div>
                                <h5 class="table-name">${ban.soBan}</h5>
                                <p class="table-status">${ban.trangThai}</p>
                                ${tongTienHtml}
                            </div>
                        `;
                        $container.append(tableCardHtml);
                    });

                    // Cập nhật lại trạng thái (nếu bàn đang chọn bị thay đổi)
                    if (currentSelectedBan) {
                        const $selectedCard = $container.find(`.table-card[data-id-ban="${currentSelectedBan.idBan}"]`);
                        if ($selectedCard.length > 0) {
                            $selectedCard.addClass('selected');
                            // Cập nhật lại thông tin panel nếu cần
                            updatePanel($selectedCard.data());
                        } else {
                            // Bàn đã bị xóa? Reset panel
                            resetPanel();
                        }
                    }
                },
                error: function (xhr, status, error) {
                    $container.html(`<div class="alert alert-danger w-100">Không thể tải sơ đồ bàn. Đảm bảo API (localhost:5166) đang chạy. Lỗi: ${error}</div>`);
                }
            });
        }

        // --- HÀM CẬP NHẬT PANEL THAO TÁC ---
        function updatePanel(banData) {
            // 1. Lưu dữ liệu bàn đã chọn (giống _selectedBan)
            currentSelectedBan = {
                idBan: banData.idBan,
                idHoaDonHienTai: banData.idHoadon,
                soBan: banData.soBan,
                trangThai: banData.trangThai,
                ghiChu: banData.ghiChu,
                tongTienHienTai: banData.tongTien
            };

            // 2. Ẩn placeholder, hiện panel chi tiết
            $("#panelChuaChon").hide();
            $("#panelDaChon").show();

            // 3. Điền thông tin vào panel
            $("#runSoBan").text(currentSelectedBan.soBan);
            $("#runTrangThai").text(currentSelectedBan.trangThai);

            if (currentSelectedBan.ghiChu) {
                $("#tbGhiChu").text(`Ghi chú: ${currentSelectedBan.ghiChu}`).show();
            } else {
                $("#tbGhiChu").hide();
            }

            // 4. Logic hiển thị/ẩn nút (Y HỆT WPF)
            switch (currentSelectedBan.trangThai) {
                case "Trống":
                    $("#btnGoiMon").text("Tạo Hóa Đơn Mới").prop('disabled', false);
                    $("#btnChuyenBan").prop('disabled', true);
                    $("#btnGopBan").prop('disabled', true);
                    $("#btnBaoCaoSuCo").prop('disabled', false);
                    $("#tbTongTienWrapper").hide();
                    break;
                case "Có khách":
                    $("#btnGoiMon").text("Gọi Món / Thanh Toán").prop('disabled', false);
                    $("#btnChuyenBan").prop('disabled', false);
                    $("#btnGopBan").prop('disabled', false);
                    $("#btnBaoCaoSuCo").prop('disabled', true);
                    $("#tbTongTienWrapper").show();
                    $("#runTongTien").text(currentSelectedBan.tongTienHienTai.toLocaleString('vi-VN') + " đ");
                    break;
                case "Đã đặt":
                    $("#btnGoiMon").text("Khách đặt (Mở Hóa Đơn)").prop('disabled', false);
                    $("#btnChuyenBan").prop('disabled', true);
                    $("#btnGopBan").prop('disabled', true);
                    $("#btnBaoCaoSuCo").prop('disabled', false);
                    $("#tbTongTienWrapper").hide();
                    break;
                case "Bảo trì":
                case "Tạm ngưng":
                default:
                    $("#btnGoiMon").text("BÀN ĐANG BẢO TRÌ").prop('disabled', true);
                    $("#btnChuyenBan").prop('disabled', true);
                    $("#btnGopBan").prop('disabled', true);
                    $("#btnBaoCaoSuCo").prop('disabled', true);
                    $("#tbTongTienWrapper").hide();
                    break;
            }
        }

        // --- HÀM RESET PANEL ---
        function resetPanel() {
            currentSelectedBan = null;
            $("#panelChuaChon").show();
            $("#panelDaChon").hide();
            $(".table-card").removeClass('selected');
        }

        // --- CÁC HÀM XỬ LÝ SỰ KIỆN ---

        // Tải lần đầu
        loadSoDoBan();
        // Tự động làm mới mỗi 30 giây
        setInterval(loadSoDoBan, 30000);

        // Thay thế .click() bằng .on("click", ...)
        $("#refresh-tables-btn").on("click", loadSoDoBan);

        // --- LOGIC MỚI: KHI CLICK VÀO MỘT BÀN ---
        // Thay thế .click() bằng .on("click", ...)
        $(document).on("click", ".table-card", function () {
            const $card = $(this);

            // Bỏ chọn tất cả, chọn thẻ này
            $(".table-card").removeClass('selected');
            $card.addClass('selected');

            // Lấy dữ liệu từ data-* và cập nhật panel
            updatePanel($card.data());
        });

        // 1. Nút Gọi Món / Tạo Hóa Đơn
        $("#btnGoiMon").on("click", function () {
            if (!currentSelectedBan) return;

            let idHoaDon = currentSelectedBan.idHoaDonHienTai;

            // Nếu là bàn trống, gọi API tạo HĐ trước
            if (currentSelectedBan.trangThai === "Trống" || currentSelectedBan.trangThai === "Đã đặt") {
                if (!currentNhanVienId) {
                    alert("Lỗi: Không tìm thấy ID nhân viên. Vui lòng tải lại trang.");
                    return;
                }

                $(this).prop('disabled', true).text("Đang tạo HĐ...");

                $.ajax({
                    url: `${apiBaseUrl}/api/app/sodoban/createorder/${currentSelectedBan.idBan}/${currentNhanVienId}`,
                    type: 'POST',
                    success: function (result) {
                        idHoaDon = result.idHoaDon;
                        alert(`Đã tạo Hóa đơn mới: ${idHoaDon}. Sẵn sàng điều hướng!`);
                        // TODO: Điều hướng đến trang chi tiết hóa đơn
                        // window.location.href = `/Employee/ChiTietHoaDon?id=${idHoaDon}`;
                        loadSoDoBan(); // Tải lại bàn
                        resetPanel(); // Reset panel về "Chưa chọn"
                    },
                    error: function (xhr) {
                        alert(`Lỗi tạo hóa đơn: ${xhr.responseText}`);
                        // Nếu lỗi, khôi phục lại nút
                        $("#btnGoiMon").prop('disabled', false).text("Tạo Hóa Đơn Mới");
                    }
                });
            } else {
                // Nếu đã có khách, điều hướng trực tiếp
                alert(`Sẵn sàng điều hướng đến trang gọi món cho Hóa đơn ID: ${idHoaDon}`);
                // TODO: Điều hướng
                // window.location.href = `/Employee/ChiTietHoaDon?id=${idHoaDon}`;
            }
        });

        // 2. Nút Báo cáo sự cố
        $("#btnBaoCaoSuCo").on("click", function () {
            if (!currentSelectedBan || !currentNhanVienId) return;

            // Dùng prompt() thay cho InputDialogWindow
            const ghiChu = prompt(`Vui lòng mô tả sự cố cho bàn ${currentSelectedBan.soBan}:`);

            if (ghiChu && ghiChu.trim() !== "") {
                $(this).prop('disabled', true);

                $.ajax({
                    url: `${apiBaseUrl}/api/app/sodoban/reportproblem/${currentSelectedBan.idBan}/${currentNhanVienId}`,
                    type: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify({ ghiChuSuCo: ghiChu }),
                    success: function () {
                        alert("Đã báo cáo sự cố thành công. Bàn đã được khóa.");
                        loadSoDoBan(); // Tải lại
                        resetPanel(); // Reset panel
                    },
                    error: function (xhr) {
                        alert(`Lỗi báo cáo: ${xhr.responseText}`);
                        $("#btnBaoCaoSuCo").prop('disabled', false); // Khôi phục nút nếu lỗi
                    }
                });
            }
        });

        // 3. Các nút đang phát triển
        $("#btnChuyenBan").on("click", function () { alert("Chức năng 'Chuyển Bàn' đang được phát triển."); });
        $("#btnGopBan").on("click", function () { alert("Chức năng 'Gộp Bàn' đang được phát triển."); });
    }
});