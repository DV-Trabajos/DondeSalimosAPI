using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DondeSalimos.Shared.Modelos
{
    public class Reserva
    {
        [Key]
        public int ID_Reserva { get; set; }

        [Required(ErrorMessage = "El campo {0} es requerido")]
        public DateTime FechaReserva { get; set; }

        [Required(ErrorMessage = "El campo {0} es requerido")]
        public TimeSpan TiempoTolerancia { get; set; }

        [Required(ErrorMessage = "El campo {0} es requerido")]
        public int Comenzales { get; set; }

        [Required(ErrorMessage = "El campo {0} es requerido")]
        public bool Estado { get; set; }

        [Required(ErrorMessage = "El campo {0} es requerido")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        //public int ID_Cliente { get; set; }

        //[ForeignKey("ID_Cliente")]
        //public Cliente? Cliente { get; set; }
        public string? MotivoRechazo { get; set; }
        public int ID_Usuario { get; set; }

        [ForeignKey("ID_Usuario")]
        public Usuario? Usuario { get; set; }

        public int ID_Comercio { get; set; }

        [ForeignKey("ID_Comercio")]
        public Comercio? Comercio { get; set; }
    }
}
