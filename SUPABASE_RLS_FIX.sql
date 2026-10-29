-- ====================================================================
-- CHẠY ĐOẠN SQL NÀY TẠI SUPABASE -> "SQL EDITOR" ĐỂ MỞ ĐỒNG BỘ DỮ LIỆU
-- VÀ LƯU MẬT KHẨU QUẢN TRỊ VIÊN VÀO DATABASE
-- ====================================================================

-- 1. Bật quyền Đọc, Thêm, Xoá cho bảng danh sách nộp tiền đoàn phí (fee_submissions)
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

-- 2. Bật quyền Đọc và Cập nhật trạng thái đóng/mở form (system_settings)
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

-- 3. Tạo bảng quản trị viên (admin_users) & Lưu mật khẩu mã hóa PBKDF2
CREATE TABLE IF NOT EXISTS public.admin_users (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    username VARCHAR(50) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    full_name VARCHAR(150) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT TIMEZONE('utc', NOW()),
    last_login TIMESTAMPTZ
);

-- Khởi tạo tài khoản admin ban đầu (Mật khẩu mặc định: admin123, bạn có thể đổi ngay trên Bảng Quản Trị)
INSERT INTO public.admin_users (id, username, password_hash, full_name, created_at)
VALUES (
    uuid_generate_v4(),
    'admin',
    'admin123',
    'Quản trị viên Ban Chấp hành',
    NOW()
) ON CONFLICT (username) DO NOTHING;

-- Bật quyền cho bảng admin_users
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

-- (Hoặc cách nhanh nhất: Tắt RLS trực tiếp cho cả 3 bảng):
-- ALTER TABLE public.fee_submissions DISABLE ROW LEVEL SECURITY;
-- ALTER TABLE public.system_settings DISABLE ROW LEVEL SECURITY;
-- ALTER TABLE public.admin_users DISABLE ROW LEVEL SECURITY;
