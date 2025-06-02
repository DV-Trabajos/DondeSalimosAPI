using System.ComponentModel.DataAnnotations;

namespace DondeSalimos.Shared.Modelos
{
    public class RolUsuario
    {
        [Key]
        public int ID_RolUsuario { get; set; }

        [Required(ErrorMessage = "El campo {0} es requerido")]
        public string Descripcion { get; set; }

        [Required(ErrorMessage = "El campo {0} es requerido")]
        public bool Estado { get; set; }

        [Required(ErrorMessage = "El campo {0} es requerido")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}
