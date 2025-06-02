using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DondeSalimos.Shared.Modelos
{
    public class Cliente
    {
        [Key]
        public int ID_Cliente { get; set; }

        [Required(ErrorMessage = "El campo {0} es requerido")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "El campo {0} es requerido")]
        public string Apellido { get; set; }

        public string TipoDocumento { get; set; }

        public string NroDocumento { get; set; }

        [DataType(DataType.EmailAddress)]
        [RegularExpression(@"^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$", ErrorMessage = "El correo es inválido.")]
        public string Correo { get; set; }

        [DataType(DataType.PhoneNumber)]
        public string Telefono { get; set; }

        [Required(ErrorMessage = "El campo {0} es requerido")]
        public bool Estado { get; set; }

        [Required(ErrorMessage = "El campo {0} es requerido")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public int ID_Usuario { get; set; }

        [ForeignKey("ID_Usuario")]
        public Usuario? Usuario { get; set; }
    }
}
