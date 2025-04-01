using System.ComponentModel.DataAnnotations;

namespace SpaceUser.Models.User
{
    public class ForgotPassword
    {
        [Required (ErrorMessage = "El Campo {0} es Obligatorio"), DataType(DataType.EmailAddress), Display(Name = "E-mail: ")]
        public string? Email { get; set; }
    }
}