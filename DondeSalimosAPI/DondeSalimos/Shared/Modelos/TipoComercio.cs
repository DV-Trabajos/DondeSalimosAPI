using System.ComponentModel.DataAnnotations;

namespace DondeSalimos.Shared.Modelos
{
    public class TipoComercio
    {
        [Key]
        public int ID_TipoComercio { get; set; }

        [Required(ErrorMessage = "El campo {0} es requerido")]
        public string Descripcion { get; set; }

        [Required(ErrorMessage = "El campo {0} es requerido")]
        public bool Estado { get; set; }

        [Required(ErrorMessage = "El campo {0} es requerido")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}
