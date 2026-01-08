# Danh sách chức năng & Lệnh điều khiển (NodeLabPhone)

Tài liệu này liệt kê các chức năng và lệnh điều khiển kịch bản đã được lập trình và đang hoạt động thực tế trên hệ thống.

## 1. Hệ thống lõi (Core Features)
- [x] **ADB Server Management**: Tự động quản lý và khởi động máy chủ ADB.
- [x] **Live Streaming (Dashboard)**: Stream hình ảnh màn hình tốc độ cao (~5-10 FPS) theo thời gian thực cho toàn bộ dàn máy.
- [x] **Scrcpy Integration**: Mở cửa sổ điều khiển phóng to mượt mà (60 FPS) bằng cách nhấp đúp vào máy.
- [x] **UiAutomator Engine**: Lõi phân tích cấu trúc màn hình XML để tìm phần tử thông minh.
- [x] **Multi-threading Execution**: Chạy kịch bản song song trên nhiều thiết bị mà không gây giật lag.

## 2. Danh sách lệnh Kịch bản (Script Commands)
Các lệnh này đã được viết logic thực thi trong `AdbService.ExecuteStepAsync` và có thể chạy trực tiếp.

### Nhóm Tương tác
- [x] **Open App**: Mở ứng dụng theo Package Name (Ví dụ: `com.android.chrome`).
- [x] **Smart Tap (Chạm thông minh)**:
    - Chạm theo tọa độ `X,Y`.
    - Chạm theo **Văn bản (Text)**: Tự tìm chữ trên màn hình và nhấn.
    - Chạm theo **ID**: Nhấn vào phần tử theo Resource-ID.
- [x] **Swipe (Vuốt)**: Vuốt từ điểm A đến điểm B với thời gian tùy chỉnh.
- [x] **Type (Gõ chữ)**: Nhập văn bản vào các ô nhập liệu (Hỗ trợ cả dấu cách).
- [x] **Press Key**: Gõ phím theo mã KeyCode (ví dụ: Enter, Volume Up/Down).

### Nhóm Hệ thống
- [x] **Home**: Quay về màn hình chính.
- [x] **Back**: Nhấn nút quay lại.
- [x] **Stop App**: Buộc dừng ứng dụng đang chạy.
- [x] **Clear Data**: Xóa sạch dữ liệu ứng dụng.
- [x] **ADB Command**: Chạy lệnh Shell ADB tùy chỉnh.
- [x] **Pause (Tạm dừng)**: Dừng kịch bản trong một khoảng thời gian (giây/mili giây).

### Nhóm Kiểm tra (Logic)
- [x] **Find Text**: Kiểm tra một đoạn chữ có xuất hiện trên màn hình hay không.
- [x] **Element Exists**: Kiểm tra sự tồn tại của một phần tử (ID/Text) trước khi làm bước tiếp theo.

## 3. Trạng thái giao diện (UI Status)
- [x] **Real-time Logs**: Hiển thị chi tiết từng bước đang chạy trong cửa sổ chỉnh sửa kịch bản.
- [x] **Badge Status**: Hiển thị tên bước đang thực hiện ngay trên card điện thoại ở Dashboard.
- [x] **Running Threads Count**: Đếm chính xác số lượng luồng kịch bản đang chạy đồng thời.

---
*Cập nhật lần cuối: 22:15 - 08/01/2026*
