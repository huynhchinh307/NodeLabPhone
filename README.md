# NodeLab Farm - Android Multi-Device Management & Automation

**NodeLab Farm** là một ứng dụng Windows chuyên nghiệp được xây dựng trên nền tảng WPF, cho phép quản lý, điều khiển và tự động hóa hàng loạt thiết bị Android thông qua ADB (Android Debug Bridge). 

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![Platform](https://img.shields.io/badge/platform-Windows-0078d7.svg)
![Framework](https://img.shields.io/badge/framework-.NET%208%20WPF-512bd4.svg)

## 🚀 Tính năng nổi bật

### 1. Giám sát thiết bị thời gian thực (Live Monitoring)
- Hiển thị màn hình nhiều thiết bị cùng lúc với tốc độ làm mới cao (~5-10 FPS).
- Giao diện Dashboard hiện đại, hiển thị trực quan trạng thái Pin, Kết nối và các tác vụ đang chạy.

### 2. Bộ soạn thảo kịch bản chuyên nghiệp (Script Editor)
- **Live Inspect**: Soi phần tử trực tiếp trên màn hình preview. Tự động tạo XPath và lấy tọa độ chính xác.
- **Click-to-Lock**: Khóa thông tin phần tử chỉ bằng một cú click để dễ dàng copy.
- **Hỗ trợ đa dạng câu lệnh**: Touch (Tọa độ/XPath), Swipe, Type, Open/Stop App, KeyEvent, Chạy lệnh ADB shell trực tiếp...
- **Quản lý biến**: Hỗ trợ sử dụng biến số trong kịch bản để tăng tính linh hoạt.

### 3. Tự động hóa hàng loạt (Automation)
- Chạy kịch bản đồng thời trên nhiều thiết bị đã chọn.
- Theo dõi log chi tiết từng bước thực hiện của từng thiết bị.
- Quản lý danh sách kịch bản tập trung.

### 4. Giao diện Fluent UI hiện đại
- Thiết kế theo phong cách Windows 11 (Mica backdrop, Rounded corners).
- Chế độ tối (Dark Mode) chuẩn, giúp làm việc lâu không mỏi mắt.

## 🛠 Yêu cầu hệ thống
- **Hệ điều hành**: Windows 10/11.
- **Runtime**: [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
- **ADB**: Đã được cài đặt và cấu hình đường dẫn trong Settings của ứng dụng.

## 📦 Cài đặt & Khởi chạy

1. **Clone project**:
   ```bash
   git clone https://github.com/huynhchinh307/NodeLabFarm.git
   ```
2. **Di chuyển vào thư mục dự án**:
   ```bash
   cd NodeLabFarm
   ```
3. **Build và thực thi**:
   ```bash
   dotnet run
   ```

## 🏗 Công nghệ sử dụng
- **Ngôn ngữ**: C# / XAML.
- **UI Framework**: WPF với [WPF-UI](https://github.com/lepoco/wpfui).
- **ADB Library**: [AdvancedSharpAdbClient](https://github.com/quand some other/AdvancedSharpAdbClient).
- **Dữ liệu**: JSON serialization cho kịch bản và cấu hình.

## 📝 Giấy phép
Dự án được phát hành dưới giấy phép **MIT**. Xem file `LICENSE` để biết thêm chi tiết.

---
*Phát triển bởi NodeLab Team.*
