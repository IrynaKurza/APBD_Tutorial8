using System.ComponentModel.DataAnnotations;

namespace Tutorial8.Models.DTOs;

public class ClientCreateDTO
{
    [Required] 
    [StringLength(100)]
    public string FirstName { get; set; }

    [Required] 
    [StringLength(100)]
    public string LastName { get; set; }

    [Required] 
    [EmailAddress] 
    public string Email { get; set; }

    [Required] 
    [Phone] 
    public string Telephone { get; set; }

    [Required] 
    [StringLength(11, MinimumLength = 11)]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "PESEL must be 11 digits")]
    public string Pesel { get; set; }
}