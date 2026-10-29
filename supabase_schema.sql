-- ========================================================
-- SCHEMA CƠ SỞ DỮ LIỆU & STORAGE SUPABASE (POSTGRESQL)
-- DỰ ÁN: HỆ THỐNG THU ĐOÀN PHÍ - HỘI PHÍ NĂM HỌC 2026 - 2027
-- Hướng dẫn: Copy toàn bộ nội dung này và chạy tại SQL Editor trên Supabase
-- ========================================================

-- 1. Kích hoạt extension sinh mã UUID ngẫu nhiên
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- 2. Bảng lưu trữ hồ sơ nộp đoàn phí (fee_submissions)
CREATE TABLE IF NOT EXISTS public.fee_submissions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    full_name VARCHAR(150) NOT NULL,
    student_id VARCHAR(50) NOT NULL,
    membership_status VARCHAR(50) NOT NULL,
    duration_months INT NOT NULL DEFAULT 12,
    amount NUMERIC(12, 2) NOT NULL,
    transfer_memo VARCHAR(255) NOT NULL,
    notes TEXT,
    proof_image TEXT, -- Lưu URL ảnh (nếu dùng Supabase Storage) HOẶC chuỗi Base64
    created_at TIMESTAMPTZ NOT NULL DEFAULT TIMEZONE('utc', NOW())
);

-- Tạo Index tăng tốc độ tra cứu và tìm kiếm đối soát
CREATE INDEX IF NOT EXISTS idx_fee_submissions_student_id ON public.fee_submissions(student_id);
CREATE INDEX IF NOT EXISTS idx_fee_submissions_created_at ON public.fee_submissions(created_at DESC);

-- 3. Bảng cài đặt hệ thống: Quản lý trạng thái đóng/mở form (system_settings)
CREATE TABLE IF NOT EXISTS public.system_settings (
    id INT PRIMARY KEY DEFAULT 1,
    is_form_open BOOLEAN NOT NULL DEFAULT TRUE,
    close_reason TEXT,
    last_updated TIMESTAMPTZ NOT NULL DEFAULT TIMEZONE('utc', NOW()),
    CONSTRAINT single_row_check CHECK (id = 1)
);

-- Chèn dữ liệu cài đặt mặc định ban đầu nếu chưa có
INSERT INTO public.system_settings (id, is_form_open, close_reason, last_updated)
VALUES (1, TRUE, NULL, TIMEZONE('utc', NOW()))
ON CONFLICT (id) DO NOTHING;

-- 4. Bảng tài khoản quản trị viên (admin_users) - Lưu mật khẩu mã hóa PBKDF2
CREATE TABLE IF NOT EXISTS public.admin_users (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    username VARCHAR(50) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    full_name VARCHAR(150) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT TIMEZONE('utc', NOW()),
    last_login TIMESTAMPTZ
);

-- Chèn tài khoản admin khởi tạo ban đầu (Mật khẩu mặc định: admin123, sẽ tự băm bảo mật PBKDF2 hoặc bạn có thể đổi ngay trên web)
INSERT INTO public.admin_users (id, username, password_hash, full_name, created_at)
VALUES (
    uuid_generate_v4(),
    'admin',
    'admin123',
    'Quản trị viên Ban Chấp hành',
    NOW()
) ON CONFLICT (username) DO NOTHING;

-- 5. Chèn một số bản ghi mẫu kiểm thử đối soát
INSERT INTO public.fee_submissions (id, full_name, student_id, membership_status, duration_months, amount, transfer_memo, notes, created_at)
VALUES 
    (uuid_generate_v4(), 'Nguyễn Văn An', '20261001', 'Đã vào Đoàn', 12, 108000, '20261001 NGUYEN VAN AN Doan phi', 'Lớp K48 - CNTT 01', NOW() - INTERVAL '2 days'),
    (uuid_generate_v4(), 'Trần Thị Bích', '20261002', 'Chưa vào Đoàn', 12, 84000, '20261002 TRAN THI BICH Doan phi', 'Lớp K48 - Kế toán 02', NOW() - INTERVAL '1 day'),
    (uuid_generate_v4(), 'Lê Hoàng Nam', '20261003', 'Đã vào Đoàn', 12, 108000, '20261003 LE HOANG NAM Doan phi', 'Lớp K48 - Luật 01', NOW())
ON CONFLICT DO NOTHING;

-- 5. THIẾT LẬP QUYỀN TRUY CẬP (ROW LEVEL SECURITY - RLS POLICIES)
-- Bật RLS và cấp quyền cho anon (public API key) và authenticated
ALTER TABLE public.fee_submissions ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "Cho phép đọc dữ liệu fee_submissions" ON public.fee_submissions;
CREATE POLICY "Cho phép đọc dữ liệu fee_submissions"
ON public.fee_submissions FOR SELECT
TO anon, authenticated
USING (true);

DROP POLICY IF EXISTS "Cho phép thêm bản ghi fee_submissions" ON public.fee_submissions;
CREATE POLICY "Cho phép thêm bản ghi fee_submissions"
ON public.fee_submissions FOR INSERT
TO anon, authenticated
WITH CHECK (true);

DROP POLICY IF EXISTS "Cho phép cập nhật fee_submissions" ON public.fee_submissions;
CREATE POLICY "Cho phép cập nhật fee_submissions"
ON public.fee_submissions FOR UPDATE
TO anon, authenticated
USING (true);

DROP POLICY IF EXISTS "Cho phép xoá fee_submissions" ON public.fee_submissions;
CREATE POLICY "Cho phép xoá fee_submissions"
ON public.fee_submissions FOR DELETE
TO anon, authenticated
USING (true);

-- Thiết lập RLS cho bảng cài đặt hệ thống (system_settings)
ALTER TABLE public.system_settings ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "Cho phép đọc system_settings" ON public.system_settings;
CREATE POLICY "Cho phép đọc system_settings"
ON public.system_settings FOR SELECT
TO anon, authenticated
USING (true);

DROP POLICY IF EXISTS "Cho phép sửa system_settings" ON public.system_settings;
CREATE POLICY "Cho phép sửa system_settings"
ON public.system_settings FOR ALL
TO anon, authenticated
USING (true)
WITH CHECK (true);

-- Thiết lập RLS cho bảng quản trị viên (admin_users)
ALTER TABLE public.admin_users ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "Cho phép đọc admin_users" ON public.admin_users;
CREATE POLICY "Cho phép đọc admin_users"
ON public.admin_users FOR SELECT
TO anon, authenticated
USING (true);

DROP POLICY IF EXISTS "Cho phép tạo admin_users" ON public.admin_users;
CREATE POLICY "Cho phép tạo admin_users"
ON public.admin_users FOR INSERT
TO anon, authenticated
WITH CHECK (true);

DROP POLICY IF EXISTS "Cho phép sửa admin_users" ON public.admin_users;
CREATE POLICY "Cho phép sửa admin_users"
ON public.admin_users FOR UPDATE
TO anon, authenticated
USING (true)
WITH CHECK (true);

-- 6. CẤU HÌNH SUPABASE STORAGE CHO ẢNH MINH CHỨNG (TÙY CHỌN TỐI ƯU NHẤT)
-- Tạo Bucket 'receipts' công khai để lưu file ảnh minh chứng
INSERT INTO storage.buckets (id, name, public) 
VALUES ('receipts', 'receipts', true)
ON CONFLICT (id) DO NOTHING;

-- Thiết lập quyền (RLS Policy): Cho phép nộp ảnh công khai vào bucket 'receipts'
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_policies WHERE policyname = 'Cho phép upload ảnh minh chứng công khai'
    ) THEN
        CREATE POLICY "Cho phép upload ảnh minh chứng công khai" 
        ON storage.objects FOR INSERT 
        WITH CHECK (bucket_id = 'receipts');
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_policies WHERE policyname = 'Cho phép xem và tải ảnh minh chứng'
    ) THEN
        CREATE POLICY "Cho phép xem và tải ảnh minh chứng" 
        ON storage.objects FOR SELECT 
        USING (bucket_id = 'receipts');
    END IF;
END $$;

-- Thông báo hoàn thành tạo Schema
COMMENT ON TABLE public.fee_submissions IS 'Bảng lưu trữ hồ sơ nộp tiền đoàn phí và ảnh biên nhận';
COMMENT ON TABLE public.system_settings IS 'Bảng lưu trữ trạng thái đóng/mở form nộp phí';
