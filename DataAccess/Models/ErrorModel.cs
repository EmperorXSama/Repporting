namespace DataAccess.Models;

public class ErrorModel
{
    public int Id { get; set; }
    public string ErrorType { get; set; } // e.g., Proxy Error, HTTP Error, Others
    public string ErrorMessage { get; set; }
}