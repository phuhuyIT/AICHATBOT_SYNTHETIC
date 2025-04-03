namespace WebApplication1.DTO.SendMail
{
    public class RegisterEmail
    {
        public string UserName { get; set; }
        public string OTPPart1 { get; set; }
        public string OTPPart2 { get; set; }
        public string OTPPart3 { get; set; }
        public string OTPPart4 { get; set; }
        public string VerificationLink { get; set; }
        public string LogoUrl { get; set; }
    }
}
