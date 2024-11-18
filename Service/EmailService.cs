namespace PotholeDetectionApi.Service
{
    using MailKit.Net.Smtp;
    using MimeKit;
    using System.Threading.Tasks;
    using System.Net;
    using System.Net.Mail;
    using System.Collections.Concurrent;

    public class EmailService
    {
        private readonly ConcurrentDictionary<string, (string OtpCode, DateTime ExpirationTime)> otpStore = new();
        public async Task SendOtpEmailAsync(string recipientEmail, string otpCode)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("YourAppName", "kutosuper@gmail.com"));
            message.To.Add(new MailboxAddress("", recipientEmail));
            message.Subject = "Password Reset OTP";

            message.Body = new TextPart("plain")
            {
                Text = $"Your OTP for password reset is: {otpCode}"
            };

            using (var client = new MailKit.Net.Smtp.SmtpClient())
            {
                try
                {
                    // Kết nối tới SMTP server (sử dụng SMTP của Google)
                    await client.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);

                    // Chứng thực tài khoản Gmail của bạn
                    await client.AuthenticateAsync("kutosuper@gmail.com", "mdwlcfoegzbnkfll");

                    // Gửi email
                    await client.SendAsync(message);
                }
                catch (Exception ex)
                {
                    // Xử lý lỗi
                    Console.WriteLine($"Error: {ex.Message}");
                }
                finally
                {
                    await client.DisconnectAsync(true);
                }
            }
        }

        public string GenerateOtp(int length = 6)
        {
            var random = new Random();
            string otp = new string(Enumerable.Repeat("0123456789", length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
            return otp;
        }

        public async void GenerateAndStoreOtp(string email)
        {
            string otp = GenerateOtp(); // Hàm tạo mã OTP ngẫu nhiên
            DateTime expiration = DateTime.Now.AddMinutes(1); // Thời gian hết hạn (1 phút)

            // Lưu OTP và thời gian hết hạn vào bộ nhớ tạm thời
            otpStore[email] = (otp, expiration);

            // Gửi OTP tới email
            await SendOtpEmailAsync(email, otp);
        }

        public bool VerifyOtp(string email, string inputOtp)
        {
            if (otpStore.ContainsKey(email))
            {
                var (storedOtp, expiration) = otpStore[email];
                if (DateTime.Now > expiration)
                {
                    // Xóa OTP nếu đã hết hạn
                    otpStore.Remove(email, out var _);
                    return false; // OTP hết hạn
                }

                if (storedOtp == inputOtp)
                {
                    // OTP hợp lệ, có thể xóa khỏi bộ nhớ tạm nếu muốn
                    otpStore.Remove(email, out var _);
                    return true;
                }
            }
            return false;
        }

        public List<String> GetOtpStore()
        {
            return otpStore.Select(entry => entry.Value.OtpCode).ToList();
        }

        public List<string> GetExpiredKeys(DateTime currentTime)
        {
            return otpStore.Where(entry => currentTime > entry.Value.ExpirationTime)
                           .Select(entry => entry.Key)
                           .ToList();
        }
        public void RemoveOtp(string key)
        {
            otpStore.Remove(key, out var _);
        }

    }

}
