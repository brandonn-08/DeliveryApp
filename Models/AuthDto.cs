namespace DeliveryApi.Models
{
    public class LoginRequestDto
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

    public class LoginResponseDto
    {
        public string Token { get; set; } = null!;
        public string Role { get; set; } = null!; // "client", "delivery", "admin"
        public string Name { get; set; } = null!;
        public string Identifier { get; set; } = null!; // DNI o ID según corresponda
        public string? Street1 { get; set; }
        public string? Street2 { get; set; }
        public string? Reference { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
    }
}