using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DondeSalimos.Shared.Modelos
{
    public class Publicidad
    {
        [Key]
        public int ID_Publicidad { get; set; }

        [Required(ErrorMessage = "El campo {0} es requerido")]
        public string Descripcion { get; set; }

        [Required(ErrorMessage = "El campo {0} es requerido")]
        public int Visualizaciones { get; set; }

        [Required(ErrorMessage = "El campo {0} es requerido")]
        public TimeSpan Tiempo { get; set; }

        [Required(ErrorMessage = "El campo {0} es requerido")]
        public byte[]? Imagen { get; set; }

        [Required(ErrorMessage = "El campo {0} es requerido")]
        public bool Estado { get; set; }

        [Required(ErrorMessage = "El campo {0} es requerido")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public int ID_Comercio { get; set; }

        [ForeignKey("ID_Comercio")]
        public Comercio? Comercio { get; set; }
    }
}
