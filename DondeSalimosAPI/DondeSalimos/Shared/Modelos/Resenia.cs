using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DondeSalimos.Shared.Modelos
{
    public class Resenia
    {
        [Key]
        public int ID_Resenia { get; set; }

        [Required(ErrorMessage = "El campo {0} es requerido")]
        public string Comentario { get; set; }

        [Required(ErrorMessage = "El campo {0} es requerido")]
        public bool Estado { get; set; }

        [Required(ErrorMessage = "El campo {0} es requerido")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        //public int ID_Cliente { get; set; }

        //[ForeignKey("ID_Cliente")]
        //public Cliente? Cliente { get; set; }

        public int ID_Usuario { get; set; }

        [ForeignKey("ID_Usuario")]
        public Usuario? Usuario { get; set; }        

        public int ID_Comercio { get; set; }

        [ForeignKey("ID_Comercio")]
        public Comercio? Comercio { get; set; }
    }
}
