﻿namespace WebApplication1.DTO.Configuration
{
    public class EmailSettings
    {
        public string? Host { get; set; }
        public int Port { get; set; }
        public bool EnableSsl { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

}
