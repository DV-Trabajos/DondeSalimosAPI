using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DondeSalimos.Shared.Modelos
{
    public class Usuario
    {
        [Key]
        public int ID_Usuario { get; set; }

        [Required(ErrorMessage = "El campo {0} es requerido")]
        public string NombreUsuario { get; set; }

        [Required(ErrorMessage = "El campo {0} es requerido")]
        public string Correo { get; set; }

        [StringLength(20)]
        public string? Telefono { get; set; }

        public string? MotivoRechazo { get; set; }

        public string Uid { get; set; }

        [Required(ErrorMessage = "El campo {0} es requerido")]
        public bool Estado { get; set; }

        [Required(ErrorMessage = "El campo {0} es requerido")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public int ID_RolUsuario { get; set; } = 1;

        [ForeignKey("ID_RolUsuario")]
        public RolUsuario? RolUsuario { get; set; }
    }
}
